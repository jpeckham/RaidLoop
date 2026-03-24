using System.Net;
using System.Text.Json;
using RaidLoop.Client.Services;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Client.Pages;

public partial class Home : IDisposable
{
    private const string FallbackKnifeName = "Rusty Knife";
    private const int ExtractRequired = 3;
    private const int MainStashCap = 30;
    private int _playerConstitution = 10;
    private int _maxHealth = 30;
#if DEBUG
    private static readonly TimeSpan LuckRunCooldown = TimeSpan.FromSeconds(5);
#else
    private static readonly TimeSpan LuckRunCooldown = TimeSpan.FromMinutes(5);
#endif

    private readonly List<ShopStock> _shopStock =
    [
        new(ItemCatalog.Create("Medkit")),
        new(ItemCatalog.Create("Makarov")),
        new(ItemCatalog.Create("Small Backpack"))
    ];

    private GameState _mainGame = new([]);
    private List<OnPersonEntry> _onPersonItems = [];
    private int _money;

    private RandomCharacterState? _randomCharacter;
    private DateTimeOffset _randomCharacterAvailableAt = DateTimeOffset.MinValue;
    private System.Threading.Timer? _clockTimer;

    private RaidState? _raid;
    private bool _isLoading = true;
    private bool _inRaid;
    private bool _awaitingDecision;
    private EncounterType _encounterType = EncounterType.Neutral;
    private string _encounterDescription = string.Empty;

    private string _enemyName = string.Empty;
    private int _enemyHealth;

    private string _lootContainer = string.Empty;
    private static readonly List<Item> EmptyItems = [];

    private int _ammo;
    private int _extractProgress;
    private string _resultMessage = string.Empty;
    private string _activeRaidId = string.Empty;
    private readonly List<string> _log = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await Profiles.BootstrapAsync();
            ApplySnapshot(response.Snapshot);
            NormalizeEquippedSlots();
            EnsureMainCharacterHasWeaponFallback();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await ReportHandledErrorAsync("Profile bootstrap failed due to unauthorized session.", "bootstrap", ex);
            await AuthService.SignOutAsync();
            _isLoading = false;
            return;
        }
        catch (Exception ex)
        {
            await ReportHandledErrorAsync("Profile bootstrap failed.", "bootstrap", ex);
            throw;
        }

        _clockTimer = new System.Threading.Timer(async _ =>
        {
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        _isLoading = false;
    }

    private bool IsRandomCharacterReady => DateTimeOffset.UtcNow >= _randomCharacterAvailableAt;
    private bool HasUnprocessedLuckRunLoot => _randomCharacter is not null && _randomCharacter.Inventory.Count > 0;

    private bool HasUnequippedOnPersonItems => _onPersonItems.Any(x => IsSlotType(x.Item.Type) && !x.IsEquipped);
    private bool HasEquippedWeapon => _onPersonItems.Any(x => x.IsEquipped && x.Item.Type == ItemType.Weapon);
    private bool CanStartMainRaid => !HasUnprocessedLuckRunLoot && !HasUnequippedOnPersonItems && HasEquippedWeapon;
    private bool CanStartLuckRunRaid => !HasUnprocessedLuckRunLoot && IsRandomCharacterReady;
    private string? RaidBlockReason => HasUnprocessedLuckRunLoot
        ? "Luck Run loot must be sold, stored, or moved to For Raid before entering a raid."
        : HasUnequippedOnPersonItems
            ? "You need to move your unequipped items to stash or sell them."
            : !HasEquippedWeapon ? "You don't have a weapon equipped." : null;
    private string? LuckRunBlockReason => HasUnprocessedLuckRunLoot
        ? "Luck Run loot must be sold, stored, or moved to For Raid before entering a raid."
        : null;
    private bool CanStashOnPersonItem => _mainGame.Stash.Count < MainStashCap;
    private bool EquippedWeaponUsesAmmo => CombatBalance.WeaponUsesAmmo(GetEquippedWeaponName());
    private int CurrentMagazineCapacity => CombatBalance.GetMagazineCapacity(GetEquippedWeaponName());
    private bool EquippedWeaponSupportsSingleShot => CombatBalance.SupportsSingleShot(GetEquippedWeaponName());
    private bool EquippedWeaponSupportsBurstFire => CombatBalance.SupportsBurstFire(GetEquippedWeaponName());
    private bool EquippedWeaponSupportsFullAuto => CombatBalance.SupportsFullAuto(GetEquippedWeaponName());
    private bool CanAttack => EquippedWeaponSupportsSingleShot;
    private bool CanAttackEnabled => !EquippedWeaponUsesAmmo || _ammo > 0;
    private bool CanBurstFire => EquippedWeaponSupportsBurstFire;
    private bool CanBurstFireEnabled => EquippedWeaponUsesAmmo && _ammo >= 3;
    private bool CanFullAuto => EquippedWeaponSupportsFullAuto;
    private bool CanFullAutoEnabled => EquippedWeaponUsesAmmo && _ammo >= 10;
    private bool CanReload => _raid is not null && EquippedWeaponUsesAmmo && CurrentMagazineCapacity > 0 && _ammo < CurrentMagazineCapacity;
    private bool CanUseMedkit => CurrentMedkits > 0;
    private int CurrentMedkits => _raid?.Inventory.MedkitCount ?? 0;
    private List<Item> CurrentDiscoveredLoot => _raid?.Inventory.DiscoveredLoot ?? EmptyItems;
    private List<Item> CurrentCarriedLoot => _raid?.Inventory.CarriedItems ?? EmptyItems;

    private static bool IsSlotType(ItemType type)
    {
        return type is ItemType.Weapon or ItemType.Armor or ItemType.Backpack;
    }

    private int? FindEquippedIndexForSlot(ItemType slotType)
    {
        var idx = _onPersonItems.FindIndex(x => x.IsEquipped && x.Item.Type == slotType);
        return idx >= 0 ? idx : null;
    }

    private bool ShouldEquipFromStash(Item item)
    {
        return IsSlotType(item.Type) && FindEquippedIndexForSlot(item.Type) is null;
    }

    private string GetStashPrimaryActionLabel(Item item)
    {
        return ShouldEquipFromStash(item) ? "Equip" : "For Raid";
    }

    private async Task SellStashItemAsync(int stashIndex)
    {
        if (stashIndex < 0 || stashIndex >= _mainGame.Stash.Count)
        {
            return;
        }

        var item = _mainGame.Stash[stashIndex];
        if (GetSellPrice(item) <= 0)
        {
            return;
        }

        await ExecuteProfileActionAsync("sell-stash-item", new { stashIndex });
    }

    private async Task MoveStashToOnPersonAsync(int stashIndex)
    {
        if (stashIndex < 0 || stashIndex >= _mainGame.Stash.Count)
        {
            return;
        }

        await ExecuteProfileActionAsync("move-stash-to-on-person", new { stashIndex });
    }

    private async Task SellOnPersonItemAsync(int onPersonIndex)
    {
        if (onPersonIndex < 0 || onPersonIndex >= _onPersonItems.Count)
        {
            return;
        }

        var item = _onPersonItems[onPersonIndex].Item;
        if (GetSellPrice(item) <= 0)
        {
            return;
        }

        await ExecuteProfileActionAsync("sell-on-person-item", new { onPersonIndex });
    }

    private async Task StashOnPersonItemAsync(int onPersonIndex)
    {
        if (!CanStashOnPersonItem || onPersonIndex < 0 || onPersonIndex >= _onPersonItems.Count)
        {
            return;
        }

        await ExecuteProfileActionAsync("stash-on-person-item", new { onPersonIndex });
    }

    private async Task EquipOnPersonItemAsync(int onPersonIndex)
    {
        if (onPersonIndex < 0 || onPersonIndex >= _onPersonItems.Count)
        {
            return;
        }

        var selected = _onPersonItems[onPersonIndex];
        if (!IsSlotType(selected.Item.Type))
        {
            return;
        }

        await ExecuteProfileActionAsync("equip-on-person-item", new { onPersonIndex });
    }

    private async Task UnequipOnPersonItemAsync(int onPersonIndex)
    {
        if (onPersonIndex < 0 || onPersonIndex >= _onPersonItems.Count)
        {
            return;
        }

        var current = _onPersonItems[onPersonIndex];
        if (!IsSlotType(current.Item.Type))
        {
            return;
        }

        await ExecuteProfileActionAsync("unequip-on-person-item", new { onPersonIndex });
    }

    private async Task BuyFromShopAsync(ShopStock stock)
    {
        var price = GetBuyPrice(stock.Item.Name);
        if (_money < price)
        {
            return;
        }

        await ExecuteProfileActionAsync("buy-from-shop", new { itemName = stock.Item.Name });
    }

    private async Task StartMainRaidAsync()
    {
        if (!CanStartMainRaid)
        {
            return;
        }

        _activeRaidId = Guid.NewGuid().ToString("N");
        GameEventLog.Clear();
        GameEventLog.SetRaidContext(_activeRaidId);
        await ExecuteRaidActionAsync("start-main-raid", new { });
    }

    private async Task StartRandomRaidAsync()
    {
        if (!CanStartLuckRunRaid)
        {
            return;
        }

        _activeRaidId = Guid.NewGuid().ToString("N");
        GameEventLog.Clear();
        GameEventLog.SetRaidContext(_activeRaidId);
        await ExecuteRaidActionAsync("start-random-raid", new { });
    }

    private async Task StoreLuckRunItemAsync(int luckIndex)
    {
        if (_randomCharacter is null || luckIndex < 0 || luckIndex >= _randomCharacter.Inventory.Count)
        {
            return;
        }

        if (_mainGame.Stash.Count >= MainStashCap)
        {
            return;
        }

        await ExecuteProfileActionAsync("store-luck-run-item", new { luckIndex });
    }

    private async Task MoveLuckRunItemToForRaidAsync(int luckIndex)
    {
        if (_randomCharacter is null || luckIndex < 0 || luckIndex >= _randomCharacter.Inventory.Count)
        {
            return;
        }

        await ExecuteProfileActionAsync("move-luck-run-item-to-on-person", new { luckIndex });
    }

    private async Task SellLuckRunItemAsync(int luckIndex)
    {
        if (_randomCharacter is null || luckIndex < 0 || luckIndex >= _randomCharacter.Inventory.Count)
        {
            return;
        }

        var item = _randomCharacter.Inventory[luckIndex];
        if (GetSellPrice(item) <= 0)
        {
            return;
        }

        await ExecuteProfileActionAsync("sell-luck-run-item", new { luckIndex });
    }

    private async Task ExecuteProfileActionAsync(string action, object payload)
    {
        try
        {
            var result = await Actions.SendAsync(action, payload);
            ApplyActionResult(result);
            NormalizeEquippedSlots();
            EnsureMainCharacterHasWeaponFallback();
        }
        catch (Exception ex)
        {
            await ReportHandledErrorAsync($"Profile action '{action}' failed.", "profile-action", ex, new { action, payload });
            throw;
        }
    }

    private async Task ExecuteRaidActionAsync(string action, object payload)
    {
        var raidPayload = CreateRaidActionPayload(payload);

        try
        {
            var result = await Actions.SendAsync(action, raidPayload);
            ApplyActionResult(result);
        }
        catch (Exception ex)
        {
            await ReportHandledErrorAsync($"Raid action '{action}' failed.", "raid-action", ex, new { action, payload = raidPayload });
            throw;
        }
    }

    private void ApplyActionResult(GameActionResult result)
    {
        if (result.Projections is { ValueKind: JsonValueKind.Object } projections)
        {
            ApplyProjectedState(projections);
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            _resultMessage = result.Message;
        }
    }

    private void ApplyProjectedState(JsonElement projections)
    {
        var updatedStash = false;
        var updatedLoadout = false;

        if (TryGetProjection(projections, "economy", out var economy)
            && TryGetInt32(economy, "money", out var money))
        {
            _money = money;
        }

        if (TryGetProjection(projections, "stash", out var stash)
            && TryGetProjection(stash, "mainStash", out var mainStash))
        {
            if (TryReadItemList(mainStash, out var parsedStash))
            {
                _mainGame = new GameState(parsedStash);
                updatedStash = true;
            }
        }

        if (TryGetProjection(projections, "loadout", out var loadout)
            && TryGetProjection(loadout, "onPersonItems", out var onPersonItems))
        {
            if (TryReadOnPersonEntries(onPersonItems, out var parsedOnPersonItems))
            {
                _onPersonItems = parsedOnPersonItems;
                updatedLoadout = true;
            }
        }

        if (TryGetProjection(projections, "luckRun", out var luckRun))
        {
            if (TryGetString(luckRun, "randomCharacterAvailableAt", out var availableAtText)
                && DateTimeOffset.TryParse(availableAtText, out var availableAt))
            {
                _randomCharacterAvailableAt = availableAt;
            }

            if (TryGetProjection(luckRun, "randomCharacter", out var randomCharacter))
            {
                _randomCharacter = randomCharacter.ValueKind == JsonValueKind.Null
                    ? null
                    : ReadRandomCharacter(randomCharacter);

                if (_randomCharacter is not null && _randomCharacter.Inventory.Count == 0)
                {
                    _randomCharacter = null;
                }
            }
        }

        if (TryGetProjection(projections, "raid", out var raid))
        {
            if (raid.ValueKind == JsonValueKind.Null)
            {
                ClearRaidState();
            }
            else
            {
                ApplyRaidProjection(raid);
            }
        }

        if (updatedStash || updatedLoadout)
        {
            EnsureMainCharacterHasWeaponFallback();
        }

        if (updatedLoadout)
        {
            NormalizeEquippedSlots();
        }
    }

    private void ApplyRaidProjection(JsonElement raid)
    {
        var freshRaid = _raid is null;
        if (freshRaid)
        {
            _raid = new RaidState(_maxHealth, new RaidInventory());
            _inRaid = true;
            _awaitingDecision = false;
            _extractProgress = 0;
            _ammo = 0;
            _encounterDescription = string.Empty;
            _enemyName = string.Empty;
            _enemyHealth = 0;
            _lootContainer = string.Empty;
            _log.Clear();
            _encounterType = EncounterType.Neutral;
        }

        var raidState = _raid!;
        var inventory = raidState.Inventory;
        var health = raidState.Health;
        var backpackCapacity = raidState.BackpackCapacity;
        var hasRaidPatch = freshRaid;

        if (TryGetInt32(raid, "health", out var parsedHealth))
        {
            health = parsedHealth;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "backpackCapacity", out var parsedBackpackCapacity))
        {
            backpackCapacity = parsedBackpackCapacity;
            hasRaidPatch = true;
        }

        if (TryGetProjection(raid, "equippedItems", out var equippedItems))
        {
            if (TryReadItemList(equippedItems, out var items))
            {
                inventory.EquippedWeapon = items.FirstOrDefault(item => item.Type == ItemType.Weapon);
                inventory.EquippedArmor = items.FirstOrDefault(item => item.Type == ItemType.Armor);
                inventory.EquippedBackpack = items.FirstOrDefault(item => item.Type == ItemType.Backpack);
                hasRaidPatch = true;
            }
        }

        if (TryGetProjection(raid, "carriedLoot", out var carriedLoot))
        {
            if (TryReadItemList(carriedLoot, out var items))
            {
                inventory.CarriedItems.Clear();
                inventory.CarriedItems.AddRange(items);
                hasRaidPatch = true;
            }
        }

        if (TryGetProjection(raid, "discoveredLoot", out var discoveredLoot))
        {
            if (TryReadItemList(discoveredLoot, out var items))
            {
                inventory.DiscoveredLoot.Clear();
                inventory.DiscoveredLoot.AddRange(items);
                hasRaidPatch = true;
            }
        }

        if (TryGetInt32(raid, "medkits", out var medkits))
        {
            inventory.MedkitCount = medkits;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "ammo", out var ammo))
        {
            _ammo = ammo;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "extractProgress", out var extractProgress))
        {
            _extractProgress = extractProgress;
            hasRaidPatch = true;
        }

        if (TryGetBool(raid, "awaitingDecision", out var awaitingDecision))
        {
            _awaitingDecision = awaitingDecision;
            hasRaidPatch = true;
        }

        if (TryGetString(raid, "encounterDescription", out var encounterDescription))
        {
            _encounterDescription = encounterDescription;
            hasRaidPatch = true;
        }

        if (TryGetString(raid, "enemyName", out var enemyName))
        {
            _enemyName = enemyName;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "enemyHealth", out var enemyHealth))
        {
            _enemyHealth = enemyHealth;
            hasRaidPatch = true;
        }

        if (TryGetString(raid, "lootContainer", out var lootContainer))
        {
            _lootContainer = lootContainer;
            hasRaidPatch = true;
        }

        if (TryGetString(raid, "encounterType", out var encounterTypeText)
            && Enum.TryParse<EncounterType>(encounterTypeText, ignoreCase: true, out var encounterType))
        {
            _encounterType = encounterType;
            hasRaidPatch = true;
        }

        if (TryGetProjection(raid, "logEntriesAdded", out var logEntriesAdded))
        {
            if (logEntriesAdded.ValueKind == JsonValueKind.Array)
            {
                _log.AddRange(ReadStringListFromProperty(raid, "logEntriesAdded"));
                hasRaidPatch = true;
            }
        }
        else if (TryGetProjection(raid, "logEntries", out var logEntries))
        {
            if (logEntries.ValueKind == JsonValueKind.Array)
            {
                _log.Clear();
                _log.AddRange(ReadStringListFromProperty(raid, "logEntries"));
                hasRaidPatch = true;
            }
        }

        inventory.BackpackCapacity = backpackCapacity;
        raidState.Health = health;
        raidState.BackpackCapacity = backpackCapacity;

        if (!freshRaid && hasRaidPatch)
        {
            _raid = raidState;
        }

        if (freshRaid || hasRaidPatch)
        {
            _inRaid = true;
        }
    }

    private object CreateRaidActionPayload(object payload)
    {
        return payload switch
        {
            null => new { knownLogCount = _log.Count },
            _ => JsonSerializer.Deserialize<Dictionary<string, object?>>(
                JsonSerializer.Serialize(payload)) is { } values
                ? AddKnownLogCount(values)
                : new Dictionary<string, object?> { ["knownLogCount"] = _log.Count }
        };
    }

    private Dictionary<string, object?> AddKnownLogCount(Dictionary<string, object?> values)
    {
        values["knownLogCount"] = _log.Count;
        return values;
    }

    private void ClearRaidState()
    {
        _raid = null;
        _inRaid = false;
        _awaitingDecision = false;
        _extractProgress = 0;
        _ammo = 0;
        _encounterType = EncounterType.Neutral;
        _encounterDescription = string.Empty;
        _enemyName = string.Empty;
        _enemyHealth = 0;
        _lootContainer = string.Empty;
        _log.Clear();
    }

    private static List<Item> ReadItemList(JsonElement items)
    {
        return TryReadItemList(items, out var parsedItems) ? parsedItems : [];
    }

    private static List<Item> ReadItemListFromProperty(JsonElement parent, string propertyName)
    {
        return TryGetProjection(parent, propertyName, out var items) ? ReadItemList(items) : [];
    }

    private static List<OnPersonEntry> ReadOnPersonEntries(JsonElement onPersonItems)
    {
        return TryReadOnPersonEntries(onPersonItems, out var parsedEntries) ? parsedEntries : [];
    }

    private static bool TryReadOnPersonEntries(JsonElement onPersonItems, out List<OnPersonEntry> entries)
    {
        entries = [];
        if (onPersonItems.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var hasValidEntry = false;
        foreach (var entry in onPersonItems.EnumerateArray())
        {
            if (!TryGetProjection(entry, "item", out var itemElement)
                && !TryGetProjection(entry, "Item", out itemElement))
            {
                continue;
            }

            if (!TryReadItem(itemElement, out var parsedItem))
            {
                continue;
            }

            entries.Add(new OnPersonEntry(
                parsedItem,
                TryGetBool(entry, "isEquipped", out var isEquipped) && isEquipped));
            hasValidEntry = true;
        }

        return hasValidEntry || onPersonItems.GetArrayLength() == 0;
    }

    private static RandomCharacterState ReadRandomCharacter(JsonElement randomCharacter)
    {
        var name = TryGetString(randomCharacter, "name", out var randomCharacterName)
            ? randomCharacterName
            : string.Empty;
        var inventory = TryGetProjection(randomCharacter, "inventory", out var inventoryItems)
            && TryReadItemList(inventoryItems, out var parsedInventory)
            ? parsedInventory
            : [];

        return new RandomCharacterState(name, inventory);
    }

    private static bool TryReadItemList(JsonElement items, out List<Item> parsedItems)
    {
        parsedItems = [];
        if (items.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var hasValidItem = false;
        foreach (var entry in items.EnumerateArray())
        {
            if (!TryReadItem(entry, out var parsedItem))
            {
                continue;
            }

            parsedItems.Add(parsedItem);
            hasValidItem = true;
        }

        return hasValidItem || items.GetArrayLength() == 0;
    }

    private static bool TryReadItem(JsonElement item, out Item parsedItem)
    {
        var name = TryGetString(item, "name", out var itemName)
            ? itemName
            : TryGetString(item, "Name", out var itemNameUpperCase)
                ? itemNameUpperCase
                : string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            parsedItem = default!;
            return false;
        }

        if (ItemCatalog.TryGet(name, out var catalogItem))
        {
            parsedItem = catalogItem!;
            return true;
        }

        var type = TryGetInt32(item, "type", out var parsedType) && Enum.IsDefined(typeof(ItemType), parsedType)
            ? (ItemType)parsedType
            : TryGetInt32(item, "Type", out var parsedTypeUpperCase) && Enum.IsDefined(typeof(ItemType), parsedTypeUpperCase)
                ? (ItemType)parsedTypeUpperCase
            : ItemType.Sellable;
        var value = TryGetInt32(item, "value", out var parsedValue)
            ? parsedValue
            : TryGetInt32(item, "Value", out var parsedValueUpperCase)
                ? parsedValueUpperCase
                : 1;
        var slots = TryGetInt32(item, "slots", out var parsedSlots)
            ? parsedSlots
            : TryGetInt32(item, "Slots", out var parsedSlotsUpperCase)
                ? parsedSlotsUpperCase
                : 1;
        var rarity = TryGetInt32(item, "rarity", out var parsedRarity) && Enum.IsDefined(typeof(Rarity), parsedRarity)
            ? (Rarity)parsedRarity
            : TryGetInt32(item, "Rarity", out var parsedRarityUpperCase) && Enum.IsDefined(typeof(Rarity), parsedRarityUpperCase)
                ? (Rarity)parsedRarityUpperCase
            : Rarity.Common;
        var displayRarity = TryGetInt32(item, "displayRarity", out var parsedDisplayRarity) && Enum.IsDefined(typeof(DisplayRarity), parsedDisplayRarity)
            ? (DisplayRarity)parsedDisplayRarity
            : TryGetInt32(item, "DisplayRarity", out var parsedDisplayRarityUpperCase) && Enum.IsDefined(typeof(DisplayRarity), parsedDisplayRarityUpperCase)
                ? (DisplayRarity)parsedDisplayRarityUpperCase
            : DisplayRarity.Common;

        parsedItem = new Item(name, type, value, slots, rarity, displayRarity);
        return true;
    }

    private static List<string> ReadStringListFromProperty(JsonElement parent, string propertyName)
    {
        if (!TryGetProjection(parent, propertyName, out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return items.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private static bool TryGetProjection(JsonElement parent, string propertyName, out JsonElement value)
    {
        if (parent.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in parent.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetInt32(JsonElement parent, string propertyName, out int value)
    {
        if (TryGetProjection(parent, propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value))
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetBool(JsonElement parent, string propertyName, out bool value)
    {
        if (TryGetProjection(parent, propertyName, out var property))
        {
            if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                value = property.GetBoolean();
                return true;
            }

            if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetString(JsonElement parent, string propertyName, out string value)
    {
        if (TryGetProjection(parent, propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private async Task ExecuteLootActionAsync(string action, Item item, string eventName)
    {
        await ExecuteRaidActionAsync(action, new { itemName = item.Name });
        GameEventLog.Append(new GameEvent(
            eventName,
            _activeRaidId,
            GameEventLog.CreateItemSnapshots([item]),
            DateTimeOffset.UtcNow));
    }

    private string GetRandomCooldownText()
    {
        var left = _randomCharacterAvailableAt - DateTimeOffset.UtcNow;
        if (left < TimeSpan.Zero)
        {
            left = TimeSpan.Zero;
        }

        return $"{left.Minutes:D2}:{left.Seconds:D2}";
    }

    private string GetEquippedWeaponName()
    {
        var weapon = _raid?.Inventory.EquippedWeapon;
        return weapon?.Name ?? FallbackKnifeName;
    }

    private string GetAmmoHudText()
    {
        if (!EquippedWeaponUsesAmmo)
        {
            return "Ammo: ∞";
        }

        return $"Ammo: {_ammo} / {CurrentMagazineCapacity}";
    }

    private string GetEncounterTitle()
    {
        return _encounterType switch
        {
            EncounterType.Combat => "Combat Encounter",
            EncounterType.Loot => "Loot Encounter",
            EncounterType.Neutral => "Area Clear",
            EncounterType.Extraction => "Extraction Opportunity",
            _ => "Encounter"
        };
    }

    private async Task AttackAsync()
    {
        if (_raid is null || _encounterType != EncounterType.Combat)
        {
            return;
        }
        await ExecuteRaidActionAsync("attack", new { target = "enemy" });
    }

    private async Task BurstFireAsync()
    {
        if (_raid is null || _encounterType != EncounterType.Combat)
        {
            return;
        }
        await ExecuteRaidActionAsync("burst-fire", new { target = "enemy" });
    }

    private async Task FullAutoAsync()
    {
        if (_raid is null || _encounterType != EncounterType.Combat)
        {
            return;
        }

        await ExecuteRaidActionAsync("full-auto", new { target = "enemy" });
    }

    private async Task UseMedkitAsync()
    {
        if (_raid is null)
        {
            return;
        }
        await ExecuteRaidActionAsync("use-medkit", new { });
    }

    private async Task ReloadAsync()
    {
        if (!CanReload)
        {
            return;
        }
        await ExecuteRaidActionAsync("reload", new { });
    }

    private async Task FleeAsync()
    {
        if (_encounterType != EncounterType.Combat)
        {
            return;
        }
        await ExecuteRaidActionAsync("flee", new { });
    }


    private Task TakeLootAsync(Item lootItem)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }
        return ExecuteLootActionAsync("take-loot", lootItem, "loot.acquired");
    }

    private Task DropCarriedAsync(Item item)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }
        return ExecuteRaidActionAsync("drop-carried", new { itemName = item.Name });
    }

    private Task DropEquippedAsync(ItemType slotType)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }
        return ExecuteRaidActionAsync("drop-equipped", new { slotType = slotType.ToString() });
    }

    private Task EquipFromDiscoveredAsync(Item item)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }
        return ExecuteLootActionAsync("equip-from-discovered", item, "player.equip");
    }

    private Task EquipFromCarriedAsync(Item item)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }
        return ExecuteLootActionAsync("equip-from-carried", item, "player.equip");
    }

    private static bool CanEquipItem(Item item)
    {
        return item.Type is ItemType.Weapon or ItemType.Armor or ItemType.Backpack;
    }

    private IEnumerable<Item> GetEquippedItems()
    {
        if (_raid?.Inventory.EquippedWeapon is not null)
        {
            yield return _raid.Inventory.EquippedWeapon;
        }

        if (_raid?.Inventory.EquippedArmor is not null)
        {
            yield return _raid.Inventory.EquippedArmor;
        }

        if (_raid?.Inventory.EquippedBackpack is not null)
        {
            yield return _raid.Inventory.EquippedBackpack;
        }
    }

    private async Task AttemptExtractAsync()
    {
        if (_raid is null)
        {
            return;
        }
        await ExecuteRaidActionAsync("attempt-extract", new { });
    }

    private async Task ContinueSearching()
    {
        await ExecuteRaidActionAsync("continue-searching", new { });
    }

    private async Task MoveTowardExtract()
    {
        await ExecuteRaidActionAsync("move-toward-extract", new { });
    }

    private ValueTask ReportHandledErrorAsync(string message, string source, Exception exception, object? context = null)
    {
        return Telemetry.ReportErrorAsync(
            message,
            new
            {
                source,
                exception = exception.GetType().FullName,
                exceptionMessage = exception.Message,
                stack = exception.ToString(),
                context
            });
    }

    private int GetLootSlotCount()
    {
        return _raid?.Inventory.CarriedItems.Sum(x => x.Slots) ?? 0;
    }

    private int GetBuyPrice(string itemName)
    {
        return CombatBalance.GetBuyPrice(itemName);
    }

    private int GetSellPrice(Item item)
    {
        if (string.Equals(item.Name, FallbackKnifeName, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return item.Value;
    }

    private bool CanSellItem(Item item)
    {
        return GetSellPrice(item) > 0;
    }

    private bool CanLootItem(Item item)
    {
        if (_raid is null)
        {
            return false;
        }

        if (string.Equals(item.Name, "Medkit", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var currentSlots = _raid.Inventory.CarriedItems.Sum(x => x.Slots);
        return currentSlots + item.Slots <= _raid.BackpackCapacity;
    }

    private void EnsureMainCharacterHasWeaponFallback()
    {
        var stashHasWeapon = _mainGame.Stash.Any(item => item.Type == ItemType.Weapon);
        var onPersonHasWeapon = _onPersonItems.Any(entry => entry.Item.Type == ItemType.Weapon);
        if (!stashHasWeapon && !onPersonHasWeapon)
        {
            _mainGame.Stash.Add(ItemCatalog.Create("Rusty Knife"));
        }
    }

    private void NormalizeEquippedSlots()
    {
        NormalizeEquippedSlot(ItemType.Weapon);
        NormalizeEquippedSlot(ItemType.Armor);
        NormalizeEquippedSlot(ItemType.Backpack);
    }

    private void NormalizeEquippedSlot(ItemType slotType)
    {
        var found = false;
        for (var i = 0; i < _onPersonItems.Count; i++)
        {
            var entry = _onPersonItems[i];
            if (entry.Item.Type != slotType || !entry.IsEquipped)
            {
                continue;
            }

            if (!found)
            {
                found = true;
                continue;
            }

            _onPersonItems[i] = entry with { IsEquipped = false };
        }
    }

    private void ApplySnapshot(PlayerSnapshot snapshot)
    {
        _mainGame = new GameState([.. snapshot.MainStash]);
        _randomCharacter = snapshot.RandomCharacter is null
            ? null
            : new RandomCharacterState(snapshot.RandomCharacter.Name, [.. snapshot.RandomCharacter.Inventory]);
        _randomCharacterAvailableAt = snapshot.RandomCharacterAvailableAt;

        if (_randomCharacter is not null && _randomCharacter.Inventory.Count == 0)
        {
            _randomCharacter = null;
        }

        _money = snapshot.Money;
        _playerConstitution = snapshot.PlayerConstitution;
        _maxHealth = snapshot.PlayerMaxHealth;
        _onPersonItems = snapshot.OnPersonItems
            .Select(entry => new OnPersonEntry(entry.Item, entry.IsEquipped))
            .ToList();

        if (snapshot.ActiveRaid is null)
        {
            _raid = null;
            _inRaid = false;
            return;
        }

        ApplyActiveRaidSnapshot(snapshot.ActiveRaid);
    }

    private void ApplyActiveRaidSnapshot(RaidSnapshot snapshot)
    {
        var broughtItems = snapshot.EquippedItems.ToList();
        var carriedItems = snapshot.CarriedLoot.ToList();
        _raid = new RaidState(
            snapshot.Health,
            RaidInventory.FromItems(broughtItems, carriedItems, snapshot.BackpackCapacity));
        _raid.Inventory.DiscoveredLoot.Clear();
        _raid.Inventory.DiscoveredLoot.AddRange(snapshot.DiscoveredLoot);
        _raid.Inventory.MedkitCount = snapshot.Medkits;
        _raid.Inventory.BackpackCapacity = snapshot.BackpackCapacity;
        _inRaid = true;
        _awaitingDecision = snapshot.AwaitingDecision;
        _extractProgress = snapshot.ExtractProgress;
        _ammo = snapshot.Ammo;
        _encounterDescription = snapshot.EncounterDescription;
        _enemyName = snapshot.EnemyName;
        _enemyHealth = snapshot.EnemyHealth;
        _lootContainer = snapshot.LootContainer;
        _log.Clear();
        _log.AddRange(snapshot.LogEntries);

        if (!Enum.TryParse<EncounterType>(snapshot.EncounterType, ignoreCase: true, out var encounterType))
        {
            encounterType = EncounterType.Neutral;
        }

        _encounterType = encounterType;
    }

    public void Dispose()
    {
        _clockTimer?.Dispose();
    }

}
