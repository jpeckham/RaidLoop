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

public sealed record Item(string Name, ItemType Type, int Slots = 1);

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
    public int BackpackCapacity { get; }
    public List<Item> BroughtItems { get; } = [];
    public List<Item> RaidLoot { get; } = [];
    public bool IsDead => Health <= 0;

    public RaidState(int health, int backpackCapacity, List<Item> broughtItems, List<Item> raidLoot)
    {
        Health = health;
        BackpackCapacity = backpackCapacity;
        BroughtItems = broughtItems;
        RaidLoot = raidLoot;
    }
}
