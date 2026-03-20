namespace RaidLoop.Core.Contracts;

public sealed record RaidSnapshot(
    int Health,
    int BackpackCapacity,
    int Ammo,
    bool WeaponMalfunction,
    int Medkits,
    int LootSlots,
    int ExtractProgress,
    int ExtractRequired,
    string EncounterType,
    string EncounterTitle,
    string EncounterDescription,
    string EnemyName,
    int EnemyHealth,
    string LootContainer,
    bool AwaitingDecision,
    IReadOnlyList<Item> DiscoveredLoot,
    IReadOnlyList<Item> CarriedLoot,
    IReadOnlyList<Item> EquippedItems,
    IReadOnlyList<string> LogEntries);
