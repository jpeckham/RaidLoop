using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

public class RaidEngineTests
{
    [Fact]
    public void PlayerStatRules_DefaultAllocation_StartsAtEightWithTwentySevenPoints()
    {
        var allocation = PlayerStatAllocation.CreateDefault();

        Assert.Equal(8, allocation.Stats.Strength);
        Assert.Equal(8, allocation.Stats.Dexterity);
        Assert.Equal(8, allocation.Stats.Constitution);
        Assert.Equal(8, allocation.Stats.Intelligence);
        Assert.Equal(8, allocation.Stats.Wisdom);
        Assert.Equal(8, allocation.Stats.Charisma);
        Assert.Equal(27, allocation.AvailablePoints);
    }

    [Theory]
    [InlineData(8, 1)]
    [InlineData(12, 1)]
    [InlineData(13, 2)]
    [InlineData(14, 2)]
    [InlineData(15, 3)]
    [InlineData(16, 3)]
    [InlineData(17, 4)]
    public void PlayerStatRules_RaiseCost_FollowsEscalatingPointBuy(int currentScore, int expectedCost)
    {
        Assert.Equal(expectedCost, PlayerStatRules.GetRaiseCost(currentScore));
    }

    [Theory]
    [InlineData(9, 1)]
    [InlineData(13, 1)]
    [InlineData(14, 2)]
    [InlineData(15, 2)]
    [InlineData(16, 3)]
    [InlineData(17, 3)]
    [InlineData(18, 4)]
    public void PlayerStatRules_LowerRefund_MirrorsPurchaseCost(int currentScore, int expectedRefund)
    {
        Assert.Equal(expectedRefund, PlayerStatRules.GetLowerRefund(currentScore));
    }

    [Theory]
    [InlineData("Makarov", AttackMode.Standard, 2, 12)]
    [InlineData("PPSH", AttackMode.Standard, 2, 8)]
    [InlineData("AK74", AttackMode.Standard, 2, 16)]
    [InlineData("SVDS", AttackMode.Standard, 2, 24)]
    [InlineData("AK47", AttackMode.Standard, 2, 20)]
    [InlineData("PPSH", AttackMode.Burst, 3, 12)]
    [InlineData("AK74", AttackMode.Burst, 3, 24)]
    [InlineData("SVDS", AttackMode.Burst, 3, 36)]
    [InlineData("AK47", AttackMode.Burst, 3, 30)]
    [InlineData("PKP", AttackMode.Burst, 3, 36)]
    [InlineData("PPSH", AttackMode.FullAuto, 4, 16)]
    [InlineData("AK74", AttackMode.FullAuto, 4, 32)]
    [InlineData("AK47", AttackMode.FullAuto, 4, 40)]
    [InlineData("PKP", AttackMode.FullAuto, 4, 48)]
    public void CombatBalance_WeaponDamageProfiles_AreConfigured(string weapon, AttackMode mode, int min, int max)
    {
        var range = CombatBalance.GetDamageRange(weapon, mode);

        Assert.Equal(min, range.Min);
        Assert.Equal(max, range.Max);
    }

    [Theory]
    [InlineData("6B2 body armor", 1)]
    [InlineData("BNTI Kirasa-N", 2)]
    [InlineData("6B13 assault armor", 3)]
    [InlineData("FORT Defender-2", 4)]
    [InlineData("6B43 Zabralo-Sh body armor", 5)]
    [InlineData("NFM THOR", 6)]
    [InlineData("Unknown armor", 0)]
    public void CombatBalance_ArmorReduction_ByQuality(string armorName, int reduction)
    {
        Assert.Equal(reduction, CombatBalance.GetArmorReduction(armorName));
    }

    [Fact]
    public void CombatBalance_RollDamage_UsesInjectableRng()
    {
        var rng = new SequenceRng([0, 1, 0, 1, 2, 3]);

        var makarov = CombatBalance.RollDamage("Makarov", AttackMode.Standard, rng);
        var ak47 = CombatBalance.RollDamage("AK47", AttackMode.FullAuto, rng);

        Assert.Equal(3, makarov);
        Assert.Equal(10, ak47);
    }

    [Theory]
    [InlineData("Makarov", true, true, false)]
    [InlineData("PPSH", true, true, true)]
    [InlineData("AK74", true, true, true)]
    [InlineData("AK47", true, true, true)]
    [InlineData("SVDS", true, true, false)]
    [InlineData("PKP", false, true, true)]
    [InlineData("Rusty Knife", true, false, false)]
    public void CombatBalance_WeaponFireModes_AreConfigured(string weapon, bool supportsSingle, bool supportsBurst, bool supportsFullAuto)
    {
        Assert.Equal(supportsSingle, CombatBalance.SupportsSingleShot(weapon));
        Assert.Equal(supportsBurst, CombatBalance.SupportsBurstFire(weapon));
        Assert.Equal(supportsFullAuto, CombatBalance.SupportsFullAuto(weapon));
    }

    [Theory]
    [InlineData("Makarov", 3)]
    [InlineData("PPSH", 2)]
    [InlineData("AK74", 2)]
    [InlineData("AK47", 2)]
    [InlineData("SVDS", 2)]
    [InlineData("PKP", 2)]
    [InlineData("Rusty Knife", 3)]
    public void CombatBalance_BurstAttackPenalty_IsConfiguredPerWeapon(string weapon, int expectedPenalty)
    {
        Assert.Equal(expectedPenalty, CombatBalance.GetBurstAttackPenalty(weapon));
    }

    [Theory]
    [InlineData(2, 5, 1)]
    [InlineData(9, 3, 6)]
    [InlineData(12, 0, 12)]
    public void CombatBalance_ApplyArmorReduction_HasMinimumFloor(int incomingDamage, int armorReduction, int expected)
    {
        var mitigated = CombatBalance.ApplyArmorReduction(incomingDamage, armorReduction);

        Assert.Equal(expected, mitigated);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(14, 2)]
    [InlineData(18, 4)]
    [InlineData(9, -1)]
    public void CombatBalance_AbilityModifier_UsesD20Flooring(int score, int expected)
    {
        Assert.Equal(expected, CombatBalance.GetAbilityModifier(score));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 30)]
    [InlineData(12, 34)]
    public void CombatBalance_MaxHealth_UsesConstitutionRule(int constitution, int expectedMaxHealth)
    {
        Assert.Equal(expectedMaxHealth, CombatBalance.GetMaxHealthFromConstitution(constitution));
    }

    [Fact]
    public void CombatBalance_HigherDexterity_IncreasesRangedAttackBonus()
    {
        var lowDexBonus = CombatBalance.GetRangedAttackBonusFromDexterity(10);
        var highDexBonus = CombatBalance.GetRangedAttackBonusFromDexterity(14);

        Assert.Equal(0, lowDexBonus);
        Assert.Equal(2, highDexBonus);
        Assert.True(highDexBonus > lowDexBonus);
    }

    [Fact]
    public void CombatBalance_HigherDexterity_IncreasesDefense()
    {
        var lowDexDefense = CombatBalance.GetDefenseFromDexterity(10);
        var highDexDefense = CombatBalance.GetDefenseFromDexterity(14);

        Assert.Equal(10, lowDexDefense);
        Assert.Equal(12, highDexDefense);
        Assert.True(highDexDefense > lowDexDefense);
    }

    [Fact]
    public void CombatBalance_DefenseFromDexterity_RespectsArmorCap()
    {
        var uncapped = CombatBalance.GetDefenseFromDexterity(18, maxDexBonus: null);
        var capped = CombatBalance.GetDefenseFromDexterity(18, maxDexBonus: 2);

        Assert.Equal(14, uncapped);
        Assert.Equal(12, capped);
    }

    [Fact]
    public void CombatBalance_SameAttackRoll_CanMissLowDex_AndHitHighDex()
    {
        const int roll = 11;
        const int defense = 13;

        var lowDexHit = CombatBalance.ResolveAttackRoll(roll, CombatBalance.GetRangedAttackBonusFromDexterity(10), defense);
        var highDexHit = CombatBalance.ResolveAttackRoll(roll, CombatBalance.GetRangedAttackBonusFromDexterity(14), defense);

        Assert.False(lowDexHit);
        Assert.True(highDexHit);
    }

    [Fact]
    public void CombatBalance_SameEnemyAttackRoll_CanHitLowDex_AndMissHighDex()
    {
        const int roll = 11;
        const int attackBonus = 0;

        var lowDexHit = CombatBalance.ResolveAttackRoll(roll, attackBonus, CombatBalance.GetDefenseFromDexterity(10));
        var highDexHit = CombatBalance.ResolveAttackRoll(roll, attackBonus, CombatBalance.GetDefenseFromDexterity(14));

        Assert.True(lowDexHit);
        Assert.False(highDexHit);
    }

    [Fact]
    public void CombatBalance_NaturalOne_AlwaysMisses()
    {
        var hit = CombatBalance.ResolveAttackRoll(1, 100, 1);

        Assert.False(hit);
    }

    [Fact]
    public void CombatBalance_NaturalTwenty_AlwaysHits()
    {
        var hit = CombatBalance.ResolveAttackRoll(20, -100, 999);

        Assert.True(hit);
    }

    [Theory]
    [InlineData("Makarov", 240)]
    [InlineData("PPSH", 650)]
    [InlineData("AK74", 1250)]
    [InlineData("SVDS", 2200)]
    [InlineData("AK47", 1500)]
    [InlineData("PKP", 3200)]
    [InlineData("6B2 body armor", 380)]
    [InlineData("BNTI Kirasa-N", 640)]
    [InlineData("6B13 assault armor", 900)]
    [InlineData("FORT Defender-2", 1500)]
    [InlineData("6B43 Zabralo-Sh body armor", 1800)]
    [InlineData("NFM THOR", 2600)]
    [InlineData("Small Backpack", 100)]
    [InlineData("Large Backpack", 200)]
    [InlineData("Tasmanian Tiger Trooper 35", 1600)]
    [InlineData("6Sh118", 2400)]
    public void CombatBalance_Prices_AreConfigured(string itemName, int buyPrice)
    {
        Assert.Equal(buyPrice, CombatBalance.GetBuyPrice(itemName));
    }

    [Theory]
    [InlineData("Makarov", 8)]
    [InlineData("PPSH", 35)]
    [InlineData("AK74", 30)]
    [InlineData("SVDS", 20)]
    [InlineData("AK47", 30)]
    [InlineData("PKP", 100)]
    [InlineData("Rusty Knife", 0)]
    [InlineData("Unknown Weapon", 8)]
    public void CombatBalance_AmmoCapacity_ByWeapon(string weaponName, int capacity)
    {
        Assert.Equal(capacity, CombatBalance.GetMagazineCapacity(weaponName));
    }

    [Theory]
    [InlineData("Rusty Knife", false)]
    [InlineData("Makarov", true)]
    [InlineData("PPSH", true)]
    [InlineData("AK74", true)]
    [InlineData("SVDS", true)]
    [InlineData("AK47", true)]
    [InlineData("PKP", true)]
    public void CombatBalance_WeaponAmmoUsage_ByWeapon(string weaponName, bool usesAmmo)
    {
        Assert.Equal(usesAmmo, CombatBalance.WeaponUsesAmmo(weaponName));
    }

    [Theory]
    [InlineData("Small Backpack", 3)]
    [InlineData("Large Backpack", 4)]
    [InlineData("Tactical Backpack", 6)]
    [InlineData("Tasmanian Tiger Trooper 35", 8)]
    [InlineData("6Sh118", 10)]
    [InlineData(null, 2)]
    public void CombatBalance_BackpackCapacity_ByBackpack(string? backpackName, int capacity)
    {
        Assert.Equal(capacity, CombatBalance.GetBackpackCapacity(backpackName));
    }

    [Fact]
    public void CombatBalance_StrengthCarryCapacity_ProvidesFutureEncumbranceSeam()
    {
        var lowStrengthCapacity = CombatBalance.GetCarryCapacityFromStrength(8);
        var highStrengthCapacity = CombatBalance.GetCarryCapacityFromStrength(18);

        Assert.True(highStrengthCapacity > lowStrengthCapacity);
    }

    [Fact]
    public void CombatBalance_StrengthMaxEncumbrance_GrowsWithStrength()
    {
        var lowStrengthLimit = CombatBalance.GetMaxEncumbranceFromStrength(8);
        var highStrengthLimit = CombatBalance.GetMaxEncumbranceFromStrength(18);

        Assert.Equal(40, lowStrengthLimit);
        Assert.Equal(90, highStrengthLimit);
        Assert.True(highStrengthLimit > lowStrengthLimit);
    }

    [Fact]
    public void CombatBalance_TotalEncumbrance_IncludesItemsAndMedkits()
    {
        var items = new[]
        {
            ItemCatalog.Get("Rusty Knife"),
            ItemCatalog.Get("Makarov"),
            ItemCatalog.Get("6B2 body armor"),
            ItemCatalog.Get("Medkit"),
            ItemCatalog.Get("Medkit")
        };

        var totalEncumbrance = CombatBalance.GetTotalEncumbrance(items);

        Assert.Equal(20, totalEncumbrance);
    }

    [Theory]
    [InlineData(8, 240)]
    [InlineData(12, 228)]
    [InlineData(18, 192)]
    public void CombatBalance_CharismaMod_ReducesBuyPrices(int charisma, int expectedPrice)
    {
        var price = CombatBalance.GetShopPrice(240, charismaModifier: CombatBalance.GetCharismaModifier(charisma), isBuying: true);

        Assert.Equal(expectedPrice, price);
    }

    [Theory]
    [InlineData(8, Rarity.Common)]
    [InlineData(12, Rarity.Uncommon)]
    [InlineData(14, Rarity.Rare)]
    [InlineData(16, Rarity.Epic)]
    [InlineData(18, Rarity.Legendary)]
    public void CombatBalance_CharismaMod_UnlocksExpectedShopRarity(int charisma, Rarity expectedRarity)
    {
        var maxRarity = CombatBalance.GetMaxShopRarityFromChaBonus(CombatBalance.GetCharismaModifier(charisma));

        Assert.Equal(expectedRarity, maxRarity);
    }

    [Fact]
    public void EncounterLoot_StartLootEncounter_ClearsAndAddsNewItems()
    {
        var discovered = new List<Item>
        {
            new("Old Loot", ItemType.Sellable, Weight: 1, Slots: 1)
        };

        EncounterLoot.StartLootEncounter(discovered, [new Item("New Loot", ItemType.Material, Weight: 1, Slots: 1)]);

        Assert.Single(discovered);
        Assert.Equal("New Loot", discovered[0].Name);
    }

    [Fact]
    public void EncounterLoot_AppendDiscoveredLoot_AddsWithoutClearing()
    {
        var discovered = new List<Item>
        {
            new("First", ItemType.Material, Weight: 1, Slots: 1)
        };

        EncounterLoot.AppendDiscoveredLoot(discovered, [new Item("Second", ItemType.Weapon, Weight: 1, Slots: 1)]);

        Assert.Equal(2, discovered.Count);
        Assert.Equal("First", discovered[0].Name);
        Assert.Equal("Second", discovered[1].Name);
    }

    [Fact]
    public void ApplyCombatDamage_ReducesHealth_AndMarksDeathAtZero()
    {
        var state = new RaidState(
            health: 10,
            backpackCapacity: 4,
            broughtItems: [],
            raidLoot: []);

        RaidEngine.ApplyCombatDamage(state, 12);

        Assert.Equal(0, state.Health);
        Assert.True(state.IsDead);
    }

    [Fact]
    public void ResolveOpeningPhase_PlayerAmbush_GrantsPlayerSurpriseAndOneOpeningAction()
    {
        var result = RaidEngine.ResolveOpeningPhase(OpeningContactState.PlayerAmbush, playerInitiative: 12, enemyInitiative: 8);

        Assert.Equal(OpeningContactState.PlayerAmbush, result.ContactState);
        Assert.Equal(OpeningSide.Player, result.SurpriseSide);
        Assert.Equal(OpeningSide.None, result.InitiativeWinner);
        Assert.Equal(1, result.OpeningActionsRemaining);
        Assert.False(result.SurprisePersistenceEligible);
    }

    [Fact]
    public void ResolveOpeningPhase_EnemyAmbush_GrantsEnemySurpriseAndOneOpeningAction()
    {
        var result = RaidEngine.ResolveOpeningPhase(OpeningContactState.EnemyAmbush, playerInitiative: 7, enemyInitiative: 14);

        Assert.Equal(OpeningContactState.EnemyAmbush, result.ContactState);
        Assert.Equal(OpeningSide.Enemy, result.SurpriseSide);
        Assert.Equal(OpeningSide.None, result.InitiativeWinner);
        Assert.Equal(1, result.OpeningActionsRemaining);
        Assert.False(result.SurprisePersistenceEligible);
    }

    [Fact]
    public void ResolveOpeningPhase_MutualContact_UsesInitiativeToSetOpeningControl()
    {
        var result = RaidEngine.ResolveOpeningPhase(OpeningContactState.MutualContact, playerInitiative: 15, enemyInitiative: 11);

        Assert.Equal(OpeningContactState.MutualContact, result.ContactState);
        Assert.Equal(OpeningSide.None, result.SurpriseSide);
        Assert.Equal(OpeningSide.Player, result.InitiativeWinner);
        Assert.Equal(1, result.OpeningActionsRemaining);
        Assert.False(result.SurprisePersistenceEligible);
    }

    [Fact]
    public void ResolveOpeningPhase_ContextWithZeroModifiers_PreservesCurrentBehavior()
    {
        var context = new OpeningPhaseContext(
            OpeningContactState.MutualContact,
            PlayerInitiative: 15,
            EnemyInitiative: 11);

        var result = RaidEngine.ResolveOpeningPhase(context);

        Assert.Equal(OpeningContactState.MutualContact, result.ContactState);
        Assert.Equal(OpeningSide.None, result.SurpriseSide);
        Assert.Equal(OpeningSide.Player, result.InitiativeWinner);
        Assert.Equal(1, result.OpeningActionsRemaining);
        Assert.False(result.SurprisePersistenceEligible);
    }

    [Fact]
    public void TryAddLoot_RejectsWhenBackpackCapacityExceeded()
    {
        var state = new RaidState(
            health: 30,
            backpackCapacity: 2,
            broughtItems: [],
            raidLoot: []);

        var addedFirst = RaidEngine.TryAddLoot(state, new Item("Bandage", ItemType.Sellable, Weight: 1, Slots: 1));
        var addedSecond = RaidEngine.TryAddLoot(state, new Item("Rifle", ItemType.Weapon, Weight: 1, Slots: 2));

        Assert.True(addedFirst);
        Assert.False(addedSecond);
        Assert.Single(state.RaidLoot);
    }

    [Fact]
    public void Sellable_Items_AreRegularLootForCapacityChecks()
    {
        var state = new RaidState(
            health: 30,
            backpackCapacity: 1,
            broughtItems: [],
            raidLoot: []);

        var added = RaidEngine.TryAddLoot(state, new Item("Ammo Box", ItemType.Sellable, Weight: 2, Slots: 1));

        Assert.True(added);
        Assert.Single(state.RaidLoot);
    }

    [Fact]
    public void RaidInventory_LootingMedkit_FromDiscovered_IncrementsResourceOnly()
    {
        var state = new RaidState(
            health: 30,
            backpackCapacity: 2,
            broughtItems: [],
            raidLoot: []);

        RaidEngine.StartDiscoveredLootEncounter(state, [new Item("Medkit", ItemType.Consumable, Weight: 3, Slots: 1)]);

        var looted = RaidEngine.TryLootFromDiscovered(state, state.Inventory.DiscoveredLoot[0]);

        Assert.True(looted);
        Assert.Equal(1, state.Inventory.MedkitCount);
        Assert.Empty(state.Inventory.CarriedItems);
        Assert.Empty(state.Inventory.DiscoveredLoot);
    }

    [Fact]
    public void RaidInventory_EquipFromDiscovered_SwapsOldEquippedToDiscovered()
    {
        var state = new RaidState(
            health: 30,
            backpackCapacity: 2,
            broughtItems: [new Item("Makarov", ItemType.Weapon, Weight: 4, Slots: 1)],
            raidLoot: []);

        RaidEngine.StartDiscoveredLootEncounter(state, [new Item("AK74", ItemType.Weapon, Weight: 9, Slots: 1)]);

        var equipped = RaidEngine.TryEquipFromDiscovered(state, state.Inventory.DiscoveredLoot[0]);

        Assert.True(equipped);
        Assert.Equal("AK74", state.Inventory.EquippedWeapon?.Name);
        Assert.Single(state.Inventory.DiscoveredLoot);
        Assert.Equal("Makarov", state.Inventory.DiscoveredLoot[0].Name);
    }

    [Fact]
    public void RaidInventory_DroppingEquippedBackpack_SpillsCarriedItemsToDiscovered()
    {
        var state = new RaidState(
            health: 30,
            backpackCapacity: 6,
            broughtItems:
            [
                new Item("Makarov", ItemType.Weapon, Weight: 4, Slots: 1),
                new Item("Tactical Backpack", ItemType.Backpack, Weight: 8, Slots: 1)
            ],
            raidLoot:
            [
                new Item("Scrap Metal", ItemType.Material, Weight: 3, Slots: 1),
                new Item("Rare Scope", ItemType.Material, Weight: 2, Slots: 1)
            ]);

        var dropped = RaidEngine.TryDropEquippedToDiscovered(state, ItemType.Backpack);

        Assert.True(dropped);
        Assert.Null(state.Inventory.EquippedBackpack);
        Assert.Empty(state.Inventory.CarriedItems);
        Assert.Equal(3, state.Inventory.DiscoveredLoot.Count);
        Assert.Contains(state.Inventory.DiscoveredLoot, x => x.Name == "Tactical Backpack");
        Assert.Contains(state.Inventory.DiscoveredLoot, x => x.Name == "Scrap Metal");
        Assert.Contains(state.Inventory.DiscoveredLoot, x => x.Name == "Rare Scope");
    }

    [Fact]
    public void FinalizeRaid_Success_TransfersBroughtItemsAndLootToStash()
    {
        var game = new GameState([new Item("Spare Knife", ItemType.Weapon, Weight: 1, Slots: 1)]);
        var loadout = new List<Item>
        {
            new("Pistol", ItemType.Weapon, Weight: 1, Slots: 1),
            new("Light Armor", ItemType.Armor, Weight: 1, Slots: 1)
        };
        var raid = RaidEngine.StartRaid(game, loadout, backpackCapacity: 4, startingHealth: 30);
        RaidEngine.TryAddLoot(raid, new Item("Medkit", ItemType.Consumable, Weight: 3, Slots: 1));

        RaidEngine.FinalizeRaid(game, raid, extracted: true);

        Assert.Contains(game.Stash, i => i.Name == "Pistol");
        Assert.Contains(game.Stash, i => i.Name == "Light Armor");
        Assert.Contains(game.Stash, i => i.Name == "Medkit");
        Assert.Contains(game.Stash, i => i.Name == "Spare Knife");
    }

    [Fact]
    public void StartRaid_RemovesLoadoutFromStash()
    {
        var pistol = new Item("Pistol", ItemType.Weapon, Weight: 1, Slots: 1);
        var backpack = new Item("Backpack", ItemType.Backpack, Weight: 1, Slots: 1);
        var game = new GameState([pistol, backpack]);

        _ = RaidEngine.StartRaid(game, [pistol, backpack], backpackCapacity: 3, startingHealth: 20);

        Assert.Empty(game.Stash);
    }

    [Fact]
    public void FinalizeRaid_Death_LosesBroughtItemsAndRaidLoot()
    {
        var game = new GameState([new Item("Spare Knife", ItemType.Weapon, Weight: 1, Slots: 1)]);
        var loadout = new List<Item>
        {
            new("Pistol", ItemType.Weapon, Weight: 1, Slots: 1),
            new("Backpack", ItemType.Backpack, Weight: 1, Slots: 1)
        };
        var raid = RaidEngine.StartRaid(game, loadout, backpackCapacity: 2, startingHealth: 10);
        RaidEngine.TryAddLoot(raid, new Item("Ammo", ItemType.Material, Weight: 1, Slots: 1));

        RaidEngine.FinalizeRaid(game, raid, extracted: false);

        Assert.DoesNotContain(game.Stash, i => i.Name == "Pistol");
        Assert.DoesNotContain(game.Stash, i => i.Name == "Backpack");
        Assert.DoesNotContain(game.Stash, i => i.Name == "Ammo");
        Assert.Contains(game.Stash, i => i.Name == "Spare Knife");
    }

    private sealed class SequenceRng : IRng
    {
        private readonly Queue<int> _sequence;

        public SequenceRng(IEnumerable<int> sequence)
        {
            _sequence = new Queue<int>(sequence);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            var offset = _sequence.Dequeue();
            var span = maxExclusive - minInclusive;
            return minInclusive + (offset % span);
        }
    }
}
