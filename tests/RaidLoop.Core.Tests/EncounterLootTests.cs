using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

public class EncounterLootTests
{
    [Fact]
    public void StartLootEncounter_WithLootTable_ClearsAndAddsDrawnItems()
    {
        var discovered = new List<Item>
        {
            new("Old Loot", ItemType.Sellable, Slots: 1)
        };
        var table = new LootTable(
        [
            (new Item("New Loot", ItemType.Material, Slots: 1, Rarity: Rarity.Rare), 10),
            (new Item("Ammo Box", ItemType.Sellable, Slots: 1, Rarity: Rarity.Common), 10)
        ]);

        EncounterLoot.StartLootEncounter(discovered, table, new TestSequenceRng([0, 0]), 1);

        Assert.Single(discovered);
        Assert.Equal("New Loot", discovered[0].Name);
        Assert.Equal(Rarity.Rare, discovered[0].Rarity);
    }

    [Fact]
    public void StartLootEncounter_WithEnumerable_StillBehavesAsBefore()
    {
        var discovered = new List<Item>
        {
            new("Old Loot", ItemType.Sellable, Slots: 1)
        };

        EncounterLoot.StartLootEncounter(discovered, [new Item("New Loot", ItemType.Material, Slots: 1)]);

        Assert.Single(discovered);
        Assert.Equal("New Loot", discovered[0].Name);
    }
}
