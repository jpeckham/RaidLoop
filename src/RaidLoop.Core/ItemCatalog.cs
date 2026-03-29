namespace RaidLoop.Core;

public static class ItemCatalog
{
    private static readonly Dictionary<string, Item> Items = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rusty Knife"] = new Item("Rusty Knife", ItemType.Weapon, Value: 1, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1),
        ["Light Pistol"] = new Item("Light Pistol", ItemType.Weapon, Value: 60, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 2),
        ["Drum SMG"] = new Item("Drum SMG", ItemType.Weapon, Value: 160, Slots: 1, Rarity: Rarity.Uncommon, DisplayRarity: DisplayRarity.Uncommon, Weight: 12),
        ["Field Carbine"] = new Item("Field Carbine", ItemType.Weapon, Value: 320, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare, Weight: 7),
        ["Marksman Rifle"] = new Item("Marksman Rifle", ItemType.Weapon, Value: 550, Slots: 1, Rarity: Rarity.Epic, DisplayRarity: DisplayRarity.Epic, Weight: 10),
        ["Battle Rifle"] = new Item("Battle Rifle", ItemType.Weapon, Value: 375, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare, Weight: 10),
        ["Support Machine Gun"] = new Item("Support Machine Gun", ItemType.Weapon, Value: 800, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary, Weight: 18),
        ["Soft Armor Vest"] = new Item("Soft Armor Vest", ItemType.Armor, Value: 95, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 9),
        ["Reinforced Vest"] = new Item("Reinforced Vest", ItemType.Armor, Value: 160, Slots: 1, Rarity: Rarity.Uncommon, DisplayRarity: DisplayRarity.Uncommon, Weight: 7),
        ["Light Plate Carrier"] = new Item("Light Plate Carrier", ItemType.Armor, Value: 225, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare, Weight: 7),
        ["Medium Plate Carrier"] = new Item("Medium Plate Carrier", ItemType.Armor, Value: 375, Slots: 1, Rarity: Rarity.Epic, DisplayRarity: DisplayRarity.Epic, Weight: 22),
        ["Heavy Plate Carrier"] = new Item("Heavy Plate Carrier", ItemType.Armor, Value: 450, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary, Weight: 28),
        ["Assault Plate Carrier"] = new Item("Assault Plate Carrier", ItemType.Armor, Value: 650, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary, Weight: 32),
        ["Small Backpack"] = new Item("Small Backpack", ItemType.Backpack, Value: 25, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1),
        ["Large Backpack"] = new Item("Large Backpack", ItemType.Backpack, Value: 50, Slots: 1, Rarity: Rarity.Uncommon, DisplayRarity: DisplayRarity.Uncommon, Weight: 1),
        ["Tactical Backpack"] = new Item("Tactical Backpack", ItemType.Backpack, Value: 75, Slots: 2, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare, Weight: 2),
        ["Hiking Backpack"] = new Item("Hiking Backpack", ItemType.Backpack, Value: 400, Slots: 3, Rarity: Rarity.Epic, DisplayRarity: DisplayRarity.Epic, Weight: 2),
        ["Raid Backpack"] = new Item("Raid Backpack", ItemType.Backpack, Value: 600, Slots: 4, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary, Weight: 8),
        ["Medkit"] = new Item("Medkit", ItemType.Consumable, Value: 30, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1),
        ["Bandage"] = new Item("Bandage", ItemType.Sellable, Value: 15, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 1),
        ["Ammo Box"] = new Item("Ammo Box", ItemType.Sellable, Value: 20, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 4),
        ["Scrap Metal"] = new Item("Scrap Metal", ItemType.Material, Value: 18, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 10),
        ["Rare Scope"] = new Item("Rare Scope", ItemType.Material, Value: 80, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.SellOnly, Weight: 1),
        ["Legendary Trigger Group"] = new Item("Legendary Trigger Group", ItemType.Material, Value: 150, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
    };

    public static Item Get(string name)
    {
        if (!TryGet(name, out var item))
        {
            throw new KeyNotFoundException($"No authored item definition exists for '{name}'.");
        }

        return item!;
    }

    public static bool TryGet(string name, out Item? item)
    {
        return Items.TryGetValue(name, out item);
    }

    public static Item Create(string name)
    {
        return Get(name) with { };
    }
}
