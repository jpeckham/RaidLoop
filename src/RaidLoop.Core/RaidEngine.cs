namespace RaidLoop.Core;

public static class RaidEngine
{
    public static RaidState StartRaid(GameState game, List<Item> loadout, int backpackCapacity, int startingHealth)
    {
        foreach (var item in loadout)
        {
            game.Stash.Remove(item);
        }

        return new RaidState(
            health: startingHealth,
            backpackCapacity: backpackCapacity,
            broughtItems: [.. loadout],
            raidLoot: []);
    }

    public static void ApplyCombatDamage(RaidState state, int damage)
    {
        state.Health = Math.Max(0, state.Health - Math.Max(0, damage));
    }

    public static bool TryAddLoot(RaidState state, Item item)
    {
        var currentSlots = state.RaidLoot.Sum(x => x.Slots);
        if (currentSlots + item.Slots > state.BackpackCapacity)
        {
            return false;
        }

        state.RaidLoot.Add(item);
        return true;
    }

    public static void FinalizeRaid(GameState game, RaidState raid, bool extracted)
    {
        if (!extracted || raid.IsDead)
        {
            return;
        }

        game.Stash.AddRange(raid.BroughtItems);
        game.Stash.AddRange(raid.RaidLoot);
    }
}
