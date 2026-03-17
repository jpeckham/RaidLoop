namespace RaidLoop.Core;

public sealed class LootTable
{
    private readonly (Item Item, int Weight)[] _entries;

    public LootTable(IReadOnlyList<(Item Item, int Weight)> entries)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        if (entries.Any(entry => entry.Weight <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(entries), "All weights must be positive.");
        }

        _entries = entries.ToArray();
    }

    public List<Item> Draw(IRng rng, int count)
    {
        if (rng is null)
        {
            throw new ArgumentNullException(nameof(rng));
        }

        if (count <= 0 || _entries.Length == 0)
        {
            return [];
        }

        var working = _entries.ToList();
        var drawn = new List<Item>(Math.Min(count, working.Count));

        while (working.Count > 0 && drawn.Count < count)
        {
            var totalWeight = working.Sum(entry => entry.Weight);
            var roll = rng.Next(0, totalWeight);
            var cumulative = 0;

            for (var i = 0; i < working.Count; i++)
            {
                cumulative += working[i].Weight;
                if (roll >= cumulative)
                {
                    continue;
                }

                drawn.Add(working[i].Item);
                working.RemoveAt(i);
                break;
            }
        }

        if (drawn.Count > 0 && !string.IsNullOrWhiteSpace(GameEventLog.CurrentRaidId))
        {
            GameEventLog.Append(new GameEvent(
                "loot.drawn",
                GameEventLog.CurrentRaidId,
                GameEventLog.CreateItemSnapshots(drawn),
                DateTimeOffset.UtcNow));
        }

        return drawn;
    }
}
