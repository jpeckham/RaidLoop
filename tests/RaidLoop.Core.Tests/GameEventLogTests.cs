using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

[Collection("GameEventLog")]
public class GameEventLogTests : IDisposable
{
    public GameEventLogTests()
    {
        GameEventLog.Clear();
    }

    [Fact]
    public void Append_StoresEventsInOrder()
    {
        GameEventLog.Append(new GameEvent("one", "raid-1", [new ItemSnapshot("Bandage", "Sellable", "Common", 1)], DateTimeOffset.UtcNow));
        GameEventLog.Append(new GameEvent("two", "raid-1", [new ItemSnapshot("Field Carbine", "Weapon", "Rare", 8)], DateTimeOffset.UtcNow));

        Assert.Collection(
            GameEventLog.Events,
            evt => Assert.Equal("one", evt.EventName),
            evt => Assert.Equal("two", evt.EventName));
    }

    [Fact]
    public void Clear_RemovesEvents()
    {
        GameEventLog.Append(new GameEvent("one", "raid-1", [new ItemSnapshot("Bandage", "Sellable", "Common", 1)], DateTimeOffset.UtcNow));

        GameEventLog.Clear();

        Assert.Empty(GameEventLog.Events);
    }

    [Fact]
    public void Events_AreReadableWhenEmptyAndPopulated()
    {
        _ = GameEventLog.Events.Count;
        GameEventLog.Append(new GameEvent("one", "raid-1", [new ItemSnapshot("Bandage", "Sellable", "Common", 1)], DateTimeOffset.UtcNow));

        Assert.Single(GameEventLog.Events);
    }

    [Fact]
    public void ItemSnapshot_PreservesValue()
    {
        var snapshot = new ItemSnapshot("Battle Rifle", "Weapon", "Legendary", 20);

        Assert.Equal(20, snapshot.Value);
    }

    [Fact]
    public void GameEvent_TotalValueDefaultsToZero()
    {
        var evt = new GameEvent("one", "raid-1", [new ItemSnapshot("Bandage", "Sellable", "Common", 1)], DateTimeOffset.UtcNow);

        Assert.Equal(0, evt.TotalValue);
    }

    [Fact]
    public void CreateItemSnapshots_UsesDisplayRarityAndAuthoredValue()
    {
        var snapshots = GameEventLog.CreateItemSnapshots([ItemCatalog.Get("Bandage")]);

        var snapshot = Assert.Single(snapshots);
        Assert.Equal("SellOnly", snapshot.Rarity);
        Assert.Equal(ItemCatalog.Get("Bandage").Value, snapshot.Value);
    }

    public void Dispose()
    {
        GameEventLog.Clear();
    }
}
