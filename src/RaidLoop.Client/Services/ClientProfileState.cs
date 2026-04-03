using RaidLoop.Core;

namespace RaidLoop.Client.Services;

public sealed record RandomCharacterState
{
    public RandomCharacterState(string Name, List<Item> Inventory, PlayerStats Stats)
    {
        ArgumentNullException.ThrowIfNull(Stats);
        this.Name = Name;
        this.Inventory = Inventory;
        this.Stats = Stats;
    }

    public string Name { get; init; }
    public List<Item> Inventory { get; init; }
    public PlayerStats Stats { get; init; }
}

public sealed record GameSave(
    List<Item> MainStash,
    DateTimeOffset RandomCharacterAvailableAt,
    RandomCharacterState? RandomCharacter,
    int Money,
    List<OnPersonEntry> OnPersonItems);

public sealed record OnPersonEntry(Item Item, bool IsEquipped);
