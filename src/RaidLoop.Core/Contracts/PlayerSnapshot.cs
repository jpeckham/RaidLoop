using System.Text.Json.Serialization;

namespace RaidLoop.Core.Contracts;

public sealed record OnPersonSnapshot(Item Item, bool IsEquipped);

public sealed record ItemRuleSnapshot(
    int ItemDefId,
    ItemType Type,
    int Weight,
    int Slots,
    Rarity Rarity);

public sealed record ShopOfferSnapshot(
    int ItemDefId,
    int Price,
    int Stock);

public sealed record RandomCharacterSnapshot
{
    public RandomCharacterSnapshot(string Name, IReadOnlyList<Item> Inventory, PlayerStats Stats)
    {
        ArgumentNullException.ThrowIfNull(Stats);
        this.Name = Name;
        this.Inventory = Inventory;
        this.Stats = Stats;
    }

    public string Name { get; init; }
    public IReadOnlyList<Item> Inventory { get; init; }
    public PlayerStats Stats { get; init; }
}

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
        IReadOnlyList<ShopOfferSnapshot>? ShopStock = null,
        IReadOnlyList<ItemRuleSnapshot>? ItemRules = null)
    {
        this.Money = Money;
        this.MainStash = MainStash;
        this.OnPersonItems = OnPersonItems;
        this.ShopStock = ShopStock ?? [];
        this.ItemRules = ItemRules ?? [];
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
    public IReadOnlyList<ShopOfferSnapshot> ShopStock { get; init; }
    public IReadOnlyList<ItemRuleSnapshot> ItemRules { get; init; }
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
