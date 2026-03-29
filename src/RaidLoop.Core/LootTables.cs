namespace RaidLoop.Core;

public static class LootTables
{
    private static readonly LootTierProfile WeaponsCrateProfile = new(
        commonWeight: 40,
        uncommonWeight: 12,
        rareWeight: 6,
        epicWeight: 3,
        legendaryWeight: 2);

    private static readonly LootTierProfile ArmourCrateProfile = new(
        commonWeight: 40,
        uncommonWeight: 12,
        rareWeight: 6,
        epicWeight: 3,
        legendaryWeight: 2);

    private static readonly LootTierProfile MixedCacheProfile = new(
        commonWeight: 40,
        uncommonWeight: 12,
        rareWeight: 6,
        epicWeight: 3,
        legendaryWeight: 2);

    private static readonly LootTierProfile EnemyLoadoutProfile = new(
        commonWeight: 55,
        uncommonWeight: 10,
        rareWeight: 3,
        epicWeight: 1,
        legendaryWeight: 1);

    public static LootTable WeaponsCrate()
    {
        return new LootTable(WeaponsCrateProfile,
        [
            ItemCatalog.CreateByKey("light_pistol"),
            ItemCatalog.CreateByKey("drum_smg"),
            ItemCatalog.CreateByKey("field_carbine"),
            ItemCatalog.CreateByKey("marksman_rifle"),
            ItemCatalog.CreateByKey("battle_rifle"),
            ItemCatalog.CreateByKey("support_machine_gun")
        ]);
    }

    public static LootTable ArmourCrate()
    {
        return new LootTable(ArmourCrateProfile,
        [
            ItemCatalog.CreateByKey("soft_armor_vest"),
            ItemCatalog.CreateByKey("small_backpack"),
            ItemCatalog.CreateByKey("reinforced_vest"),
            ItemCatalog.CreateByKey("large_backpack"),
            ItemCatalog.CreateByKey("light_plate_carrier"),
            ItemCatalog.CreateByKey("tactical_backpack"),
            ItemCatalog.CreateByKey("medium_plate_carrier"),
            ItemCatalog.CreateByKey("hiking_backpack"),
            ItemCatalog.CreateByKey("heavy_plate_carrier"),
            ItemCatalog.CreateByKey("assault_plate_carrier"),
            ItemCatalog.CreateByKey("raid_backpack")
        ]);
    }

    public static LootTable MixedCache()
    {
        return new LootTable(MixedCacheProfile,
        [
            ItemCatalog.CreateByKey("bandage"),
            ItemCatalog.CreateByKey("ammo_box"),
            ItemCatalog.CreateByKey("scrap_metal"),
            ItemCatalog.CreateByKey("medkit"),
            ItemCatalog.CreateByKey("drum_smg"),
            ItemCatalog.CreateByKey("rare_scope"),
            ItemCatalog.CreateByKey("field_carbine"),
            ItemCatalog.CreateByKey("marksman_rifle"),
            ItemCatalog.CreateByKey("legendary_trigger_group")
        ]);
    }

    public static LootTable EnemyLoadout()
    {
        return new LootTable(EnemyLoadoutProfile,
        [
            ItemCatalog.CreateByKey("light_pistol"),
            ItemCatalog.CreateByKey("bandage"),
            ItemCatalog.CreateByKey("drum_smg"),
            ItemCatalog.CreateByKey("soft_armor_vest"),
            ItemCatalog.CreateByKey("reinforced_vest"),
            ItemCatalog.CreateByKey("field_carbine"),
            ItemCatalog.CreateByKey("light_plate_carrier"),
            ItemCatalog.CreateByKey("marksman_rifle"),
            ItemCatalog.CreateByKey("medium_plate_carrier"),
            ItemCatalog.CreateByKey("battle_rifle"),
            ItemCatalog.CreateByKey("support_machine_gun"),
            ItemCatalog.CreateByKey("assault_plate_carrier")
        ]);
    }
}
