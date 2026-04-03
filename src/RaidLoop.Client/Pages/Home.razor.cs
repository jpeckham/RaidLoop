using System.Net;
using System.Text.Json;
using RaidLoop.Client.Services;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Client.Pages;

public partial class Home : IDisposable
{
    private const int FallbackKnifeItemDefId = 1;
    private const int MedkitItemDefId = 19;
    private const int MainStashCap = 30;
    private static readonly string[] StatOrder = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];
    private int _playerConstitution = 10;
    private int _maxHealth = 30;
#if DEBUG
    private static readonly TimeSpan LuckRunCooldown = TimeSpan.FromSeconds(5);
#else
    private static readonly TimeSpan LuckRunCooldown = TimeSpan.FromMinutes(5);
#endif

    private GameState _mainGame = new([]);
    private List<ShopStock> _shopStock = [];
    private Dictionary<int, ItemRuleSnapshot> _itemRulesById = [];
    private List<OnPersonEntry> _onPersonItems = [];
    private int _money;
    private PlayerStats _acceptedStats = PlayerStats.Default;
    private PlayerStats _draftStats = PlayerStats.Default;
    private int _availableStatPoints = PlayerStatRules.StartingPool;
    private bool _statsAccepted;

    private RandomCharacterState? _randomCharacter;
    private DateTimeOffset _randomCharacterAvailableAt = DateTimeOffset.MinValue;
    private System.Threading.Timer? _clockTimer;

    private RaidState? _raid;
    private bool _isLoading = true;
    private bool _inRaid;
    private bool _awaitingDecision;
    private int? _raidEncumbrance;
    private int? _raidMaxEncumbrance;
    private bool _extractHoldActive;
    private DateTimeOffset? _holdAtExtractUntil;
    private bool _extractHoldResolutionInFlight;
    private EncounterType _encounterType = EncounterType.Neutral;
    private string _encounterDescription = string.Empty;
    private string _contactState = string.Empty;
    private string _surpriseSide = string.Empty;
    private string _initiativeWinner = string.Empty;
    private int _openingActionsRemaining;
    private bool _surprisePersistenceEligible;

    private string _enemyName = string.Empty;
    private int _enemyHealth;
    private int _enemyDexterity;
    private int _enemyConstitution;
    private int _enemyStrength;

    private string _lootContainer = string.Empty;
    private static readonly List<Item> EmptyItems = [];

    private int _ammo;
    private int _challenge;
    private int _distanceFromExtract;
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
            await InvokeAsync(async () =>
            {
                await ResolveExpiredExtractHoldAsync();
                StateHasChanged();
            });
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        _isLoading = false;
    }

    private bool IsRandomCharacterReady => DateTimeOffset.UtcNow >= _randomCharacterAvailableAt;
    private bool HasUnprocessedLuckRunLoot => _randomCharacter is not null && _randomCharacter.Inventory.Count > 0;

    private bool HasUnequippedOnPersonItems => _onPersonItems.Any(x => IsSlotType(x.Item.Type) && !x.IsEquipped);
    private bool HasEquippedWeapon => _onPersonItems.Any(x => x.IsEquipped && x.Item.Type == ItemType.Weapon);
    private bool HasOverweightOnPersonItems => GetOnPersonEncumbrance() > GetMainCharacterMaxEncumbrance();
    private bool CanStartMainRaid => _statsAccepted && !HasUnprocessedLuckRunLoot && !HasUnequippedOnPersonItems && !HasOverweightOnPersonItems && HasEquippedWeapon;
    private bool CanStartLuckRunRaid => _statsAccepted && !HasUnprocessedLuckRunLoot && IsRandomCharacterReady;
    private string? RaidBlockReason => GetRaidBlockReason();
    private string? LuckRunBlockReason => HasUnprocessedLuckRunLoot
        ? "Luck Run loot must be sold, stored, or moved to For Raid before entering a raid."
        : !_statsAccepted
            ? "Accept Stats before entering a raid."
        : null;
    private bool CanStashOnPersonItem => _mainGame.Stash.Count < MainStashCap;
    private int GetOnPersonEncumbrance()
    {
        return CombatBalance.GetTotalEncumbrance(_onPersonItems.Select(entry => entry.Item));
    }

    private int GetMainCharacterMaxEncumbrance()
    {
        return CombatBalance.GetMaxEncumbranceFromStrength(_acceptedStats.Strength);
    }

    private bool CanAddOnPersonItem(Item item)
    {
        return GetOnPersonEncumbrance() + Math.Max(0, item.Weight) <= GetMainCharacterMaxEncumbrance();
    }

    private string GetPreRaidEncumbranceText()
    {
        return $"{GetOnPersonEncumbrance()}/{GetMainCharacterMaxEncumbrance()} lbs";
    }

    private string? GetRaidBlockReason()
    {
        var reasons = new List<string>();

        if (HasUnprocessedLuckRunLoot)
        {
            reasons.Add("Luck Run loot must be sold, stored, or moved to For Raid before entering a raid.");
        }

        if (!_statsAccepted)
        {
            reasons.Add("Accept Stats before entering a raid.");
        }

        if (HasUnequippedOnPersonItems)
        {
            reasons.Add("You need to move your unequipped items to stash or sell them.");
        }

        if (HasOverweightOnPersonItems)
        {
            reasons.Add($"Your loadout weight is too high. {GetPreRaidEncumbranceText()}");
        }

        if (!HasEquippedWeapon)
        {
            reasons.Add("You don't have a weapon equipped.");
        }

        return reasons.Count == 0 ? null : string.Join(" ", reasons);
    }

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
    private IReadOnlyList<ShopStock> VisibleShopStock => _shopStock.Where(stock => CanBuyItem(stock.Item)).ToList();
    private bool CanReallocateStats => _raid is null && _statsAccepted && _money >= GetReallocateStatCost();

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

        var item = _mainGame.Stash[stashIndex];
        if (!CanAddOnPersonItem(item))
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
        var price = stock.Price;
        if (_money < price || !CanAddOnPersonItem(stock.Item))
        {
            return;
        }

        await ExecuteProfileActionAsync("buy-from-shop", new { itemDefId = stock.ItemDefId });
    }

    private async Task AcceptStatsAsync()
    {
        if (_statsAccepted)
        {
            return;
        }

        await ExecuteProfileActionAsync("accept-stats", new { draftStats = _draftStats });
    }

    private async Task ReallocateStatsAsync()
    {
        if (_raid is not null || !_statsAccepted || _money < GetReallocateStatCost())
        {
            return;
        }

        await ExecuteProfileActionAsync("reallocate-stats", new { });
    }

    private void IncrementDraftStat(string statKey)
    {
        if (_raid is not null || _statsAccepted)
        {
            return;
        }

        var current = GetDraftStatValue(statKey);
        var cost = PlayerStatRules.GetRaiseCost(current);
        if (current >= PlayerStatRules.MaximumScore || cost <= 0 || _availableStatPoints < cost)
        {
            return;
        }

        SetDraftStatValue(statKey, current + 1);
        _availableStatPoints -= cost;
    }

    private void DecrementDraftStat(string statKey)
    {
        if (_raid is not null || _statsAccepted)
        {
            return;
        }

        var current = GetDraftStatValue(statKey);
        var refund = PlayerStatRules.GetLowerRefund(current);
        if (current <= PlayerStatRules.MinimumScore || refund <= 0)
        {
            return;
        }

        SetDraftStatValue(statKey, current - 1);
        _availableStatPoints += refund;
    }

    private int GetDraftStatValue(string statKey)
    {
        return statKey switch
        {
            "STR" => _draftStats.Strength,
            "DEX" => _draftStats.Dexterity,
            "CON" => _draftStats.Constitution,
            "INT" => _draftStats.Intelligence,
            "WIS" => _draftStats.Wisdom,
            "CHA" => _draftStats.Charisma,
            _ => PlayerStatRules.MinimumScore
        };
    }

    private int GetDraftModifier(string statKey)
    {
        return PlayerStatRules.GetAbilityModifier(GetDraftStatValue(statKey));
    }

    private bool CanIncreaseDraftStat(string statKey)
    {
        var current = GetDraftStatValue(statKey);
        var cost = PlayerStatRules.GetRaiseCost(current);
        return _raid is null
            && !_statsAccepted
            && current < PlayerStatRules.MaximumScore
            && cost > 0
            && _availableStatPoints >= cost;
    }

    private bool CanDecreaseDraftStat(string statKey)
    {
        return _raid is null
            && !_statsAccepted
            && GetDraftStatValue(statKey) > PlayerStatRules.MinimumScore;
    }

    private void SetDraftStatValue(string statKey, int value)
    {
        _draftStats = statKey switch
        {
            "STR" => _draftStats with { Strength = value },
            "DEX" => _draftStats with { Dexterity = value },
            "CON" => _draftStats with { Constitution = value },
            "INT" => _draftStats with { Intelligence = value },
            "WIS" => _draftStats with { Wisdom = value },
            "CHA" => _draftStats with { Charisma = value },
            _ => _draftStats
        };
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

    private async Task SignOutAsync()
    {
        await AuthService.SignOutAsync();
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

        if (TryGetProjection(projections, "player", out var playerProjection))
        {
            ApplyPlayerProjection(playerProjection);
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
                if (randomCharacter.ValueKind == JsonValueKind.Null)
                {
                    _randomCharacter = null;
                }
                else if (TryReadRandomCharacter(randomCharacter, out var parsedRandomCharacter))
                {
                    _randomCharacter = parsedRandomCharacter;
                }

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

    private void ApplyPlayerProjection(JsonElement playerProjection)
    {
        if (TryGetProjection(playerProjection, "acceptedStats", out var acceptedStats)
            && TryReadPlayerStats(acceptedStats, out var parsedAcceptedStats))
        {
            _acceptedStats = parsedAcceptedStats;
            _playerConstitution = parsedAcceptedStats.Constitution;
        }

        if (TryGetInt32(playerProjection, "playerConstitution", out var playerConstitution))
        {
            _playerConstitution = playerConstitution;
        }

        if (TryGetInt32(playerProjection, "playerMaxHealth", out var playerMaxHealth))
        {
            _maxHealth = playerMaxHealth;
        }

        if (TryGetProjection(playerProjection, "draftStats", out var draftStats)
            && TryReadPlayerStats(draftStats, out var parsedDraftStats))
        {
            _draftStats = parsedDraftStats;
        }

        if (TryGetInt32(playerProjection, "availableStatPoints", out var availableStatPoints))
        {
            _availableStatPoints = availableStatPoints;
        }

        if (TryGetBool(playerProjection, "statsAccepted", out var statsAccepted))
        {
            _statsAccepted = statsAccepted;
        }
    }

    private void ApplyRaidProjection(JsonElement raid)
    {
        var freshRaid = _raid is null;
        var raidEncumbranceProjected = false;
        var raidMaxEncumbranceProjected = false;
        var inventoryChanged = false;
        if (freshRaid)
        {
            _raidEncumbrance = null;
            _raidMaxEncumbrance = null;
            _extractHoldActive = false;
            _holdAtExtractUntil = null;
            _extractHoldResolutionInFlight = false;
            _raid = new RaidState(_maxHealth, new RaidInventory());
            _inRaid = true;
            _awaitingDecision = false;
            _challenge = 0;
            _distanceFromExtract = 3;
            _ammo = 0;
            _encounterDescription = string.Empty;
            _contactState = string.Empty;
            _surpriseSide = string.Empty;
            _initiativeWinner = string.Empty;
            _openingActionsRemaining = 0;
            _surprisePersistenceEligible = false;
            _enemyName = string.Empty;
            _enemyHealth = 0;
            _enemyDexterity = 0;
            _enemyConstitution = 0;
            _enemyStrength = 0;
            _lootContainer = string.Empty;
            _log.Clear();
            _encounterType = EncounterType.Neutral;
        }

        var raidState = _raid!;
        var inventory = raidState.Inventory;
        var health = raidState.Health;
        var backpackCapacity = raidState.BackpackCapacity;
        var hasRaidPatch = freshRaid;
        var holdProjectionSeen = false;
        var parsedExtractHoldActive = false;
        DateTimeOffset? parsedHoldAtExtractUntil = null;
        var extractHoldActiveSeen = false;
        var holdAtExtractUntilSeen = false;

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

        if (TryGetInt32(raid, "encumbrance", out var parsedEncumbrance))
        {
            _raidEncumbrance = parsedEncumbrance;
            raidEncumbranceProjected = true;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "maxEncumbrance", out var parsedMaxEncumbrance))
        {
            _raidMaxEncumbrance = parsedMaxEncumbrance;
            raidState.MaxEncumbrance = parsedMaxEncumbrance;
            raidMaxEncumbranceProjected = true;
            hasRaidPatch = true;
        }

        if (TryGetBool(raid, "extractHoldActive", out var extractHoldActive))
        {
            parsedExtractHoldActive = extractHoldActive;
            extractHoldActiveSeen = true;
            holdProjectionSeen = true;
            hasRaidPatch = true;
        }

        if (TryGetNullableDateTimeOffset(raid, "holdAtExtractUntil", out var holdAtExtractUntil))
        {
            parsedHoldAtExtractUntil = holdAtExtractUntil;
            holdAtExtractUntilSeen = true;
            holdProjectionSeen = true;
            hasRaidPatch = true;
        }

        if (holdProjectionSeen)
        {
            _extractHoldActive = extractHoldActiveSeen ? parsedExtractHoldActive : false;
            _holdAtExtractUntil = holdAtExtractUntilSeen ? parsedHoldAtExtractUntil : null;
        }

        if (TryGetProjection(raid, "equippedItems", out var equippedItems))
        {
            if (TryReadItemList(equippedItems, out var items))
            {
                inventory.EquippedWeapon = items.FirstOrDefault(item => item.Type == ItemType.Weapon);
                inventory.EquippedArmor = items.FirstOrDefault(item => item.Type == ItemType.Armor);
                inventory.EquippedBackpack = items.FirstOrDefault(item => item.Type == ItemType.Backpack);
                inventoryChanged = true;
                hasRaidPatch = true;
            }
        }

        if (TryGetProjection(raid, "carriedLoot", out var carriedLoot))
        {
            if (TryReadItemList(carriedLoot, out var items))
            {
                inventory.CarriedItems.Clear();
                inventory.CarriedItems.AddRange(items);
                inventoryChanged = true;
                hasRaidPatch = true;
            }
        }

        if (TryGetProjection(raid, "discoveredLoot", out var discoveredLoot))
        {
            if (TryReadItemList(discoveredLoot, out var items))
            {
                inventory.DiscoveredLoot.Clear();
                inventory.DiscoveredLoot.AddRange(items);
                inventoryChanged = true;
                hasRaidPatch = true;
            }
        }

        if (TryGetInt32(raid, "medkits", out var medkits))
        {
            inventory.MedkitCount = medkits;
            inventoryChanged = true;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "ammo", out var ammo))
        {
            _ammo = ammo;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "challenge", out var challenge))
        {
            _challenge = challenge;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "distanceFromExtract", out var distanceFromExtract))
        {
            _distanceFromExtract = distanceFromExtract;
            hasRaidPatch = true;
        }

        if (TryGetBool(raid, "awaitingDecision", out var awaitingDecision))
        {
            _awaitingDecision = awaitingDecision;
            hasRaidPatch = true;
        }

        string? encounterDescription = null;
        string? encounterDescriptionKey = null;
        if (TryGetString(raid, "encounterDescription", out var encounterDescriptionText))
        {
            encounterDescription = encounterDescriptionText;
            hasRaidPatch = true;
        }
        if (TryGetString(raid, "encounterDescriptionKey", out var encounterDescriptionKeyText))
        {
            encounterDescriptionKey = encounterDescriptionKeyText;
            hasRaidPatch = true;
        }

        if (TryGetString(raid, "contactState", out var contactState))
        {
            _contactState = string.IsNullOrWhiteSpace(contactState) ? string.Empty : contactState;
            hasRaidPatch = true;
        }

        if (TryGetString(raid, "surpriseSide", out var surpriseSide))
        {
            _surpriseSide = string.IsNullOrWhiteSpace(surpriseSide) ? string.Empty : surpriseSide;
            hasRaidPatch = true;
        }

        if (TryGetString(raid, "initiativeWinner", out var initiativeWinner))
        {
            _initiativeWinner = string.IsNullOrWhiteSpace(initiativeWinner) ? string.Empty : initiativeWinner;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "openingActionsRemaining", out var openingActionsRemaining))
        {
            _openingActionsRemaining = openingActionsRemaining;
            hasRaidPatch = true;
        }

        if (TryGetBool(raid, "surprisePersistenceEligible", out var surprisePersistenceEligible))
        {
            _surprisePersistenceEligible = surprisePersistenceEligible;
            hasRaidPatch = true;
        }

        string? enemyName = null;
        string? enemyKey = null;
        if (TryGetString(raid, "enemyName", out var enemyNameText))
        {
            enemyName = enemyNameText;
            hasRaidPatch = true;
        }
        if (TryGetString(raid, "enemyKey", out var enemyKeyText))
        {
            enemyKey = enemyKeyText;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "enemyHealth", out var enemyHealth))
        {
            _enemyHealth = enemyHealth;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "enemyDexterity", out var enemyDexterity))
        {
            _enemyDexterity = enemyDexterity;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "enemyConstitution", out var enemyConstitution))
        {
            _enemyConstitution = enemyConstitution;
            hasRaidPatch = true;
        }

        if (TryGetInt32(raid, "enemyStrength", out var enemyStrength))
        {
            _enemyStrength = enemyStrength;
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

        if (encounterDescription is not null)
        {
            _encounterDescription = RaidPresentationCatalog.GetEncounterDescription(encounterDescriptionKey, _encounterType, encounterDescription, _extractHoldActive);
        }

        if (enemyName is not null || enemyKey is not null)
        {
            _enemyName = RaidPresentationCatalog.GetEnemyLabel(enemyKey, enemyName);
        }

        if (TryGetProjection(raid, "logEntriesAdded", out var logEntriesAdded))
        {
            if (logEntriesAdded.ValueKind == JsonValueKind.Array)
            {
                _log.AddRange(ReadStringListFromProperty(raid, "logEntriesAdded").Select(RaidPresentationCatalog.LocalizeLogEntry));
                hasRaidPatch = true;
            }
        }
        else if (TryGetProjection(raid, "logEntries", out var logEntries))
        {
            if (logEntries.ValueKind == JsonValueKind.Array)
            {
                _log.Clear();
                _log.AddRange(ReadStringListFromProperty(raid, "logEntries").Select(RaidPresentationCatalog.LocalizeLogEntry));
                hasRaidPatch = true;
            }
        }

        inventory.BackpackCapacity = backpackCapacity;
        raidState.Health = health;
        raidState.BackpackCapacity = backpackCapacity;

        if (inventoryChanged && !raidEncumbranceProjected)
        {
            _raidEncumbrance = null;
        }

        if (inventoryChanged && !raidMaxEncumbranceProjected)
        {
            _raidMaxEncumbrance = null;
        }

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
        _raidEncumbrance = null;
        _raidMaxEncumbrance = null;
        _extractHoldActive = false;
        _holdAtExtractUntil = null;
        _extractHoldResolutionInFlight = false;
        _inRaid = false;
        _awaitingDecision = false;
        _challenge = 0;
        _distanceFromExtract = 0;
        _ammo = 0;
        _encounterType = EncounterType.Neutral;
        _encounterDescription = string.Empty;
        _contactState = string.Empty;
        _surpriseSide = string.Empty;
        _initiativeWinner = string.Empty;
        _openingActionsRemaining = 0;
        _surprisePersistenceEligible = false;
        _enemyName = string.Empty;
        _enemyHealth = 0;
        _enemyDexterity = 0;
        _enemyConstitution = 0;
        _enemyStrength = 0;
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

    private static bool TryReadRandomCharacter(JsonElement randomCharacter, out RandomCharacterState parsedRandomCharacter)
    {
        parsedRandomCharacter = null!;
        var name = TryGetString(randomCharacter, "name", out var randomCharacterName)
            ? randomCharacterName
            : string.Empty;
        var inventory = TryGetProjection(randomCharacter, "inventory", out var inventoryItems)
            && TryReadItemList(inventoryItems, out var parsedInventory)
            ? parsedInventory
            : [];
        if (!TryGetProjection(randomCharacter, "stats", out var statsElement) || !TryReadPlayerStats(statsElement, out var parsedStats))
        {
            return false;
        }

        parsedRandomCharacter = new RandomCharacterState(name, inventory, parsedStats);
        return true;
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
        var hasItemDefId = TryGetInt32(item, "itemDefId", out var itemDefId) && itemDefId > 0;
        var itemKey = TryGetString(item, "itemKey", out var itemKeyValue)
            ? itemKeyValue
            : TryGetString(item, "ItemKey", out var itemKeyUpperCase)
                ? itemKeyUpperCase
                : string.Empty;
        var name = TryGetString(item, "name", out var itemName)
            ? itemName
            : TryGetString(item, "Name", out var itemNameUpperCase)
                ? itemNameUpperCase
                : string.Empty;

        if (hasItemDefId
            && ItemCatalog.TryGetByItemDefId(itemDefId, out var catalogItemById)
            && catalogItemById is not null)
        {
            parsedItem = catalogItemById;
            return true;
        }

        if (!hasItemDefId
            && !string.IsNullOrWhiteSpace(itemKey)
            && ItemCatalog.TryGetByKey(itemKey, out var catalogItemByKey)
            && catalogItemByKey is not null)
        {
            parsedItem = catalogItemByKey;
            return true;
        }

        if (!hasItemDefId
            && !string.IsNullOrWhiteSpace(name)
            && ItemCatalog.TryGet(name, out var catalogItem))
        {
            parsedItem = catalogItem!;
            return true;
        }

        if (!hasItemDefId && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(itemKey))
        {
            parsedItem = default!;
            return false;
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
        if (!TryGetInt32(item, "weight", out var parsedWeight)
            && !TryGetInt32(item, "Weight", out parsedWeight))
        {
            parsedItem = default!;
            return false;
        }
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

        parsedItem = new Item(string.IsNullOrWhiteSpace(name) ? itemKey : name, type, parsedWeight, value, slots, rarity, displayRarity)
        {
            ItemDefId = hasItemDefId ? itemDefId : 0,
            Key = itemKey
        };
        return true;
    }

    private void ApplyItemRulesProjection(JsonElement itemRules)
    {
        if (itemRules.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var updatedRules = new Dictionary<int, ItemRuleSnapshot>();
        foreach (var rule in itemRules.EnumerateArray())
        {
            if (!TryGetInt32(rule, "itemDefId", out var itemDefId) || itemDefId <= 0)
            {
                continue;
            }

            var type = TryGetInt32(rule, "type", out var parsedType) && Enum.IsDefined(typeof(ItemType), parsedType)
                ? (ItemType)parsedType
                : ItemType.Sellable;
            var weight = TryGetInt32(rule, "weight", out var parsedWeight) ? parsedWeight : 0;
            var slots = TryGetInt32(rule, "slots", out var parsedSlots) ? parsedSlots : 1;
            var rarity = TryGetInt32(rule, "rarity", out var parsedRarity) && Enum.IsDefined(typeof(Rarity), parsedRarity)
                ? (Rarity)parsedRarity
                : Rarity.Common;

            updatedRules[itemDefId] = new ItemRuleSnapshot(itemDefId, type, weight, slots, rarity);
        }

        if (updatedRules.Count > 0)
        {
            _itemRulesById = updatedRules;
        }
    }

    private static bool TryReadPlayerStats(JsonElement statsElement, out PlayerStats parsedStats)
    {
        var strength = TryGetInt32(statsElement, "strength", out var parsedStrength)
            ? parsedStrength
            : TryGetInt32(statsElement, "Strength", out var parsedStrengthUpper)
                ? parsedStrengthUpper
                : PlayerStatRules.MinimumScore;
        var dexterity = TryGetInt32(statsElement, "dexterity", out var parsedDexterity)
            ? parsedDexterity
            : TryGetInt32(statsElement, "Dexterity", out var parsedDexterityUpper)
                ? parsedDexterityUpper
                : PlayerStatRules.MinimumScore;
        var constitution = TryGetInt32(statsElement, "constitution", out var parsedConstitution)
            ? parsedConstitution
            : TryGetInt32(statsElement, "Constitution", out var parsedConstitutionUpper)
                ? parsedConstitutionUpper
                : PlayerStatRules.MinimumScore;
        var intelligence = TryGetInt32(statsElement, "intelligence", out var parsedIntelligence)
            ? parsedIntelligence
            : TryGetInt32(statsElement, "Intelligence", out var parsedIntelligenceUpper)
                ? parsedIntelligenceUpper
                : PlayerStatRules.MinimumScore;
        var wisdom = TryGetInt32(statsElement, "wisdom", out var parsedWisdom)
            ? parsedWisdom
            : TryGetInt32(statsElement, "Wisdom", out var parsedWisdomUpper)
                ? parsedWisdomUpper
                : PlayerStatRules.MinimumScore;
        var charisma = TryGetInt32(statsElement, "charisma", out var parsedCharisma)
            ? parsedCharisma
            : TryGetInt32(statsElement, "Charisma", out var parsedCharismaUpper)
                ? parsedCharismaUpper
                : PlayerStatRules.MinimumScore;

        parsedStats = new PlayerStats(strength, dexterity, constitution, intelligence, wisdom, charisma);
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

    private static bool TryGetNullableDateTimeOffset(JsonElement parent, string propertyName, out DateTimeOffset? value)
    {
        if (TryGetProjection(parent, propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Null)
            {
                value = null;
                return true;
            }

            if (property.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(property.GetString(), out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        value = default;
        return false;
    }

    private async Task ExecuteLootActionAsync(string action, Item item, string eventName)
    {
        await ExecuteRaidActionAsync(action, new { itemDefId = item.ItemDefId });
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
        return ItemPresentationCatalog.GetLabel(weapon) is { Length: > 0 } label
            ? label
            : ItemPresentationCatalog.GetLabel(ItemCatalog.GetByItemDefId(FallbackKnifeItemDefId));
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
        return ExecuteRaidActionAsync("drop-carried", new { itemDefId = item.ItemDefId });
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

    private int GetRaidEncumbrance()
    {
        if (_raid is null)
        {
            return 0;
        }

        return _raidEncumbrance ?? CombatBalance.GetTotalEncumbrance(_raid.Inventory.GetExtractableItems());
    }

    private int GetRaidMaxEncumbrance()
    {
        if (_raid is null)
        {
            return 0;
        }

        return _raidMaxEncumbrance ?? _raid.MaxEncumbrance;
    }

    private string GetRaidEncumbranceText()
    {
        return $"{GetRaidEncumbrance()}/{GetRaidMaxEncumbrance()} lbs";
    }

    private static bool IsEquipableRaidItem(Item item)
    {
        return item.Type is ItemType.Weapon or ItemType.Armor or ItemType.Backpack;
    }

    private bool CanEquipRaidItem(Item item)
    {
        if (_raid is null || !IsEquipableRaidItem(item))
        {
            return false;
        }

        var carriedItems = CurrentCarriedLoot.ToList();
        var carriedIndex = carriedItems.FindIndex(current => ReferenceEquals(current, item));
        var itemWasCarried = carriedIndex >= 0;
        if (carriedIndex >= 0)
        {
            carriedItems.RemoveAt(carriedIndex);
        }
        var currentEncumbrance = GetRaidEncumbrance();
        var projectedEncumbrance = currentEncumbrance;
        var replacedItem = GetEquippedItems().FirstOrDefault(existing => existing.Type == item.Type);
        if (replacedItem is not null)
        {
            projectedEncumbrance -= Math.Max(0, replacedItem.Weight);
        }

        if (item.Type == ItemType.Backpack)
        {
            var backpackCapacity = CombatBalance.GetBackpackCapacity(item);
            var currentSlots = carriedItems.Sum(x => x.Slots);
            var spilledWeight = 0;
            while (currentSlots > backpackCapacity && carriedItems.Count > 0)
            {
                var spill = carriedItems[^1];
                carriedItems.RemoveAt(carriedItems.Count - 1);
                currentSlots -= spill.Slots;
                spilledWeight += Math.Max(0, spill.Weight);
            }

            projectedEncumbrance -= spilledWeight;
        }

        if (!itemWasCarried)
        {
            projectedEncumbrance += Math.Max(0, item.Weight);
        }

        return projectedEncumbrance <= GetRaidMaxEncumbrance();
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

    private async Task GoDeeper()
    {
        if (_raid is null)
        {
            return;
        }

        await ExecuteRaidActionAsync("go-deeper", new { });
    }

    private async Task MoveTowardExtract()
    {
        if (_raid is null)
        {
            return;
        }

        await ExecuteRaidActionAsync("move-toward-extract", new { });
    }

    private async Task StartExtractHoldAsync()
    {
        if (_raid is null || IsExtractHoldEffectivelyActive() || _extractHoldResolutionInFlight)
        {
            return;
        }

        await ExecuteRaidActionAsync("start-extract-hold", new { });
    }

    private async Task CancelExtractHoldAsync()
    {
        if (_raid is null || !_extractHoldActive || _extractHoldResolutionInFlight)
        {
            return;
        }

        await ExecuteRaidActionAsync("cancel-extract-hold", new { });
    }

    private async Task ResolveExpiredExtractHoldAsync()
    {
        if (_raid is null || !HasExpiredExtractHold() || _extractHoldResolutionInFlight)
        {
            return;
        }

        _extractHoldResolutionInFlight = true;
        try
        {
            await ExecuteRaidActionAsync("resolve-extract-hold", new
            {
                holdAtExtractUntil = _holdAtExtractUntil?.ToString("O")
            });
        }
        finally
        {
            _extractHoldResolutionInFlight = false;
        }
    }

    private bool IsExtractHoldEffectivelyActive()
    {
        if (!_extractHoldActive)
        {
            return false;
        }

        return _holdAtExtractUntil is null || _holdAtExtractUntil > DateTimeOffset.UtcNow;
    }

    private bool HasExpiredExtractHold()
    {
        return _extractHoldActive
            && _holdAtExtractUntil is not null
            && _holdAtExtractUntil <= DateTimeOffset.UtcNow;
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

    private int GetReallocateStatCost()
    {
        return (int)Math.Round(_money / 2m, MidpointRounding.AwayFromZero);
    }

    private string GetReallocateStatCostLabel()
    {
        return $"${GetReallocateStatCost()}";
    }

    private int GetBuyPrice(Item item)
    {
        return CombatBalance.GetShopPrice(
            CombatBalance.GetBuyPrice(item),
            CombatBalance.GetCharismaModifier(_acceptedStats.Charisma),
            isBuying: true);
    }

    private bool CanBuyItem(Item item)
    {
        return item.Rarity <= CombatBalance.GetMaxShopRarityFromChaBonus(
            CombatBalance.GetCharismaModifier(_acceptedStats.Charisma));
    }

    private int GetSellPrice(Item item)
    {
        if (item.ItemDefId == FallbackKnifeItemDefId)
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

        var currentEncumbrance = GetRaidEncumbrance();
        if (currentEncumbrance + Math.Max(0, item.Weight) > GetRaidMaxEncumbrance())
        {
            return false;
        }

        if (item.ItemDefId == MedkitItemDefId)
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
            : new RandomCharacterState(snapshot.RandomCharacter.Name, [.. snapshot.RandomCharacter.Inventory], snapshot.RandomCharacter.Stats);
        _randomCharacterAvailableAt = snapshot.RandomCharacterAvailableAt;

        if (_randomCharacter is not null && _randomCharacter.Inventory.Count == 0)
        {
            _randomCharacter = null;
        }

        _money = snapshot.Money;
        _shopStock = snapshot.ShopStock
            .Select(offer => ItemCatalog.TryGetByItemDefId(offer.ItemDefId, out var item) && item is not null
                ? new ShopStock(offer, item)
                : null)
            .Where(stock => stock is not null)
            .Cast<ShopStock>()
            .ToList();
        _itemRulesById = snapshot.ItemRules.ToDictionary(rule => rule.ItemDefId);
        _acceptedStats = snapshot.AcceptedStats;
        _draftStats = snapshot.DraftStats;
        _availableStatPoints = snapshot.AvailableStatPoints;
        _statsAccepted = snapshot.StatsAccepted;
        _playerConstitution = snapshot.AcceptedStats.Constitution;
        _maxHealth = snapshot.PlayerMaxHealth;
        _onPersonItems = snapshot.OnPersonItems
            .Select(entry => new OnPersonEntry(entry.Item, entry.IsEquipped))
            .ToList();
        _raidEncumbrance = null;
        _raidMaxEncumbrance = null;

        if (snapshot.ActiveRaid is null)
        {
            _raid = null;
            _inRaid = false;
            _extractHoldActive = false;
            _holdAtExtractUntil = null;
            _extractHoldResolutionInFlight = false;
            _contactState = string.Empty;
            _surpriseSide = string.Empty;
            _initiativeWinner = string.Empty;
            _openingActionsRemaining = 0;
            _surprisePersistenceEligible = false;
            return;
        }

        ApplyActiveRaidSnapshot(snapshot.ActiveRaid);
    }

    private void ApplyActiveRaidSnapshot(RaidSnapshot snapshot)
    {
        var broughtItems = (snapshot.EquippedItems ?? []).ToList();
        var carriedItems = (snapshot.CarriedLoot ?? []).ToList();
        _raid = new RaidState(
            snapshot.Health,
            RaidInventory.FromItems(broughtItems, carriedItems, snapshot.BackpackCapacity));
        _raid.Inventory.DiscoveredLoot.Clear();
        _raid.Inventory.DiscoveredLoot.AddRange(snapshot.DiscoveredLoot ?? []);
        _raid.Inventory.MedkitCount = snapshot.Medkits;
        _raid.Inventory.BackpackCapacity = snapshot.BackpackCapacity;
        _raidEncumbrance = snapshot.Encumbrance > 0 || snapshot.MaxEncumbrance > 0 ? snapshot.Encumbrance : null;
        _raidMaxEncumbrance = snapshot.MaxEncumbrance > 0 ? snapshot.MaxEncumbrance : null;
        _extractHoldActive = snapshot.ExtractHoldActive;
        _holdAtExtractUntil = snapshot.HoldAtExtractUntil;
        if (_raidMaxEncumbrance is not null)
        {
            _raid.MaxEncumbrance = _raidMaxEncumbrance.Value;
        }
        _inRaid = true;
        _awaitingDecision = snapshot.AwaitingDecision;
        _challenge = snapshot.Challenge;
        _distanceFromExtract = snapshot.DistanceFromExtract;
        _ammo = snapshot.Ammo;
        _encounterDescription = RaidPresentationCatalog.GetEncounterDescription(
            snapshot.EncounterDescriptionKey,
            Enum.TryParse<EncounterType>(snapshot.EncounterType, ignoreCase: true, out var snapshotEncounterType)
                ? snapshotEncounterType
                : EncounterType.Neutral,
            snapshot.EncounterDescription,
            snapshot.ExtractHoldActive);
        _contactState = string.IsNullOrWhiteSpace(snapshot.ContactState) ? string.Empty : snapshot.ContactState;
        _surpriseSide = string.IsNullOrWhiteSpace(snapshot.SurpriseSide) ? string.Empty : snapshot.SurpriseSide;
        _initiativeWinner = string.IsNullOrWhiteSpace(snapshot.InitiativeWinner) ? string.Empty : snapshot.InitiativeWinner;
        _openingActionsRemaining = snapshot.OpeningActionsRemaining;
        _surprisePersistenceEligible = snapshot.SurprisePersistenceEligible;
        _enemyName = RaidPresentationCatalog.GetEnemyLabel(snapshot.EnemyKey, snapshot.EnemyName);
        _enemyHealth = snapshot.EnemyHealth;
        _enemyDexterity = snapshot.EnemyDexterity;
        _enemyConstitution = snapshot.EnemyConstitution;
        _enemyStrength = snapshot.EnemyStrength;
        _lootContainer = snapshot.LootContainer;
        _log.Clear();
        _log.AddRange((snapshot.LogEntries ?? []).Select(RaidPresentationCatalog.LocalizeLogEntry));

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
