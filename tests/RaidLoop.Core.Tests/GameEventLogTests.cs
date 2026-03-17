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
        GameEventLog.Append(new GameEvent("one", "raid-1", [new ItemSnapshot("Bandage", "Sellable", "Common")], DateTimeOffset.UtcNow));
        GameEventLog.Append(new GameEvent("two", "raid-1", [new ItemSnapshot("AK74", "Weapon", "Rare")], DateTimeOffset.UtcNow));

        Assert.Collection(
            GameEventLog.Events,
            evt => Assert.Equal("one", evt.EventName),
            evt => Assert.Equal("two", evt.EventName));
    }

    [Fact]
    public void Clear_RemovesEvents()
    {
        GameEventLog.Append(new GameEvent("one", "raid-1", [new ItemSnapshot("Bandage", "Sellable", "Common")], DateTimeOffset.UtcNow));

        GameEventLog.Clear();

        Assert.Empty(GameEventLog.Events);
    }

    [Fact]
    public void Events_AreReadableWhenEmptyAndPopulated()
    {
        _ = GameEventLog.Events.Count;
        GameEventLog.Append(new GameEvent("one", "raid-1", [new ItemSnapshot("Bandage", "Sellable", "Common")], DateTimeOffset.UtcNow));

        Assert.Single(GameEventLog.Events);
    }

    public void Dispose()
    {
        GameEventLog.Clear();
    }
}
