namespace RaidLoop.Core;

public static class LootTables
{
    private const int CommonWeight = 40;
    private const int UncommonWeight = 12;
    private const int RareWeight = 6;
    private const int LegendaryWeight = 2;
    private const int MixedCacheCommonWeight = 10;
    private const int MixedCacheUncommonWeight = 12;
    private const int MixedCacheRareWeight = 3;
    private const int EnemyLoadoutCommonWeight = 20;
    private const int EnemyLoadoutUncommonWeight = 6;
    private const int EnemyLoadoutRareWeight = 3;

    public static LootTable WeaponsCrate()
    {
        return new LootTable(
        [
            (new Item("Makarov", ItemType.Weapon, 1, Rarity.Common), CommonWeight),
            (new Item("PPSH", ItemType.Weapon, 1, Rarity.Uncommon), UncommonWeight),
            (new Item("AK74", ItemType.Weapon, 1, Rarity.Rare), RareWeight),
            (new Item("AK47", ItemType.Weapon, 1, Rarity.Legendary), LegendaryWeight)
        ]);
    }

    public static LootTable ArmourCrate()
    {
        return new LootTable(
        [
            (new Item("6B2 body armor", ItemType.Armor, 1, Rarity.Common), CommonWeight),
            (new Item("Small Backpack", ItemType.Backpack, 1, Rarity.Uncommon), UncommonWeight),
            (new Item("6B13 assault armor", ItemType.Armor, 1, Rarity.Rare), RareWeight),
            (new Item("6B43 Zabralo-Sh body armor", ItemType.Armor, 1, Rarity.Legendary), LegendaryWeight)
        ]);
    }

    public static LootTable MixedCache()
    {
        return new LootTable(
        [
            (new Item("Bandage", ItemType.Sellable, 1, Rarity.Common), MixedCacheCommonWeight),
            (new Item("Ammo Box", ItemType.Sellable, 1, Rarity.Common), MixedCacheCommonWeight),
            (new Item("Scrap Metal", ItemType.Material, 1, Rarity.Common), MixedCacheCommonWeight),
            (new Item("Medkit", ItemType.Consumable, 1, Rarity.Common), MixedCacheCommonWeight),
            (new Item("PPSH", ItemType.Weapon, 1, Rarity.Uncommon), MixedCacheUncommonWeight),
            (new Item("Rare Scope", ItemType.Material, 1, Rarity.Rare), MixedCacheRareWeight),
            (new Item("AK74", ItemType.Weapon, 1, Rarity.Rare), MixedCacheRareWeight),
            (new Item("Legendary Trigger Group", ItemType.Material, 1, Rarity.Legendary), LegendaryWeight)
        ]);
    }

    public static LootTable EnemyLoadout()
    {
        return new LootTable(
        [
            (new Item("Makarov", ItemType.Weapon, 1, Rarity.Common), EnemyLoadoutCommonWeight),
            (new Item("Bandage", ItemType.Sellable, 1, Rarity.Common), EnemyLoadoutCommonWeight),
            (new Item("PPSH", ItemType.Weapon, 1, Rarity.Uncommon), EnemyLoadoutUncommonWeight),
            (new Item("6B2 body armor", ItemType.Armor, 1, Rarity.Uncommon), EnemyLoadoutUncommonWeight),
            (new Item("AK74", ItemType.Weapon, 1, Rarity.Rare), EnemyLoadoutRareWeight),
            (new Item("6B13 assault armor", ItemType.Armor, 1, Rarity.Rare), EnemyLoadoutRareWeight),
            (new Item("AK47", ItemType.Weapon, 1, Rarity.Legendary), LegendaryWeight)
        ]);
    }
}
