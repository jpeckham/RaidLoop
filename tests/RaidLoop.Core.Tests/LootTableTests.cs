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
            (new Item("Bandage", ItemType.Sellable, 1, Rarity.Common), 4)
        ]);

        var drawn = table.Draw(new TestSequenceRng([0]), 0);

        Assert.Empty(drawn);
    }

    [Fact]
    public void Draw_ReturnsDistinctItemsWithoutReplacement()
    {
        var table = new LootTable(
        [
            (new Item("Bandage", ItemType.Sellable, 1, Rarity.Common), 10),
            (new Item("Medkit", ItemType.Consumable, 1, Rarity.Uncommon), 10),
            (new Item("AK74", ItemType.Weapon, 1, Rarity.Rare), 10)
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
            (new Item("Bandage", ItemType.Sellable, 1, Rarity.Common), 10),
            (new Item("Medkit", ItemType.Consumable, 1, Rarity.Uncommon), 10)
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
            new[] { 0, 0, 0 },
            new[] { 170, 150, 140 }
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

        Assert.InRange(counts[Rarity.Common] / 10_000d, 0.65, 0.69);
        Assert.InRange(counts[Rarity.Uncommon] / 10_000d, 0.18, 0.22);
        Assert.InRange(counts[Rarity.Rare] / 10_000d, 0.08, 0.12);
        Assert.InRange(counts[Rarity.Legendary] / 10_000d, 0.02, 0.04);
    }

    [Fact]
    public void Draw_DoesNotEmitLootEventWithoutRaidContext()
    {
        GameEventLog.Clear();
        var table = LootTables.MixedCache();

        _ = table.Draw(new TestSequenceRng([0]), 1);

        Assert.Empty(GameEventLog.Events);
    }

    [Fact]
    public void Draw_EmitsLootEventWithRaidContext()
    {
        GameEventLog.Clear();
        GameEventLog.SetRaidContext("raid-123");
        var table = LootTables.MixedCache();

        var drawn = table.Draw(new TestSequenceRng([0]), 1);

        var evt = Assert.Single(GameEventLog.Events);
        Assert.Equal("loot.drawn", evt.EventName);
        Assert.Equal("raid-123", evt.RaidId);
        Assert.Equal(drawn[0].Rarity.ToString(), evt.Items[0].Rarity);
    }
}
