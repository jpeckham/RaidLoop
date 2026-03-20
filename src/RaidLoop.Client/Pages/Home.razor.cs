using System.Net;
using RaidLoop.Client.Services;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Client.Pages;

public partial class Home : IDisposable
{
    private const string FallbackKnifeName = "Rusty Knife";
    private const int MaxHealth = 30;
    private const int ExtractRequired = 3;
    private const int MainStashCap = 30;
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
    private bool _weaponMalfunction;
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
            await AuthService.SignOutAsync();
            _isLoading = false;
            return;
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
    private bool CanAttack => (!EquippedWeaponUsesAmmo || _ammo > 0) && !_weaponMalfunction;
    private bool CanBurstFire => EquippedWeaponUsesAmmo && _ammo >= 2 && !_weaponMalfunction;
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
        var response = await Actions.SendAsync("start-main-raid", new { });
        ApplySnapshot(response.Snapshot);
        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            _resultMessage = response.Message;
        }
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
        var response = await Actions.SendAsync("start-random-raid", new { });
        ApplySnapshot(response.Snapshot);
        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            _resultMessage = response.Message;
        }
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
        var response = await Actions.SendAsync(action, payload);
        ApplySnapshot(response.Snapshot);
        NormalizeEquippedSlots();
        EnsureMainCharacterHasWeaponFallback();
        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            _resultMessage = response.Message;
        }
    }

    private async Task ExecuteRaidActionAsync(string action, object payload)
    {
        var response = await Actions.SendAsync(action, payload);
        ApplySnapshot(response.Snapshot);
        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            _resultMessage = response.Message;
        }
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
        if (_encounterType != EncounterType.Combat)
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
        _weaponMalfunction = snapshot.WeaponMalfunction;
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
