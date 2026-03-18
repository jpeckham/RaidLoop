namespace RaidLoop.Core;

public static class EncounterLoot
{
    public static void StartLootEncounter(List<Item> discoveredLoot, IEnumerable<Item> items)
    {
        discoveredLoot.Clear();
        AppendDiscoveredLoot(discoveredLoot, items);
    }

    public static void StartLootEncounter(List<Item> discoveredLoot, LootTable table, IRng rng, int drawCount = 3)
    {
        discoveredLoot.Clear();
        AppendDiscoveredLoot(discoveredLoot, table.Draw(rng, drawCount));
    }

    public static void AppendDiscoveredLoot(List<Item> discoveredLoot, IEnumerable<Item> items)
    {
        discoveredLoot.AddRange(items);
    }
}
