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
            ItemCatalog.Create("Makarov"),
            ItemCatalog.Create("PPSH"),
            ItemCatalog.Create("AK74"),
            ItemCatalog.Create("SVDS"),
            ItemCatalog.Create("AK47"),
            ItemCatalog.Create("PKP")
        ]);
    }

    public static LootTable ArmourCrate()
    {
        return new LootTable(ArmourCrateProfile,
        [
            ItemCatalog.Create("6B2 body armor"),
            ItemCatalog.Create("Small Backpack"),
            ItemCatalog.Create("6B13 assault armor"),
            ItemCatalog.Create("Tactical Backpack"),
            ItemCatalog.Create("FORT Defender-2"),
            ItemCatalog.Create("Tasmanian Tiger Trooper 35"),
            ItemCatalog.Create("6B43 Zabralo-Sh body armor"),
            ItemCatalog.Create("NFM THOR"),
            ItemCatalog.Create("6Sh118")
        ]);
    }

    public static LootTable MixedCache()
    {
        return new LootTable(MixedCacheProfile,
        [
            ItemCatalog.Create("Bandage"),
            ItemCatalog.Create("Ammo Box"),
            ItemCatalog.Create("Scrap Metal"),
            ItemCatalog.Create("Medkit"),
            ItemCatalog.Create("PPSH"),
            new Item("Rare Scope", ItemType.Material, Value: 16, Slots: 1, Rarity: Rarity.Rare, DisplayRarity: DisplayRarity.Rare),
            ItemCatalog.Create("AK74"),
            ItemCatalog.Create("SVDS"),
            ItemCatalog.Create("Legendary Trigger Group")
        ]);
    }

    public static LootTable EnemyLoadout()
    {
        return new LootTable(EnemyLoadoutProfile,
        [
            ItemCatalog.Create("Makarov"),
            ItemCatalog.Create("Bandage"),
            ItemCatalog.Create("PPSH"),
            ItemCatalog.Create("6B2 body armor"),
            ItemCatalog.Create("AK74"),
            ItemCatalog.Create("6B13 assault armor"),
            ItemCatalog.Create("SVDS"),
            ItemCatalog.Create("FORT Defender-2"),
            ItemCatalog.Create("AK47"),
            ItemCatalog.Create("PKP"),
            ItemCatalog.Create("NFM THOR")
        ]);
    }
}
