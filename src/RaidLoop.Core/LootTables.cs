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
            ItemCatalog.CreateByKey("makarov"),
            ItemCatalog.CreateByKey("ppsh"),
            ItemCatalog.CreateByKey("ak74"),
            ItemCatalog.CreateByKey("svds"),
            ItemCatalog.CreateByKey("ak47"),
            ItemCatalog.CreateByKey("pkp")
        ]);
    }

    public static LootTable ArmourCrate()
    {
        return new LootTable(ArmourCrateProfile,
        [
            ItemCatalog.CreateByKey("6b2_body_armor"),
            ItemCatalog.CreateByKey("small_backpack"),
            ItemCatalog.CreateByKey("bnti_kirasa_n"),
            ItemCatalog.CreateByKey("large_backpack"),
            ItemCatalog.CreateByKey("6b13_assault_armor"),
            ItemCatalog.CreateByKey("tactical_backpack"),
            ItemCatalog.CreateByKey("fort_defender_2"),
            ItemCatalog.CreateByKey("tasmanian_tiger_trooper_35"),
            ItemCatalog.CreateByKey("6b43_zabralo_sh_body_armor"),
            ItemCatalog.CreateByKey("nfm_thor"),
            ItemCatalog.CreateByKey("6sh118")
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
            ItemCatalog.CreateByKey("ppsh"),
            ItemCatalog.CreateByKey("rare_scope"),
            ItemCatalog.CreateByKey("ak74"),
            ItemCatalog.CreateByKey("svds"),
            ItemCatalog.CreateByKey("legendary_trigger_group")
        ]);
    }

    public static LootTable EnemyLoadout()
    {
        return new LootTable(EnemyLoadoutProfile,
        [
            ItemCatalog.CreateByKey("makarov"),
            ItemCatalog.CreateByKey("bandage"),
            ItemCatalog.CreateByKey("ppsh"),
            ItemCatalog.CreateByKey("6b2_body_armor"),
            ItemCatalog.CreateByKey("bnti_kirasa_n"),
            ItemCatalog.CreateByKey("ak74"),
            ItemCatalog.CreateByKey("6b13_assault_armor"),
            ItemCatalog.CreateByKey("svds"),
            ItemCatalog.CreateByKey("fort_defender_2"),
            ItemCatalog.CreateByKey("ak47"),
            ItemCatalog.CreateByKey("pkp"),
            ItemCatalog.CreateByKey("nfm_thor")
        ]);
    }
}

