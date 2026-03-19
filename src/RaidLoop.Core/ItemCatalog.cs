namespace RaidLoop.Core;

public static class ItemCatalog
{
    private static readonly Dictionary<string, Item> Items = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rusty Knife"] = new Item("Rusty Knife", ItemType.Weapon, Value: 1, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common),
        ["Makarov"] = new Item("Makarov", ItemType.Weapon, Value: 12, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common),
        ["PPSH"] = new Item("PPSH", ItemType.Weapon, Value: 20, Slots: 1, Rarity: Rarity.Uncommon, DisplayRarity: DisplayRarity.Uncommon),
        ["AK74"] = new Item("AK74", ItemType.Weapon, Value: 34, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare),
        ["SVDS"] = new Item("SVDS", ItemType.Weapon, Value: 44, Slots: 1, Rarity: Rarity.Epic, DisplayRarity: DisplayRarity.Epic),
        ["AK47"] = new Item("AK47", ItemType.Weapon, Value: 38, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare),
        ["PKP"] = new Item("PKP", ItemType.Weapon, Value: 66, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary),
        ["6B2 body armor"] = new Item("6B2 body armor", ItemType.Armor, Value: 14, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common),
        ["6B13 assault armor"] = new Item("6B13 assault armor", ItemType.Armor, Value: 30, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare),
        ["FORT Defender-2"] = new Item("FORT Defender-2", ItemType.Armor, Value: 40, Slots: 1, Rarity: Rarity.Epic, DisplayRarity: DisplayRarity.Epic),
        ["6B43 Zabralo-Sh body armor"] = new Item("6B43 Zabralo-Sh body armor", ItemType.Armor, Value: 52, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary),
        ["NFM THOR"] = new Item("NFM THOR", ItemType.Armor, Value: 68, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary),
        ["Small Backpack"] = new Item("Small Backpack", ItemType.Backpack, Value: 18, Slots: 1, Rarity: Rarity.Uncommon, DisplayRarity: DisplayRarity.Uncommon),
        ["Tactical Backpack"] = new Item("Tactical Backpack", ItemType.Backpack, Value: 28, Slots: 2, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare),
        ["Tasmanian Tiger Trooper 35"] = new Item("Tasmanian Tiger Trooper 35", ItemType.Backpack, Value: 40, Slots: 3, Rarity: Rarity.Epic, DisplayRarity: DisplayRarity.Epic),
        ["6Sh118"] = new Item("6Sh118", ItemType.Backpack, Value: 58, Slots: 4, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.Legendary),
        ["Medkit"] = new Item("Medkit", ItemType.Consumable, Value: 10, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common),
        ["Bandage"] = new Item("Bandage", ItemType.Sellable, Value: 4, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly),
        ["Ammo Box"] = new Item("Ammo Box", ItemType.Sellable, Value: 6, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly),
        ["Scrap Metal"] = new Item("Scrap Metal", ItemType.Material, Value: 5, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly),
        ["Rare Scope"] = new Item("Rare Scope", ItemType.Material, Value: 16, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.SellOnly),
        ["Legendary Trigger Group"] = new Item("Legendary Trigger Group", ItemType.Material, Value: 26, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.SellOnly)
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
