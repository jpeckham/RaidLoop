namespace RaidLoop.Core;

public sealed class LootTierProfile
{
    private readonly (Rarity Tier, int Weight)[] _weights;

    public LootTierProfile(int commonWeight, int uncommonWeight, int rareWeight, int epicWeight, int legendaryWeight)
    {
        _weights =
        [
            (Rarity.Common, commonWeight),
            (Rarity.Uncommon, uncommonWeight),
            (Rarity.Rare, rareWeight),
            (Rarity.Epic, epicWeight),
            (Rarity.Legendary, legendaryWeight)
        ];

        if (_weights.Any(entry => entry.Weight < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(commonWeight), "Tier weights cannot be negative.");
        }

        if (_weights.All(entry => entry.Weight == 0))
        {
            throw new ArgumentOutOfRangeException(nameof(commonWeight), "At least one tier weight must be positive.");
        }
    }

    public Rarity Roll(IRng rng, IReadOnlyCollection<Rarity> availableTiers)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(availableTiers);

        var weightedAvailable = _weights
            .Where(entry => entry.Weight > 0 && availableTiers.Contains(entry.Tier))
            .ToArray();

        if (weightedAvailable.Length == 0)
        {
            throw new InvalidOperationException("No weighted tiers are available to roll.");
        }

        var totalWeight = weightedAvailable.Sum(entry => entry.Weight);
        var roll = rng.Next(0, totalWeight);
        var cumulative = 0;

        foreach (var entry in weightedAvailable)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                return entry.Tier;
            }
        }

        return weightedAvailable[^1].Tier;
    }
}
