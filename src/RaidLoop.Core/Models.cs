using System.Text.Json.Serialization;

namespace RaidLoop.Core;

public enum ItemType
{
    Weapon,
    Armor,
    Backpack,
    Consumable,
    Sellable,
    Material
}

public enum OpeningContactState
{
    MutualContact,
    PlayerAmbush,
    EnemyAmbush
}

public enum OpeningSide
{
    None,
    Player,
    Enemy
}

public sealed record OpeningPhaseContext(
    OpeningContactState ContactState,
    int PlayerInitiative,
    int EnemyInitiative,
    int TimeOfDayVisibilityModifier = 0,
    int EnvironmentAwarenessModifier = 0,
    int PlayerGearAwarenessModifier = 0,
    int EnemyLocalizationModifier = 0);

[JsonConverter(typeof(ItemJsonConverter))]
public sealed record Item(
    string Name,
    ItemType Type,
    int Weight,
    int Value = 1,
    int Slots = 1,
    Rarity Rarity = Rarity.Common,
    DisplayRarity DisplayRarity = DisplayRarity.Common)
{
    [JsonPropertyName("itemDefId")]
    public int ItemDefId
    {
        get
        {
            if (_itemDefId > 0)
            {
                return _itemDefId;
            }

            if (!string.IsNullOrWhiteSpace(_key) && ItemCatalog.TryGetItemDefIdByKey(_key!, out var resolvedFromKey))
            {
                return resolvedFromKey;
            }

            return 0;
        }
        init => _itemDefId = value;
    }

    [JsonPropertyName("itemKey")]
    public string Key
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_key))
            {
                return _key!;
            }

            if (_itemDefId > 0 && ItemCatalog.TryGetByItemDefId(_itemDefId, out var resolvedById) && resolvedById is not null)
            {
                return resolvedById.Key;
            }

            return string.Empty;
        }
        init => _key = value ?? string.Empty;
    }

    private string? _key;
    private int _itemDefId;
}

public sealed class GameState
{
    public List<Item> Stash { get; } = [];

    public GameState(IEnumerable<Item> initialStash)
    {
        Stash.AddRange(initialStash);
    }
}

public sealed class RaidState
{
    public int Health { get; set; }
    public int BackpackCapacity { get; set; }
    public int MaxEncumbrance
    {
        get => Inventory.MaxEncumbrance;
        set => Inventory.MaxEncumbrance = value;
    }
    public List<Item> BroughtItems { get; } = [];
    public RaidInventory Inventory { get; }
    public List<Item> RaidLoot => Inventory.CarriedItems;
    public bool IsDead => Health <= 0;

    public RaidState(int health, int backpackCapacity, List<Item> broughtItems, List<Item> raidLoot)
    {
        Health = health;
        BackpackCapacity = backpackCapacity;
        BroughtItems = broughtItems;
        Inventory = RaidInventory.FromItems(broughtItems, raidLoot, backpackCapacity);
    }

    public RaidState(int health, RaidInventory inventory)
    {
        Health = health;
        Inventory = inventory;
        BackpackCapacity = inventory.BackpackCapacity;
        BroughtItems = inventory.GetExtractableItems().ToList();
    }
}

public sealed class RaidInventory
{
    public Item? EquippedWeapon { get; set; }
    public Item? EquippedArmor { get; set; }
    public Item? EquippedBackpack { get; set; }
    public List<Item> CarriedItems { get; } = [];
    public List<Item> DiscoveredLoot { get; } = [];
    public int MedkitCount { get; set; }
    public int BackpackCapacity { get; set; }
    public int MaxEncumbrance { get; set; } = CombatBalance.GetMaxEncumbranceFromStrength(PlayerStatRules.MinimumScore);

    public IEnumerable<Item> GetExtractableItems()
    {
        if (EquippedWeapon is not null)
        {
            yield return EquippedWeapon;
        }

        if (EquippedArmor is not null)
        {
            yield return EquippedArmor;
        }

        if (EquippedBackpack is not null)
        {
            yield return EquippedBackpack;
        }

        foreach (var item in CarriedItems)
        {
            yield return item;
        }

        for (var i = 0; i < MedkitCount; i++)
        {
            yield return ItemCatalog.Create("Medkit");
        }
    }

    public static RaidInventory FromItems(List<Item> broughtItems, List<Item> carriedItems, int backpackCapacity)
    {
        var inventory = new RaidInventory
        {
            EquippedWeapon = broughtItems.FirstOrDefault(x => x.Type == ItemType.Weapon),
            EquippedArmor = broughtItems.FirstOrDefault(x => x.Type == ItemType.Armor),
            EquippedBackpack = broughtItems.FirstOrDefault(x => x.Type == ItemType.Backpack),
            BackpackCapacity = backpackCapacity
        };

        foreach (var item in broughtItems.Where(x => x.Type is not (ItemType.Weapon or ItemType.Armor or ItemType.Backpack)))
        {
            if (CombatBalance.IsMedkit(item))
            {
                inventory.MedkitCount++;
                continue;
            }

            inventory.CarriedItems.Add(item);
        }

        foreach (var item in carriedItems)
        {
            if (CombatBalance.IsMedkit(item))
            {
                inventory.MedkitCount++;
                continue;
            }

            inventory.CarriedItems.Add(item);
        }

        return inventory;
    }
}

public sealed record OpeningPhaseResult(
    OpeningContactState ContactState,
    OpeningSide SurpriseSide,
    OpeningSide InitiativeWinner,
    int OpeningActionsRemaining,
    bool SurprisePersistenceEligible);
