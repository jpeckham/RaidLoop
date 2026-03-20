using System.Text.Json.Serialization;

namespace RaidLoop.Core.Contracts;

public sealed record OnPersonSnapshot(Item Item, bool IsEquipped);

public sealed record RandomCharacterSnapshot(string Name, IReadOnlyList<Item> Inventory);

public sealed record PlayerSnapshot(
    int Money,
    IReadOnlyList<Item> MainStash,
    IReadOnlyList<OnPersonSnapshot> OnPersonItems,
    [property: JsonConverter(typeof(FlexibleDateTimeOffsetJsonConverter))]
    DateTimeOffset RandomCharacterAvailableAt,
    RandomCharacterSnapshot? RandomCharacter,
    RaidSnapshot? ActiveRaid);
