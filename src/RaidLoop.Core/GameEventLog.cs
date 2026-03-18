namespace RaidLoop.Core;

public static class GameEventLog
{
    private static readonly List<GameEvent> Entries = [];

    public static IReadOnlyList<GameEvent> Events => Entries;

    public static string CurrentRaidId { get; private set; } = string.Empty;

    public static void SetRaidContext(string raidId)
    {
        CurrentRaidId = raidId ?? string.Empty;
    }

    public static void Append(GameEvent evt)
    {
        Entries.Add(evt);
    }

    public static void Clear()
    {
        Entries.Clear();
        CurrentRaidId = string.Empty;
    }

    public static IReadOnlyList<ItemSnapshot> CreateItemSnapshots(IEnumerable<Item> items)
    {
        return items
            .Select(item => new ItemSnapshot(item.Name, item.Type.ToString(), item.DisplayRarity.ToString(), item.Value))
            .ToList();
    }
}

public sealed record GameEvent(
    string EventName,
    string RaidId,
    IReadOnlyList<ItemSnapshot> Items,
    DateTimeOffset Timestamp,
    int TotalValue = 0);

public sealed record ItemSnapshot(string Name, string Category, string Rarity, int Value);
