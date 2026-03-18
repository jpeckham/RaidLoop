namespace RaidLoop.Core;

public sealed class LootTable
{
    private readonly (Item Item, int Weight)[] _entries;
    private readonly LootTierProfile? _tierProfile;
    private readonly Dictionary<Rarity, List<Item>>? _itemsByTier;

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

    public LootTable(LootTierProfile tierProfile, IReadOnlyList<Item> items)
    {
        ArgumentNullException.ThrowIfNull(tierProfile);
        ArgumentNullException.ThrowIfNull(items);

        _entries = [];
        _tierProfile = tierProfile;
        _itemsByTier = items
            .GroupBy(item => item.Rarity)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    public List<Item> Draw(IRng rng, int count)
    {
        return Draw(rng, count, null);
    }

    public List<Item> Draw(IRng rng, int count, LootBooster? booster)
    {
        if (rng is null)
        {
            throw new ArgumentNullException(nameof(rng));
        }

        if (count <= 0)
        {
            return [];
        }

        if (_tierProfile is not null && _itemsByTier is not null)
        {
            return DrawByTier(rng, count, booster);
        }

        if (_entries.Length == 0)
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

    private List<Item> DrawByTier(IRng rng, int count, LootBooster? booster)
    {
        var working = _itemsByTier!
            .ToDictionary(entry => entry.Key, entry => entry.Value.ToList());
        var totalItemCount = working.Sum(entry => entry.Value.Count);
        var drawn = new List<Item>(Math.Min(count, totalItemCount));

        while (drawn.Count < count)
        {
            var availableTiers = working
                .Where(entry => entry.Value.Count > 0)
                .Select(entry => entry.Key)
                .ToArray();

            if (availableTiers.Length == 0)
            {
                break;
            }

            var rolledTier = _tierProfile!.Roll(rng, availableTiers);
            var shiftedTier = ShiftTier(rolledTier, booster?.TierShift ?? 0);
            var resolvedTier = ResolveAvailableTier(shiftedTier, working);
            var tierItems = working[resolvedTier];
            var index = rng.Next(0, tierItems.Count);

            drawn.Add(tierItems[index]);
            tierItems.RemoveAt(index);
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

    private static Rarity ShiftTier(Rarity tier, int shift)
    {
        var shifted = Math.Clamp((int)tier + shift, (int)Rarity.Common, (int)Rarity.Legendary);
        return (Rarity)shifted;
    }

    private static Rarity ResolveAvailableTier(Rarity preferredTier, IReadOnlyDictionary<Rarity, List<Item>> working)
    {
        for (var tier = (int)preferredTier; tier >= (int)Rarity.Common; tier--)
        {
            if (working.TryGetValue((Rarity)tier, out var items) && items.Count > 0)
            {
                return (Rarity)tier;
            }
        }

        for (var tier = (int)preferredTier + 1; tier <= (int)Rarity.Legendary; tier++)
        {
            if (working.TryGetValue((Rarity)tier, out var items) && items.Count > 0)
            {
                return (Rarity)tier;
            }
        }

        throw new InvalidOperationException("No items remain in any loot tier.");
    }
}
