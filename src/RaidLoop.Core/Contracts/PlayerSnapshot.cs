using System.Text.Json.Serialization;

namespace RaidLoop.Core.Contracts;

public sealed record OnPersonSnapshot(Item Item, bool IsEquipped);

public sealed record RandomCharacterSnapshot(string Name, IReadOnlyList<Item> Inventory);

public sealed record PlayerSnapshot
{
    public PlayerSnapshot(
        int Money,
        IReadOnlyList<Item> MainStash,
        IReadOnlyList<OnPersonSnapshot> OnPersonItems,
        int PlayerConstitution,
        int PlayerMaxHealth,
        DateTimeOffset RandomCharacterAvailableAt,
        RandomCharacterSnapshot? RandomCharacter,
        RaidSnapshot? ActiveRaid,
        PlayerStats? AcceptedStats = null,
        PlayerStats? DraftStats = null,
        int AvailableStatPoints = PlayerStatRules.StartingPool,
        bool StatsAccepted = false,
        IReadOnlyList<Item>? ShopStock = null)
    {
        this.Money = Money;
        this.MainStash = MainStash;
        this.OnPersonItems = OnPersonItems;
        this.ShopStock = ShopStock ?? [];
        this.PlayerConstitution = PlayerConstitution;
        this.PlayerMaxHealth = PlayerMaxHealth;
        this.RandomCharacterAvailableAt = RandomCharacterAvailableAt;
        this.RandomCharacter = RandomCharacter;
        this.ActiveRaid = ActiveRaid;
        this.AcceptedStats = AcceptedStats ?? PlayerStats.Default;
        this.DraftStats = DraftStats ?? PlayerStats.Default;
        this.AvailableStatPoints = AvailableStatPoints;
        this.StatsAccepted = StatsAccepted;
    }

    public int Money { get; init; }
    public IReadOnlyList<Item> MainStash { get; init; }
    public IReadOnlyList<OnPersonSnapshot> OnPersonItems { get; init; }
    public IReadOnlyList<Item> ShopStock { get; init; }
    public int PlayerConstitution { get; init; }
    public int PlayerMaxHealth { get; init; }

    [JsonConverter(typeof(FlexibleDateTimeOffsetJsonConverter))]
    public DateTimeOffset RandomCharacterAvailableAt { get; init; }

    public RandomCharacterSnapshot? RandomCharacter { get; init; }
    public RaidSnapshot? ActiveRaid { get; init; }
    public PlayerStats AcceptedStats { get; init; }
    public PlayerStats DraftStats { get; init; }
    public int AvailableStatPoints { get; init; }
    public bool StatsAccepted { get; init; }
}
