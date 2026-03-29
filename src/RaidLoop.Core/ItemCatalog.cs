namespace RaidLoop.Core;

public static class ItemCatalog
{
    private static readonly Dictionary<string, Item> ItemsByKey = new(StringComparer.OrdinalIgnoreCase);
    // Legacy label aliases stay as a compatibility bridge while the app moves to key-first lookups.
    private static readonly Dictionary<string, Item> LegacyNames = new(StringComparer.OrdinalIgnoreCase);

    static ItemCatalog()
    {
        RegisterCanonical(new Item("Rusty Knife", ItemType.Weapon, Value: 1, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            Key = "rusty_knife"
        });

        RegisterAuthored(
            key: "light_pistol",
            displayName: "Light Pistol",
            itemType: ItemType.Weapon,
            weight: 2,
            value: 60,
            rarity: Rarity.Common,
            displayRarity: DisplayRarity.Common,
            aliases: ["Makarov"]);

        RegisterAuthored(
            key: "drum_smg",
            displayName: "Drum SMG",
            itemType: ItemType.Weapon,
            weight: 12,
            value: 160,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon,
            aliases: ["PPSH"]);

        RegisterAuthored(
            key: "field_carbine",
            displayName: "Field Carbine",
            itemType: ItemType.Weapon,
            weight: 7,
            value: 320,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["AK74"]);

        RegisterAuthored(
            key: "marksman_rifle",
            displayName: "Marksman Rifle",
            itemType: ItemType.Weapon,
            weight: 10,
            value: 550,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["SVDS"]);

        RegisterAuthored(
            key: "battle_rifle",
            displayName: "Battle Rifle",
            itemType: ItemType.Weapon,
            weight: 10,
            value: 375,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["AK47"]);

        RegisterAuthored(
            key: "support_machine_gun",
            displayName: "Support Machine Gun",
            itemType: ItemType.Weapon,
            weight: 18,
            value: 800,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["PKP"]);

        RegisterAuthored(
            key: "soft_armor_vest",
            displayName: "Soft Armor Vest",
            itemType: ItemType.Armor,
            weight: 9,
            value: 95,
            rarity: Rarity.Common,
            displayRarity: DisplayRarity.Common,
            aliases: ["6B2 body armor"]);

        RegisterAuthored(
            key: "reinforced_vest",
            displayName: "Reinforced Vest",
            itemType: ItemType.Armor,
            weight: 7,
            value: 160,
            rarity: Rarity.Uncommon,
            displayRarity: DisplayRarity.Uncommon,
            aliases: ["BNTI Kirasa-N"]);

        RegisterAuthored(
            key: "light_plate_carrier",
            displayName: "Light Plate Carrier",
            itemType: ItemType.Armor,
            weight: 7,
            value: 225,
            rarity: Rarity.Rare,
            displayRarity: DisplayRarity.Rare,
            aliases: ["6B13 assault armor"]);

        RegisterAuthored(
            key: "medium_plate_carrier",
            displayName: "Medium Plate Carrier",
            itemType: ItemType.Armor,
            weight: 22,
            value: 375,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["FORT Defender-2"]);

        RegisterAuthored(
            key: "heavy_plate_carrier",
            displayName: "Heavy Plate Carrier",
            itemType: ItemType.Armor,
            weight: 28,
            value: 450,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["6B43 Zabralo-Sh body armor"]);

        RegisterAuthored(
            key: "assault_plate_carrier",
            displayName: "Assault Plate Carrier",
            itemType: ItemType.Armor,
            weight: 19,
            value: 650,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["NFM THOR"]);

        RegisterCanonical(new Item("Small Backpack", ItemType.Backpack, Value: 25, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            Key = "small_backpack"
        });
        RegisterCanonical(new Item("Large Backpack", ItemType.Backpack, Value: 50, Slots: 1, Rarity: Rarity.Uncommon, DisplayRarity: DisplayRarity.Uncommon, Weight: 1)
        {
            Key = "large_backpack"
        });
        RegisterCanonical(new Item("Tactical Backpack", ItemType.Backpack, Value: 75, Slots: 2, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare, Weight: 2)
        {
            Key = "tactical_backpack"
        });
        RegisterAuthored(
            key: "hiking_backpack",
            displayName: "Hiking Backpack",
            itemType: ItemType.Backpack,
            weight: 2,
            value: 400,
            rarity: Rarity.Epic,
            displayRarity: DisplayRarity.Epic,
            aliases: ["Tasmanian Tiger Trooper 35"]);
        RegisterAuthored(
            key: "raid_backpack",
            displayName: "Raid Backpack",
            itemType: ItemType.Backpack,
            weight: 8,
            value: 600,
            rarity: Rarity.Legendary,
            displayRarity: DisplayRarity.Legendary,
            aliases: ["6Sh118"]);

        RegisterCanonical(new Item("Medkit", ItemType.Consumable, Value: 30, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.Common, Weight: 1)
        {
            Key = "medkit"
        });
        RegisterCanonical(new Item("Bandage", ItemType.Sellable, Value: 15, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            Key = "bandage"
        });
        RegisterCanonical(new Item("Ammo Box", ItemType.Sellable, Value: 20, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 4)
        {
            Key = "ammo_box"
        });
        RegisterCanonical(new Item("Scrap Metal", ItemType.Material, Value: 18, Slots: 1, Rarity: Rarity.Common, DisplayRarity: DisplayRarity.SellOnly, Weight: 10)
        {
            Key = "scrap_metal"
        });
        RegisterCanonical(new Item("Rare Scope", ItemType.Material, Value: 80, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
            Key = "rare_scope"
        });
        RegisterCanonical(new Item("Legendary Trigger Group", ItemType.Material, Value: 150, Slots: 1, Rarity: Rarity.Legendary, DisplayRarity: DisplayRarity.SellOnly, Weight: 1)
        {
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
        if (TryGetByKey(name, out item))
        {
            return true;
        }

        return LegacyNames.TryGetValue(name, out item);
    }

    public static bool TryGetKeyByLegacyName(string name, out string key)
    {
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

    public static bool TryGetByKey(string key, out Item? item)
    {
        return ItemsByKey.TryGetValue(key, out item);
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
        ItemsByKey[item.Key] = item;
        LegacyNames[item.Name] = item;
    }

    private static void RegisterAlias(string alias, Item item)
    {
        LegacyNames[alias] = item with { Name = alias };
    }
}
