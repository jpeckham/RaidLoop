using RaidLoop.Client.Services;
using RaidLoop.Core;

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

    private readonly Random _rng = new();
    private IRng CombatRng => new RandomRng(_rng);
    private readonly List<ShopStock> _shopStock =
    [
        new("Medkit", ItemType.Consumable),
        new("Makarov", ItemType.Weapon),
        new("Small Backpack", ItemType.Backpack)
    ];

    private GameState _mainGame = new([]);
    private GameState? _activeGame;
    private List<Item> _raidLoadout = [];
    private List<OnPersonEntry> _onPersonItems = [];
    private List<Item> _currentMainRaidBroughtItems = [];
    private int _money;

    private RandomCharacterState? _randomCharacter;
    private DateTimeOffset _randomCharacterAvailableAt = DateTimeOffset.MinValue;
    private System.Threading.Timer? _clockTimer;

    private RaidState? _raid;
    private bool _isLoading = true;
    private bool _inRaid;
    private bool _awaitingDecision;
    private RaidProfile _activeProfile = RaidProfile.Main;

    private EncounterType _encounterType = EncounterType.Neutral;
    private string _encounterDescription = string.Empty;

    private string _enemyName = string.Empty;
    private int _enemyHealth;
    private bool _extractionCombat;

    private string _lootContainer = string.Empty;
    private static readonly List<Item> EmptyItems = [];

    private int _ammo;
    private bool _weaponMalfunction;
    private int _extractProgress;
    private string _resultMessage = string.Empty;
    private string _activeRaiderName = "Main Character";
    private readonly List<string> _log = [];

    protected override async Task OnInitializedAsync()
    {
        var save = await Storage.LoadAsync();
        _mainGame = new GameState(save.MainStash);
        _randomCharacter = save.RandomCharacter;
        _randomCharacterAvailableAt = save.RandomCharacterAvailableAt;
        _money = save.Money;
        _onPersonItems = save.OnPersonItems;
        NormalizeEquippedSlots();
        EnsureMainCharacterHasWeaponFallback();

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
        var sellPrice = GetSellPrice(item.Name);
        if (sellPrice <= 0)
        {
            return;
        }

        _money += sellPrice;
        _mainGame.Stash.RemoveAt(stashIndex);
        EnsureMainCharacterHasWeaponFallback();
        await SaveAllAsync();
    }

    private async Task MoveStashToOnPersonAsync(int stashIndex)
    {
        if (stashIndex < 0 || stashIndex >= _mainGame.Stash.Count)
        {
            return;
        }

        var item = _mainGame.Stash[stashIndex];
        _mainGame.Stash.RemoveAt(stashIndex);
        var shouldEquip = ShouldEquipFromStash(item);
        if (shouldEquip)
        {
            var existing = FindEquippedIndexForSlot(item.Type);
            if (existing is not null)
            {
                var existingEntry = _onPersonItems[existing.Value];
                _onPersonItems[existing.Value] = existingEntry with { IsEquipped = false };
            }
        }

        _onPersonItems.Add(new OnPersonEntry(item, shouldEquip));
        NormalizeEquippedSlots();
        EnsureMainCharacterHasWeaponFallback();
        await SaveAllAsync();
    }

    private async Task SellOnPersonItemAsync(int onPersonIndex)
    {
        if (onPersonIndex < 0 || onPersonIndex >= _onPersonItems.Count)
        {
            return;
        }

        var item = _onPersonItems[onPersonIndex].Item;
        var sellPrice = GetSellPrice(item.Name);
        if (sellPrice <= 0)
        {
            return;
        }

        _money += sellPrice;
        _onPersonItems.RemoveAt(onPersonIndex);
        EnsureMainCharacterHasWeaponFallback();
        await SaveAllAsync();
    }

    private async Task StashOnPersonItemAsync(int onPersonIndex)
    {
        if (!CanStashOnPersonItem || onPersonIndex < 0 || onPersonIndex >= _onPersonItems.Count)
        {
            return;
        }

        var item = _onPersonItems[onPersonIndex].Item;
        _onPersonItems.RemoveAt(onPersonIndex);
        _mainGame.Stash.Add(item);
        NormalizeEquippedSlots();
        await SaveAllAsync();
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

        var existing = FindEquippedIndexForSlot(selected.Item.Type);
        if (existing is not null)
        {
            var existingEntry = _onPersonItems[existing.Value];
            _onPersonItems[existing.Value] = existingEntry with { IsEquipped = false };
        }

        _onPersonItems[onPersonIndex] = selected with { IsEquipped = true };
        NormalizeEquippedSlots();
        await SaveAllAsync();
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

        _onPersonItems[onPersonIndex] = current with { IsEquipped = false };
        NormalizeEquippedSlots();
        await SaveAllAsync();
    }

    private async Task BuyFromShopAsync(ShopStock stock)
    {
        var price = GetBuyPrice(stock.Name);
        if (_money < price)
        {
            return;
        }

        _money -= price;
        _onPersonItems.Add(new OnPersonEntry(new Item(stock.Name, stock.Type, 1), false));
        NormalizeEquippedSlots();
        await SaveAllAsync();
    }

    private async Task StartMainRaidAsync()
    {
        if (!CanStartMainRaid)
        {
            return;
        }

        _activeProfile = RaidProfile.Main;
        _activeRaiderName = "Main Character";
        _raidLoadout = _onPersonItems.Select(x => x.Item).ToList();
        _activeGame = new GameState([.. _raidLoadout]);
        _currentMainRaidBroughtItems = [.. _raidLoadout];

        await StartRaidAsync(GetBackpackCapacity(_raidLoadout));
    }

    private async Task StartRandomRaidAsync()
    {
        if (!CanStartLuckRunRaid)
        {
            return;
        }

        _randomCharacter = new RandomCharacterState(
            Name: GenerateRandomName(),
            Inventory: GenerateRandomLoadout());

        _activeProfile = RaidProfile.Random;
        _activeRaiderName = _randomCharacter.Name;
        _activeGame = new GameState(_randomCharacter.Inventory);
        _raidLoadout = [.. _activeGame.Stash];

        await StartRaidAsync(GetBackpackCapacity(_raidLoadout));
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

        var item = _randomCharacter.Inventory[luckIndex];
        _mainGame.Stash.Add(item);
        _randomCharacter.Inventory.RemoveAt(luckIndex);
        EnsureMainCharacterHasWeaponFallback();
        await CompleteLuckRunSettlementIfDoneAsync();
    }

    private async Task MoveLuckRunItemToForRaidAsync(int luckIndex)
    {
        if (_randomCharacter is null || luckIndex < 0 || luckIndex >= _randomCharacter.Inventory.Count)
        {
            return;
        }

        var item = _randomCharacter.Inventory[luckIndex];
        _randomCharacter.Inventory.RemoveAt(luckIndex);
        _onPersonItems.Add(new OnPersonEntry(item, ShouldEquipFromStash(item)));
        EnsureMainCharacterHasWeaponFallback();
        NormalizeEquippedSlots();
        await CompleteLuckRunSettlementIfDoneAsync();
    }

    private async Task SellLuckRunItemAsync(int luckIndex)
    {
        if (_randomCharacter is null || luckIndex < 0 || luckIndex >= _randomCharacter.Inventory.Count)
        {
            return;
        }

        var item = _randomCharacter.Inventory[luckIndex];
        var sellPrice = GetSellPrice(item.Name);
        if (sellPrice <= 0)
        {
            return;
        }

        _money += sellPrice;
        _randomCharacter.Inventory.RemoveAt(luckIndex);
        await CompleteLuckRunSettlementIfDoneAsync();
    }

    private async Task CompleteLuckRunSettlementIfDoneAsync()
    {
        if (_randomCharacter is not null && _randomCharacter.Inventory.Count == 0)
        {
            _randomCharacter = null;
            _randomCharacterAvailableAt = DateTimeOffset.UtcNow.Add(LuckRunCooldown);
        }

        await SaveAllAsync();
    }

    private async Task StartRaidAsync(int backpackCapacity)
    {
        if (_activeGame is null)
        {
            return;
        }

        _resultMessage = string.Empty;
        _raid = RaidEngine.StartRaid(_activeGame, _raidLoadout, backpackCapacity, MaxHealth);
        _inRaid = true;
        _awaitingDecision = false;
        _extractProgress = 0;
        _ammo = CurrentMagazineCapacity;
        _weaponMalfunction = false;
        _log.Clear();
        _log.Add($"Raid started as {_activeRaiderName}.");

        await SaveAllAsync();
        GenerateEncounter(movingToExtract: false);
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

    private string GenerateRandomName()
    {
        var names = new[] { "Ghost", "Moth", "Brick", "Vex", "Nail", "Echo" };
        return $"{names[_rng.Next(names.Length)]}-{_rng.Next(100, 999)}";
    }

    private List<Item> GenerateRandomLoadout()
    {
        var backpacks = new[]
        {
            new Item("Small Backpack", ItemType.Backpack, 1),
            new Item("Tactical Backpack", ItemType.Backpack, 1)
        };

        return
        [
            new Item("Makarov", ItemType.Weapon, 1),
            backpacks[_rng.Next(backpacks.Length)],
            new Item("Medkit", ItemType.Consumable, 1),
            new Item("Bandage", ItemType.Sellable, 1),
            new Item("Ammo Box", ItemType.Sellable, 1)
        ];
    }

    private int GetBackpackCapacity(IEnumerable<Item> items)
    {
        var backpack = items.FirstOrDefault(i => i.Type == ItemType.Backpack);
        return backpack?.Name switch
        {
            "Tactical Backpack" => 6,
            "Small Backpack" => 3,
            _ => 2
        };
    }

    private int GetArmorReduction()
    {
        var armor = _raid?.Inventory.EquippedArmor;
        return armor is null ? 0 : CombatBalance.GetArmorReduction(armor.Name);
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

    private void GenerateEncounter(bool movingToExtract)
    {
        _awaitingDecision = false;
        if (_raid is not null)
        {
            RaidEngine.ClearDiscoveredLoot(_raid);
        }
        _extractionCombat = false;

        if (movingToExtract)
        {
            _extractProgress++;
        }

        if (_extractProgress >= ExtractRequired && _rng.Next(100) < 35)
        {
            _encounterType = EncounterType.Extraction;
            _encounterDescription = "You are near the extraction route.";
            _log.Add("Extraction point located.");
            return;
        }

        var roll = _rng.Next(100);
        if (roll < 50)
        {
            _encounterType = EncounterType.Combat;
            _enemyName = _rng.Next(100) < 65 ? "Scav" : "Patrol Guard";
            _enemyHealth = _rng.Next(12, 21);
            _encounterDescription = "Enemy contact on your position.";
            _log.Add($"Combat started vs {_enemyName}.");
            return;
        }

        if (roll < 80)
        {
            var container = GetRandomLootContainer();
            var discovered = GenerateLootItems(_rng.Next(2, 5));
            ShowDiscoveredLoot(container, discovered, "A searchable container appears.");
            return;
        }

        _encounterType = EncounterType.Neutral;
        _encounterDescription = "Area looks quiet. Nothing useful here.";
        _log.Add("No enemies or loot found.");
    }

    private string GetRandomLootContainer()
    {
        var options = new[] { "Filing Cabinet", "Weapons Crate", "Medical Container", "Dead Body" };
        return options[_rng.Next(options.Length)];
    }

    private Item GenerateLootItem()
    {
        var roll = _rng.Next(100);
        if (roll < 50)
        {
            var commonLoot = new List<Item>
            {
                new("Bandage", ItemType.Sellable, 1),
                new("Ammo Box", ItemType.Sellable, 1),
                new("Scrap Metal", ItemType.Material, 1),
                new("Rare Scope", ItemType.Material, 1)
            };

            return commonLoot[_rng.Next(commonLoot.Count)];
        }

        if (roll < 80)
        {
            var uncommonLoot = new List<Item>
            {
                new("Medkit", ItemType.Consumable, 1),
                new("PPSH", ItemType.Weapon, 1),
                new("6B2 body armor", ItemType.Armor, 1),
                new("Legendary Trigger Group", ItemType.Material, 1)
            };

            return uncommonLoot[_rng.Next(uncommonLoot.Count)];
        }

        if (roll < 95)
        {
            var rareLoot = new List<Item>
            {
                new("AK74", ItemType.Weapon, 1),
                new("6B13 assault armor", ItemType.Armor, 1)
            };

            return rareLoot[_rng.Next(rareLoot.Count)];
        }

        var veryRareLoot = new List<Item>
        {
            new("AK47", ItemType.Weapon, 1),
            new("6B43 Zabralo-Sh body armor", ItemType.Armor, 1)
        };

        return veryRareLoot[_rng.Next(veryRareLoot.Count)];
    }

    private List<Item> GenerateLootItems(int count)
    {
        var items = new List<Item>();
        for (var i = 0; i < count; i++)
        {
            items.Add(GenerateLootItem());
        }

        return items;
    }

    private async Task AttackAsync()
    {
        if (_raid is null || _encounterType != EncounterType.Combat)
        {
            return;
        }

        if (!CanAttack)
        {
            _log.Add(_weaponMalfunction ? "Weapon is malfunctioned. Reload to clear it." : "No ammo.");
            return;
        }

        if (TryTriggerMalfunction())
        {
            _log.Add("Weapon malfunctioned. Reload to clear it.");
            await EnemyTurnAsync();
            return;
        }

        if (EquippedWeaponUsesAmmo)
        {
            _ammo--;
        }
        var damage = CombatBalance.RollDamage(GetEquippedWeaponName(), AttackMode.Standard, CombatRng);
        _enemyHealth -= damage;
        _log.Add($"You hit {_enemyName} for {damage}.");

        if (_enemyHealth <= 0)
        {
            await ResolveCombatVictoryAsync();
            return;
        }

        await EnemyTurnAsync();
    }

    private async Task BurstFireAsync()
    {
        if (_raid is null || _encounterType != EncounterType.Combat)
        {
            return;
        }

        if (!CanBurstFire)
        {
            _log.Add(_weaponMalfunction ? "Weapon is malfunctioned. Reload to clear it." : "Not enough ammo for Burst Fire.");
            return;
        }

        if (TryTriggerMalfunction())
        {
            _log.Add("Weapon malfunctioned during Burst Fire. Reload to clear it.");
            await EnemyTurnAsync();
            return;
        }

        _ammo -= 2;
        var damage = CombatBalance.RollDamage(GetEquippedWeaponName(), AttackMode.Burst, CombatRng);
        _enemyHealth -= damage;
        _log.Add($"Burst Fire deals {damage}.");

        if (_enemyHealth <= 0)
        {
            await ResolveCombatVictoryAsync();
            return;
        }

        await EnemyTurnAsync();
    }

    private async Task UseMedkitAsync()
    {
        if (_raid is null)
        {
            return;
        }

        if (_raid.Inventory.MedkitCount <= 0)
        {
            return;
        }

        _raid.Inventory.MedkitCount--;
        _raid.Health = Math.Min(MaxHealth, _raid.Health + 10);
        _log.Add("Medkit used (+10 HP).");
        if (_encounterType == EncounterType.Combat)
        {
            await EnemyTurnAsync();
        }
    }

    private async Task ReloadAsync()
    {
        if (_encounterType != EncounterType.Combat)
        {
            return;
        }

        if (!EquippedWeaponUsesAmmo)
        {
            _log.Add("Knife doesn't need reloading.");
            await EnemyTurnAsync();
            return;
        }

        _ammo = CurrentMagazineCapacity;
        _weaponMalfunction = false;
        _log.Add("Weapon reloaded and cleared.");
        await EnemyTurnAsync();
    }

    private async Task FleeAsync()
    {
        if (_encounterType != EncounterType.Combat)
        {
            return;
        }

        if (_rng.Next(100) < 15)
        {
            _log.Add("Flee succeeded.");
            EnterDecisionState();
            return;
        }

        _log.Add("Flee failed.");
        await EnemyTurnAsync();
    }

    private async Task EnemyTurnAsync()
    {
        if (_raid is null)
        {
            return;
        }

        var incoming = _rng.Next(3, 9);
        var reduced = CombatBalance.ApplyArmorReduction(incoming, GetArmorReduction());
        RaidEngine.ApplyCombatDamage(_raid, reduced);
        _log.Add($"{_enemyName} hits you for {reduced}.");

        if (_raid.IsDead)
        {
            await EndRaidAsync(false, "You were killed in raid. Loadout and loot lost.");
        }
    }

    private async Task ResolveCombatVictoryAsync()
    {
        if (_raid is null)
        {
            return;
        }

        if (_extractionCombat)
        {
            await EndRaidAsync(true, "Final guard defeated. Extraction successful.");
            return;
        }

        var drop = GenerateLootItem();
        ShowDiscoveredLoot("Dead Body", [drop], "Enemy down. Check the body for loot.");
    }

    private Task TakeLootAsync(Item lootItem)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }

        if (RaidEngine.TryLootFromDiscovered(_raid, lootItem))
        {
            _log.Add($"Looted {lootItem.Name}.");
        }
        else
        {
            _log.Add($"Could not loot {lootItem.Name}: backpack full.");
        }

        return Task.CompletedTask;
    }

    private Task DropCarriedAsync(Item item)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }

        if (RaidEngine.TryDropCarriedToDiscovered(_raid, item))
        {
            _log.Add($"Dropped {item.Name}.");
        }

        return Task.CompletedTask;
    }

    private Task DropEquippedAsync(ItemType slotType)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }

        if (RaidEngine.TryDropEquippedToDiscovered(_raid, slotType))
        {
            _log.Add($"Dropped equipped {slotType}.");
            if (slotType == ItemType.Weapon)
            {
                _weaponMalfunction = false;
            }
        }

        return Task.CompletedTask;
    }

    private Task EquipFromDiscoveredAsync(Item item)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }

        if (RaidEngine.TryEquipFromDiscovered(_raid, item))
        {
            _log.Add($"Equipped {item.Name} from discovered loot.");
            if (item.Type == ItemType.Weapon)
            {
                _ammo = Math.Min(_ammo, CurrentMagazineCapacity);
                _weaponMalfunction = false;
            }
        }

        return Task.CompletedTask;
    }

    private Task EquipFromCarriedAsync(Item item)
    {
        if (_raid is null)
        {
            return Task.CompletedTask;
        }

        if (RaidEngine.TryEquipFromCarried(_raid, item))
        {
            _log.Add($"Equipped {item.Name} from carried loot.");
            if (item.Type == ItemType.Weapon)
            {
                _ammo = Math.Min(_ammo, CurrentMagazineCapacity);
                _weaponMalfunction = false;
            }
        }

        return Task.CompletedTask;
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

    private void ShowDiscoveredLoot(string container, IEnumerable<Item> discoveredItems, string description)
    {
        if (_raid is null)
        {
            return;
        }

        var items = discoveredItems.ToList();
        var shouldAppend = _encounterType == EncounterType.Loot && CurrentDiscoveredLoot.Count > 0;
        if (shouldAppend)
        {
            RaidEngine.AppendDiscoveredLoot(_raid, items);
            _log.Add($"More loot discovered in {container}: {string.Join(", ", items.Select(x => x.Name))}.");
        }
        else
        {
            RaidEngine.StartDiscoveredLootEncounter(_raid, items);
            _log.Add($"Found {container} with {CurrentDiscoveredLoot.Count} lootable items.");
        }

        _encounterType = EncounterType.Loot;
        _lootContainer = container;
        _encounterDescription = description;
        _awaitingDecision = false;
    }

    private async Task AttemptExtractAsync()
    {
        if (_raid is null)
        {
            return;
        }

        if (_rng.Next(100) < 50)
        {
            _encounterType = EncounterType.Combat;
            _encounterDescription = "Extraction ambush engaged.";
            _enemyName = "Final Guard";
            _enemyHealth = 22;
            _extractionCombat = true;
            _log.Add("Extraction ambush.");
            return;
        }

        await EndRaidAsync(true, "Extraction completed. Loot secured.");
    }

    private void ContinueSearching()
    {
        AbandonRemainingLootIfAny();
        GenerateEncounter(movingToExtract: false);
    }

    private void MoveTowardExtract()
    {
        AbandonRemainingLootIfAny();
        GenerateEncounter(movingToExtract: true);
    }

    private async Task EndRaidAsync(bool extracted, string message)
    {
        if (_raid is null || _activeGame is null)
        {
            return;
        }

        if (_activeProfile == RaidProfile.Main)
        {
            foreach (var prior in _currentMainRaidBroughtItems)
            {
                var idx = _onPersonItems.FindIndex(x => x.Item == prior);
                if (idx >= 0)
                {
                    _onPersonItems.RemoveAt(idx);
                }
            }

            if (extracted)
            {
                foreach (var extractedItem in _raid.Inventory.GetExtractableItems())
                {
                    var shouldBeEquipped = extractedItem == _raid.Inventory.EquippedWeapon
                        || extractedItem == _raid.Inventory.EquippedArmor
                        || extractedItem == _raid.Inventory.EquippedBackpack;
                    _onPersonItems.Add(new OnPersonEntry(extractedItem, shouldBeEquipped));
                }
            }

            NormalizeEquippedSlots();
            EnsureMainCharacterHasWeaponFallback();
            _currentMainRaidBroughtItems.Clear();
        }
        else
        {
            RaidEngine.FinalizeRaid(_activeGame, _raid, extracted);
            if (extracted && _randomCharacter is not null)
            {
                _randomCharacter = _randomCharacter with { Inventory = [.. _activeGame.Stash] };
            }
            else
            {
                _randomCharacter = null;
                _randomCharacterAvailableAt = DateTimeOffset.UtcNow.Add(LuckRunCooldown);
            }
        }

        await SaveAllAsync();

        _resultMessage = message;
        _inRaid = false;
        _awaitingDecision = false;
        _raid = null;
        _activeGame = null;
    }

    private void EnterDecisionState()
    {
        _awaitingDecision = true;
        _encounterDescription = "Choose your next move.";
    }

    private int GetLootSlotCount()
    {
        return _raid?.Inventory.CarriedItems.Sum(x => x.Slots) ?? 0;
    }

    private int GetBuyPrice(string itemName)
    {
        return CombatBalance.GetBuyPrice(itemName);
    }

    private int GetSellPrice(string itemName)
    {
        if (string.Equals(itemName, FallbackKnifeName, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return (int)Math.Floor(GetBuyPrice(itemName) * 0.6);
    }

    private bool CanSellItem(Item item)
    {
        return GetSellPrice(item.Name) > 0;
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

    private void AbandonRemainingLootIfAny()
    {
        if (_encounterType == EncounterType.Loot && CurrentDiscoveredLoot.Count > 0)
        {
            _log.Add($"Moved on and left {CurrentDiscoveredLoot.Count} items behind.");
            if (_raid is not null)
            {
                RaidEngine.ClearDiscoveredLoot(_raid);
            }
        }
    }

    private bool TryTriggerMalfunction()
    {
        if (_weaponMalfunction)
        {
            return true;
        }

        if (_rng.Next(100) < 10)
        {
            _weaponMalfunction = true;
            return true;
        }

        return false;
    }

    private void EnsureMainCharacterHasWeaponFallback()
    {
        var stashHasWeapon = _mainGame.Stash.Any(item => item.Type == ItemType.Weapon);
        var onPersonHasWeapon = _onPersonItems.Any(entry => entry.Item.Type == ItemType.Weapon);
        if (!stashHasWeapon && !onPersonHasWeapon)
        {
            _mainGame.Stash.Add(new Item("Rusty Knife", ItemType.Weapon, 1));
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

    private async Task SaveAllAsync()
    {
        await Storage.SaveAsync(new GameSave(
            MainStash: [.. _mainGame.Stash],
            RandomCharacterAvailableAt: _randomCharacterAvailableAt,
            RandomCharacter: _randomCharacter is null
                ? null
                : new RandomCharacterState(_randomCharacter.Name, [.. _randomCharacter.Inventory]),
            Money: _money,
            OnPersonItems: [.. _onPersonItems]));
    }

    public void Dispose()
    {
        _clockTimer?.Dispose();
    }

    private enum RaidProfile
    {
        Main,
        Random
    }
}
