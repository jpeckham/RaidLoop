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
            ItemCatalog.Create("Light Pistol"),
            ItemCatalog.Create("Drum SMG"),
            ItemCatalog.Create("Field Carbine"),
            ItemCatalog.Create("Marksman Rifle"),
            ItemCatalog.Create("Battle Rifle"),
            ItemCatalog.Create("Support Machine Gun")
        ]);
    }

    public static LootTable ArmourCrate()
    {
        return new LootTable(ArmourCrateProfile,
        [
            ItemCatalog.Create("Soft Armor Vest"),
            ItemCatalog.Create("Small Backpack"),
            ItemCatalog.Create("Reinforced Vest"),
            ItemCatalog.Create("Large Backpack"),
            ItemCatalog.Create("Light Plate Carrier"),
            ItemCatalog.Create("Tactical Backpack"),
            ItemCatalog.Create("Medium Plate Carrier"),
            ItemCatalog.Create("Hiking Backpack"),
            ItemCatalog.Create("Heavy Plate Carrier"),
            ItemCatalog.Create("Assault Plate Carrier"),
            ItemCatalog.Create("Raid Backpack")
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
            ItemCatalog.Create("Drum SMG"),
            ItemCatalog.Create("Rare Scope"),
            ItemCatalog.Create("Field Carbine"),
            ItemCatalog.Create("Marksman Rifle"),
            ItemCatalog.Create("Legendary Trigger Group")
        ]);
    }

    public static LootTable EnemyLoadout()
    {
        return new LootTable(EnemyLoadoutProfile,
        [
            ItemCatalog.Create("Light Pistol"),
            ItemCatalog.Create("Bandage"),
            ItemCatalog.Create("Drum SMG"),
            ItemCatalog.Create("Soft Armor Vest"),
            ItemCatalog.Create("Reinforced Vest"),
            ItemCatalog.Create("Field Carbine"),
            ItemCatalog.Create("Light Plate Carrier"),
            ItemCatalog.Create("Marksman Rifle"),
            ItemCatalog.Create("Medium Plate Carrier"),
            ItemCatalog.Create("Battle Rifle"),
            ItemCatalog.Create("Support Machine Gun"),
            ItemCatalog.Create("Assault Plate Carrier")
        ]);
    }
}
