namespace RaidLoop.Core;

public static class ItemCatalog
{
    private static readonly Dictionary<int, Item> ItemsById = [];
    private static readonly Dictionary<string, Item> LegacyNames = new(StringComparer.OrdinalIgnoreCase);

    static ItemCatalog()
    {
        RegisterCanonical(new Item("Rusty Knife", ItemType.Weapon, Value: 1, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            ItemDefId = 1
        }, "rusty_knife");

        RegisterAuthored(
            itemDefId: 2,
            displayName: "Makarov",
            itemType: ItemType.Weapon,
            weight: 2,
            value: 60,
            rarity: Rarity.Common,
            displayRarity: DisplayRarity.Common,
            aliases: ["makarov", "Light Pistol"]);

        RegisterAuthored(
            itemDefId: 3,
            displayName: "PPSH",
            itemType: ItemType.Weapon,
            weight: 12,
            value: 160,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon,
            aliases: ["ppsh", "Drum SMG"]);

        RegisterAuthored(
            itemDefId: 4,
            displayName: "AK74",
            itemType: ItemType.Weapon,
            weight: 7,
            value: 320,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["ak74", "Field Carbine", "Hunting Rifle", "Compact Carbine"]);

        RegisterAuthored(
            itemDefId: 5,
            displayName: "AK47",
            itemType: ItemType.Weapon,
            weight: 10,
            value: 375,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["ak47", "Battle Rifle", "Sawed Shotgun"]);

        RegisterAuthored(
            itemDefId: 6,
            displayName: "SVDS",
            itemType: ItemType.Weapon,
            weight: 10,
            value: 550,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["svds", "Marksman Rifle"]);

        RegisterAuthored(
            itemDefId: 7,
            displayName: "PKP",
            itemType: ItemType.Weapon,
            weight: 18,
            value: 800,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["pkp", "Support Machine Gun"]);

        RegisterAuthored(
            itemDefId: 8,
            displayName: "6B2 body armor",
            itemType: ItemType.Armor,
            weight: 9,
            value: 95,
            rarity: Rarity.Common,
            displayRarity: DisplayRarity.Common,
            aliases: ["6b2_body_armor", "Soft Armor Vest", "Soft Vest"]);

        RegisterAuthored(
            itemDefId: 9,
            displayName: "BNTI Kirasa-N",
            itemType: ItemType.Armor,
            weight: 7,
            value: 160,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon,
            aliases: ["bnti_kirasa_n", "Reinforced Vest"]);

        RegisterAuthored(
            itemDefId: 10,
            displayName: "6B13 assault armor",
            itemType: ItemType.Armor,
            weight: 7,
            value: 225,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["6b13_assault_armor", "Light Plate Carrier", "Plate Carrier"]);

        RegisterAuthored(
            itemDefId: 11,
            displayName: "FORT Defender-2",
            itemType: ItemType.Armor,
            weight: 22,
            value: 375,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["fort_defender_2", "Medium Plate Carrier"]);

        RegisterAuthored(
            itemDefId: 12,
            displayName: "6B43 Zabralo-Sh body armor",
            itemType: ItemType.Armor,
            weight: 28,
            value: 450,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["6b43_zabralo_sh_body_armor", "Heavy Plate Carrier"]);

        RegisterAuthored(
            itemDefId: 13,
            displayName: "NFM THOR",
            itemType: ItemType.Armor,
            weight: 19,
            value: 650,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["nfm_thor", "Assault Plate Carrier"]);

        RegisterCanonical(new Item("Small Backpack", ItemType.Backpack, Value: 25, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            ItemDefId = 14
        }, "small_backpack");
        RegisterAuthored(
            itemDefId: 15,
            displayName: "Large Backpack",
            itemType: ItemType.Backpack,
            weight: 1,
            value: 50,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon,
            aliases: ["large_backpack"]);
        RegisterCanonical(new Item("Tactical Backpack", ItemType.Backpack, Value: 75, Slots: 2, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare, Weight: 2)
        {
            ItemDefId = 16
        }, "tactical_backpack");
        RegisterAuthored(
            itemDefId: 17,
            displayName: "Tasmanian Tiger Trooper 35",
            itemType: ItemType.Backpack,
            weight: 2,
            value: 400,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["tasmanian_tiger_trooper_35", "Hiking Backpack"]);
        RegisterAuthored(
            itemDefId: 18,
            displayName: "6Sh118",
            itemType: ItemType.Backpack,
            weight: 8,
            value: 600,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["6sh118", "Raid Backpack"]);

        RegisterCanonical(new Item("Medkit", ItemType.Consumable, Value: 30, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            ItemDefId = 19
        }, "medkit");
        RegisterCanonical(new Item("Bandage", ItemType.Sellable, Value: 15, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            ItemDefId = 20
        }, "bandage");
        RegisterCanonical(new Item("Ammo Box", ItemType.Sellable, Value: 20, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 4)
        {
            ItemDefId = 21
        }, "ammo_box");
        RegisterCanonical(new Item("Scrap Metal", ItemType.Material, Value: 18, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 10)
        {
            ItemDefId = 22
        }, "scrap_metal");
        RegisterCanonical(new Item("Rare Scope", ItemType.Material, Value: 80, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            ItemDefId = 23
        }, "rare_scope");
        RegisterCanonical(new Item("Legendary Trigger Group", ItemType.Material, Value: 150, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            ItemDefId = 24
        }, "legendary_trigger_group");
    }

    public static Item Get(string name)
    {
        return GetByLegacyName(name);
    }

    public static Item GetByItemDefId(int itemDefId)
    {
        if (!TryGetByItemDefId(itemDefId, out var item))
        {
            throw new KeyNotFoundException($"No authored item definition exists for itemDefId '{itemDefId}'.");
        }

        return item!;
    }

    public static Item GetByLegacyName(string name)
    {
        if (!TryGetByLegacyName(name, out var item))
        {
            throw new KeyNotFoundException($"No authored item definition exists for '{name}'.");
        }

        return item!;
    }

    // Legacy compatibility lookup. New code should prefer TryGetByItemDefId/Create.
    public static bool TryGet(string name, out Item? item)
    {
        return TryGetByLegacyName(name, out item);
    }

    public static bool TryGetByLegacyName(string name, out Item? item)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            item = null;
            return false;
        }

        return LegacyNames.TryGetValue(name, out item);
    }

    public static bool TryResolveAuthoredItem(int itemDefId, out Item? item)
    {
        if (TryGetByItemDefId(itemDefId, out item) && item is not null)
        {
            return true;
        }

        item = null;
        return false;
    }

    public static bool TryGetByItemDefId(int itemDefId, out Item? item)
    {
        if (itemDefId <= 0)
        {
            item = null;
            return false;
        }

        return ItemsById.TryGetValue(itemDefId, out item);
    }

    public static string GetLookupToken(int itemDefId)
    {
        return itemDefId switch
        {
            1 => "rusty_knife",
            2 => "makarov",
            3 => "ppsh",
            4 => "ak74",
            5 => "ak47",
            6 => "svds",
            7 => "pkp",
            8 => "6b2_body_armor",
            9 => "bnti_kirasa_n",
            10 => "6b13_assault_armor",
            11 => "fort_defender_2",
            12 => "6b43_zabralo_sh_body_armor",
            13 => "nfm_thor",
            14 => "small_backpack",
            15 => "large_backpack",
            16 => "tactical_backpack",
            17 => "tasmanian_tiger_trooper_35",
            18 => "6sh118",
            19 => "medkit",
            20 => "bandage",
            21 => "ammo_box",
            22 => "scrap_metal",
            23 => "rare_scope",
            24 => "legendary_trigger_group",
            _ => string.Empty
        };
    }

    public static Item Create(string name)
    {
        return CreateLegacy(name);
    }

    public static Item CreateLegacy(string name)
    {
        return GetByLegacyName(name) with { };
    }

    private static void RegisterAuthored(
        int itemDefId,
        string displayName,
        ItemType itemType,
        int weight,
        int value,
        Rarity rarity,
        DisplayRarity displayRarity,
        params string[] aliases)
    {
        var item = new Item(displayName, itemType, Weight: weight, Value: value, Slots: 1, Rarity: rarity, DisplayRarity: displayRarity)
        {
            ItemDefId = itemDefId
        };

        RegisterCanonical(item);

        foreach (var alias in aliases)
        {
            RegisterAlias(alias, item);
        }
    }

    private static void RegisterCanonical(Item item, params string[] aliases)
    {
        ItemsById[item.ItemDefId] = item;
        LegacyNames[item.Name] = item;

        foreach (var alias in aliases)
        {
            RegisterAlias(alias, item);
        }
    }

    private static void RegisterAlias(string alias, Item item)
    {
        if (LegacyNames.ContainsKey(alias))
        {
            return;
        }

        LegacyNames[alias] = item with { Name = alias };
    }
}
