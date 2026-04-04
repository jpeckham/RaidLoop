namespace RaidLoop.Core;

public static class ItemCatalog
{
    private static readonly Dictionary<int, Item> ItemsById = [];
    private static readonly Dictionary<string, Item> ItemsByKey = new(StringComparer.OrdinalIgnoreCase);
    // Legacy label aliases stay as a compatibility bridge while the app moves to key-first lookups.
    private static readonly Dictionary<string, Item> LegacyNames = new(StringComparer.OrdinalIgnoreCase);

    static ItemCatalog()
    {
        RegisterCanonical(new Item("Rusty Knife", ItemType.Weapon, Value: 1, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            ItemDefId = 1,
            Key = "rusty_knife"
        });

        RegisterAuthored(
            itemDefId: 2,
            key: "makarov",
            displayName: "Makarov",
            itemType: ItemType.Weapon,
            weight: 2,
            value: 60,
            rarity: Rarity.Common,
            displayRarity: DisplayRarity.Common,
            aliases: ["Light Pistol"]);

        RegisterAuthored(
            itemDefId: 3,
            key: "ppsh",
            displayName: "PPSH",
            itemType: ItemType.Weapon,
            weight: 12,
            value: 160,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon,
            aliases: ["Drum SMG"]);

        RegisterAuthored(
            itemDefId: 4,
            key: "ak74",
            displayName: "AK74",
            itemType: ItemType.Weapon,
            weight: 7,
            value: 320,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["Field Carbine"]);

        RegisterAuthored(
            itemDefId: 5,
            key: "ak47",
            displayName: "AK47",
            itemType: ItemType.Weapon,
            weight: 10,
            value: 375,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["Battle Rifle"]);

        RegisterAuthored(
            itemDefId: 6,
            key: "svds",
            displayName: "SVDS",
            itemType: ItemType.Weapon,
            weight: 10,
            value: 550,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["Marksman Rifle"]);

        RegisterAuthored(
            itemDefId: 7,
            key: "pkp",
            displayName: "PKP",
            itemType: ItemType.Weapon,
            weight: 18,
            value: 800,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["Support Machine Gun"]);

        RegisterAuthored(
            itemDefId: 8,
            key: "6b2_body_armor",
            displayName: "6B2 body armor",
            itemType: ItemType.Armor,
            weight: 9,
            value: 95,
            rarity: Rarity.Common,
            displayRarity: DisplayRarity.Common,
            aliases: ["Soft Armor Vest"]);

        RegisterAuthored(
            itemDefId: 9,
            key: "bnti_kirasa_n",
            displayName: "BNTI Kirasa-N",
            itemType: ItemType.Armor,
            weight: 7,
            value: 160,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon,
            aliases: ["Reinforced Vest"]);

        RegisterAuthored(
            itemDefId: 10,
            key: "6b13_assault_armor",
            displayName: "6B13 assault armor",
            itemType: ItemType.Armor,
            weight: 7,
            value: 225,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["Light Plate Carrier"]);

        RegisterAuthored(
            itemDefId: 11,
            key: "fort_defender_2",
            displayName: "FORT Defender-2",
            itemType: ItemType.Armor,
            weight: 22,
            value: 375,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["Medium Plate Carrier"]);

        RegisterAuthored(
            itemDefId: 12,
            key: "6b43_zabralo_sh_body_armor",
            displayName: "6B43 Zabralo-Sh body armor",
            itemType: ItemType.Armor,
            weight: 28,
            value: 450,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["Heavy Plate Carrier"]);

        RegisterAuthored(
            itemDefId: 13,
            key: "nfm_thor",
            displayName: "NFM THOR",
            itemType: ItemType.Armor,
            weight: 19,
            value: 650,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["Assault Plate Carrier"]);

        RegisterCanonical(new Item("Small Backpack", ItemType.Backpack, Value: 25, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            ItemDefId = 14,
            Key = "small_backpack"
        });
        RegisterAuthored(
            itemDefId: 15,
            key: "large_backpack",
            displayName: "Large Backpack",
            itemType: ItemType.Backpack,
            weight: 1,
            value: 50,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon);
        RegisterCanonical(new Item("Tactical Backpack", ItemType.Backpack, Value: 75, Slots: 2, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare, Weight: 2)
        {
            ItemDefId = 16,
            Key = "tactical_backpack"
        });
        RegisterAuthored(
            itemDefId: 17,
            key: "tasmanian_tiger_trooper_35",
            displayName: "Tasmanian Tiger Trooper 35",
            itemType: ItemType.Backpack,
            weight: 2,
            value: 400,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["Hiking Backpack"]);
        RegisterAuthored(
            itemDefId: 18,
            key: "6sh118",
            displayName: "6Sh118",
            itemType: ItemType.Backpack,
            weight: 8,
            value: 600,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["Raid Backpack"]);

        RegisterCanonical(new Item("Medkit", ItemType.Consumable, Value: 30, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            ItemDefId = 19,
            Key = "medkit"
        });
        RegisterCanonical(new Item("Bandage", ItemType.Sellable, Value: 15, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            ItemDefId = 20,
            Key = "bandage"
        });
        RegisterCanonical(new Item("Ammo Box", ItemType.Sellable, Value: 20, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 4)
        {
            ItemDefId = 21,
            Key = "ammo_box"
        });
        RegisterCanonical(new Item("Scrap Metal", ItemType.Material, Value: 18, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 10)
        {
            ItemDefId = 22,
            Key = "scrap_metal"
        });
        RegisterCanonical(new Item("Rare Scope", ItemType.Material, Value: 80, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            ItemDefId = 23,
            Key = "rare_scope"
        });
        RegisterCanonical(new Item("Legendary Trigger Group", ItemType.Material, Value: 150, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            ItemDefId = 24,
            Key = "legendary_trigger_group"
        });
    }

    public static Item Get(string name)
    {
        return GetByLegacyName(name);
    }

    public static Item GetByKey(string key)
    {
        if (!TryGetByKey(key, out var item))
        {
            throw new KeyNotFoundException($"No authored item definition exists for key '{key}'.");
        }

        return item!;
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

    // Legacy compatibility lookup. New code should prefer TryGetByKey/CreateByKey.
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

        if (TryGetByKey(name, out item))
        {
            return true;
        }

        return LegacyNames.TryGetValue(name, out item);
    }

    public static bool TryResolveAuthoredItem(int itemDefId, string? itemKey, string? legacyName, out Item? item)
    {
        if (TryGetByItemDefId(itemDefId, out item) && item is not null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(itemKey) && TryGetByKey(itemKey, out item) && item is not null)
        {
            return true;
        }

        if (itemDefId <= 0
            && string.IsNullOrWhiteSpace(itemKey)
            && !string.IsNullOrWhiteSpace(legacyName)
            && TryGetByLegacyName(legacyName, out item)
            && item is not null)
        {
            return true;
        }

        item = null;
        return false;
    }

    public static bool TryGetKeyByLegacyName(string name, out string key)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            key = string.Empty;
            return false;
        }

        if (TryGetByKey(name, out var item) && item is not null)
        {
            key = item.Key;
            return true;
        }

        if (LegacyNames.TryGetValue(name, out item) && item is not null)
        {
            key = item.Key;
            return true;
        }

        key = string.Empty;
        return false;
    }

    public static bool TryGetItemDefIdByLegacyName(string name, out int itemDefId)
    {
        if (TryGetByLegacyName(name, out var item) && item is not null)
        {
            itemDefId = item.ItemDefId;
            return true;
        }

        itemDefId = 0;
        return false;
    }

    public static bool TryGetItemDefIdByKey(string key, out int itemDefId)
    {
        if (TryGetByKey(key, out var item) && item is not null)
        {
            itemDefId = item.ItemDefId;
            return true;
        }

        itemDefId = 0;
        return false;
    }

    public static bool TryGetByKey(string key, out Item? item)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            item = null;
            return false;
        }

        return ItemsByKey.TryGetValue(key, out item);
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

    public static Item Create(string name)
    {
        return CreateLegacy(name);
    }

    public static Item CreateByKey(string key)
    {
        return GetByKey(key) with { };
    }

    public static Item CreateLegacy(string name)
    {
        return GetByLegacyName(name) with { };
    }

    private static void RegisterAuthored(
        int itemDefId,
        string key,
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
            ItemDefId = itemDefId,
            Key = key
        };

        RegisterCanonical(item);

        foreach (var alias in aliases)
        {
            RegisterAlias(alias, item);
        }
    }

    private static void RegisterCanonical(Item item)
    {
        ItemsById[item.ItemDefId] = item;
        ItemsByKey[item.Key] = item;
        LegacyNames[item.Name] = item;
    }

    private static void RegisterAlias(string alias, Item item)
    {
        LegacyNames[alias] = item with { Name = alias };
    }
}
