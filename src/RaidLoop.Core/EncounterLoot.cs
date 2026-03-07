namespace RaidLoop.Core;

public static class EncounterLoot
{
    public static void StartLootEncounter(List<Item> discoveredLoot, IEnumerable<Item> items)
    {
        discoveredLoot.Clear();
        AppendDiscoveredLoot(discoveredLoot, items);
    }

    public static void AppendDiscoveredLoot(List<Item> discoveredLoot, IEnumerable<Item> items)
    {
        discoveredLoot.AddRange(items);
    }
}
