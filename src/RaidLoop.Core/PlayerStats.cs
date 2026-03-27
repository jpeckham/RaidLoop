namespace RaidLoop.Core;

public sealed record PlayerStats(
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma)
{
    public static PlayerStats Default => new(
        PlayerStatRules.MinimumScore,
        PlayerStatRules.MinimumScore,
        PlayerStatRules.MinimumScore,
        PlayerStatRules.MinimumScore,
        PlayerStatRules.MinimumScore,
        PlayerStatRules.MinimumScore);
}

public sealed record PlayerStatAllocation(PlayerStats Stats, int AvailablePoints)
{
    public static PlayerStatAllocation CreateDefault()
    {
        return new PlayerStatAllocation(PlayerStats.Default, PlayerStatRules.StartingPool);
    }
}

public static class PlayerStatRules
{
    public const int MinimumScore = 8;
    public const int MaximumScore = 18;
    public const int StartingPool = 27;

    public static int GetAbilityModifier(int score)
    {
        return (int)Math.Floor((score - 10) / 2.0);
    }

    public static int GetRaiseCost(int currentScore)
    {
        return currentScore switch
        {
            < MinimumScore => throw new ArgumentOutOfRangeException(nameof(currentScore)),
            >= MaximumScore => 0,
            <= 12 => 1,
            <= 14 => 2,
            <= 16 => 3,
            _ => 4
        };
    }

    public static int GetLowerRefund(int currentScore)
    {
        return currentScore switch
        {
            <= MinimumScore => 0,
            <= 13 => 1,
            <= 15 => 2,
            <= 17 => 3,
            _ => 4
        };
    }
}
