using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

[Collection("GameEventLog")]
public class LootTableTests
{
    [Fact]
    public void Draw_WithZeroCount_ReturnsEmpty()
    {
        var table = new LootTable(
        [
            (new Item("Bandage", ItemType.Sellable, Value: 1, Slots: 1, Rarity: Rarity.Common), 4)
        ]);

        var drawn = table.Draw(new TestSequenceRng([0]), 0);

        Assert.Empty(drawn);
    }

    [Fact]
    public void Draw_ReturnsDistinctItemsWithoutReplacement()
    {
        var table = new LootTable(
        [
            (new Item("Bandage", ItemType.Sellable, Value: 1, Slots: 1, Rarity: Rarity.Common), 10),
            (new Item("Medkit", ItemType.Consumable, Value: 1, Slots: 1, Rarity: Rarity.Uncommon), 10),
            (new Item("AK74", ItemType.Weapon, Value: 1, Slots: 1, Rarity: Rarity.Rare), 10)
        ]);

        var drawn = table.Draw(new TestSequenceRng([0, 0, 0]), 3);

        Assert.Equal(3, drawn.Count);
        Assert.Equal(3, drawn.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Draw_WhenCountExceedsEntries_ReturnsAllEntriesWithoutDuplicates()
    {
        var table = new LootTable(
        [
            (new Item("Bandage", ItemType.Sellable, Value: 1, Slots: 1, Rarity: Rarity.Common), 10),
            (new Item("Medkit", ItemType.Consumable, Value: 1, Slots: 1, Rarity: Rarity.Uncommon), 10)
        ]);

        var drawn = table.Draw(new TestSequenceRng([0, 0, 0]), 5);

        Assert.Equal(2, drawn.Count);
        Assert.Equal(2, drawn.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void LootTables_FactoriesReturnTables()
    {
        Assert.NotNull(LootTables.WeaponsCrate());
        Assert.NotNull(LootTables.ArmourCrate());
        Assert.NotNull(LootTables.MixedCache());
        Assert.NotNull(LootTables.EnemyLoadout());
    }

    [Fact]
    public void MixedCache_DrawsExposeMoreThanOneRarityAcrossDeterministicRuns()
    {
        var table = LootTables.MixedCache();
        var rarities = new HashSet<Rarity>();

        foreach (var sequence in new[]
        {
            new[] { 0, 0, 0, 0, 0, 0 },
            new[] { 58, 0, 0, 0, 0, 0 }
        })
        {
            var drawn = table.Draw(new TestSequenceRng(sequence), 3);
            foreach (var item in drawn)
            {
                rarities.Add(item.Rarity);
            }
        }

        Assert.True(rarities.Count >= 2);
    }

    [Fact]
    public void MixedCache_ApproximatesConfiguredRarityDistribution()
    {
        var table = LootTables.MixedCache();
        var counts = new Dictionary<Rarity, int>();
        var rng = new CyclingRng();

        for (var i = 0; i < 10_000; i++)
        {
            var item = table.Draw(rng, 1).Single();
            counts[item.Rarity] = counts.TryGetValue(item.Rarity, out var count) ? count + 1 : 1;
        }

        Assert.InRange(counts[Rarity.Common] / 10_000d, 0.60, 0.66);
        Assert.InRange(counts[Rarity.Uncommon] / 10_000d, 0.17, 0.22);
        Assert.InRange(counts[Rarity.Rare] / 10_000d, 0.07, 0.12);
        Assert.InRange(counts[Rarity.Epic] / 10_000d, 0.03, 0.07);
        Assert.InRange(counts[Rarity.Legendary] / 10_000d, 0.02, 0.05);
    }

    [Fact]
    public void Draw_DoesNotEmitLootEventWithoutRaidContext()
    {
        GameEventLog.Clear();
        var table = LootTables.MixedCache();

        _ = table.Draw(new TestSequenceRng([0, 0]), 1);

        Assert.Empty(GameEventLog.Events);
    }

    [Fact]
    public void Draw_EmitsLootEventWithRaidContext()
    {
        GameEventLog.Clear();
        GameEventLog.SetRaidContext("raid-123");
        var table = LootTables.MixedCache();

        var drawn = table.Draw(new TestSequenceRng([0, 0]), 1);

        var evt = Assert.Single(GameEventLog.Events);
        Assert.Equal("loot.drawn", evt.EventName);
        Assert.Equal("raid-123", evt.RaidId);
        Assert.Equal(drawn[0].DisplayRarity.ToString(), evt.Items[0].Rarity);
    }

    [Fact]
    public void Draw_WithTierProfile_RollsFromTierWeightsInsteadOfItemCount()
    {
        var table = new LootTable(
            new LootTierProfile(commonWeight: 1, uncommonWeight: 1, rareWeight: 1, epicWeight: 1, legendaryWeight: 10),
            [
                ItemCatalog.Create("Makarov"),
                ItemCatalog.Create("Bandage"),
                ItemCatalog.Create("Scrap Metal"),
                ItemCatalog.Create("AK47")
            ]);

        var drawn = table.Draw(new TestSequenceRng([12, 0]), 1);

        Assert.Equal("AK47", Assert.Single(drawn).Name);
    }

    [Fact]
    public void Draw_WithTierProfile_CanProduceHigherTierItems()
    {
        var table = new LootTable(
            new LootTierProfile(commonWeight: 0, uncommonWeight: 0, rareWeight: 1, epicWeight: 0, legendaryWeight: 0),
            [
                ItemCatalog.Create("Makarov"),
                ItemCatalog.Create("AK74")
            ]);

        var drawn = table.Draw(new TestSequenceRng([0, 0]), 1);

        Assert.Equal(Rarity.Rare, Assert.Single(drawn).Rarity);
    }

    [Fact]
    public void Draw_WithTierShiftBooster_CanUpgradeRolledTier()
    {
        var table = new LootTable(
            new LootTierProfile(commonWeight: 1, uncommonWeight: 0, rareWeight: 0, epicWeight: 0, legendaryWeight: 0),
            [
                ItemCatalog.Create("Makarov"),
                ItemCatalog.Create("PPSH")
            ]);

        var drawn = table.Draw(new TestSequenceRng([0, 0]), 1, new LootBooster(TierShift: 1));

        Assert.Equal(Rarity.Uncommon, Assert.Single(drawn).Rarity);
    }

    [Fact]
    public void Draw_WithoutBooster_LeavesBaseTierSelectionUnchanged()
    {
        var table = new LootTable(
            new LootTierProfile(commonWeight: 1, uncommonWeight: 0, rareWeight: 0, epicWeight: 0, legendaryWeight: 0),
            [
                ItemCatalog.Create("Makarov"),
                ItemCatalog.Create("PPSH")
            ]);

        var drawn = table.Draw(new TestSequenceRng([0, 0]), 1);

        Assert.Equal(Rarity.Common, Assert.Single(drawn).Rarity);
    }

    [Fact]
    public void RealFactoryProfiles_DifferBetweenWeaponsCrateAndEnemyLoadout()
    {
        var weaponsDraw = LootTables.WeaponsCrate().Draw(new TestSequenceRng([41, 0]), 1);
        var enemyDraw = LootTables.EnemyLoadout().Draw(new TestSequenceRng([41, 0]), 1);

        Assert.Equal(Rarity.Uncommon, Assert.Single(weaponsDraw).Rarity);
        Assert.Equal(Rarity.Common, Assert.Single(enemyDraw).Rarity);
    }

    [Fact]
    public void ArmourCrate_CanProduceEpicBackpackTier()
    {
        var drawn = LootTables.ArmourCrate().Draw(new TestSequenceRng([58, 1]), 1);

        Assert.Equal("Tasmanian Tiger Trooper 35", Assert.Single(drawn).Name);
        Assert.Equal(Rarity.Epic, drawn[0].Rarity);
    }
}
