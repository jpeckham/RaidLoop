using RaidLoop.Core;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using System.Reflection;
using System.IO;

namespace RaidLoop.Core.Tests;

public sealed class ItemCatalogTests
{
    private static readonly string RaidStartMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026031807_game_raid_start_functions.sql"));

    [Fact]
    public void KeyItems_HaveAuthoredValuesGreaterThanOne()
    {
        Assert.True(ItemCatalog.Get("Makarov").Value > 1);
        Assert.True(ItemCatalog.Get("Medkit").Value > 1);
        Assert.True(ItemCatalog.Get("Small Backpack").Value > 1);
    }

    [Fact]
    public void AuthoredItems_HaveRequestedWeights()
    {
        Assert.Equal(2, ItemCatalog.Get("Rusty Knife").Weight);
        Assert.Equal(4, ItemCatalog.Get("Makarov").Weight);
        Assert.Equal(8, ItemCatalog.Get("PPSH").Weight);
        Assert.Equal(9, ItemCatalog.Get("AK74").Weight);
        Assert.Equal(18, ItemCatalog.Get("6B13 assault armor").Weight);
        Assert.Equal(3, ItemCatalog.Get("Medkit").Weight);
    }

    [Fact]
    public void HeavierGear_HasHigherWeightThanLighterGear()
    {
        var knife = ItemCatalog.Get("Rusty Knife");
        var ak74 = ItemCatalog.Get("AK74");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");
        var raidBackpack = ItemCatalog.Get("6Sh118");

        Assert.True(ak74.Weight > knife.Weight);
        Assert.True(tacticalBackpack.Weight > ItemCatalog.Get("Small Backpack").Weight);
        Assert.True(raidBackpack.Weight > tacticalBackpack.Weight);
    }

    [Fact]
    public void LargerBackpacks_CostMoreThanSmallerBackpacks()
    {
        var smallBackpack = ItemCatalog.Get("Small Backpack");
        var largeBackpack = ItemCatalog.Get("Large Backpack");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");

        Assert.True(CombatBalance.GetBackpackCapacity(largeBackpack.Name) > CombatBalance.GetBackpackCapacity(smallBackpack.Name));
        Assert.True(CombatBalance.GetBackpackCapacity(tacticalBackpack.Name) > CombatBalance.GetBackpackCapacity(largeBackpack.Name));
        Assert.True(largeBackpack.Value > smallBackpack.Value);
        Assert.True(tacticalBackpack.Value > largeBackpack.Value);
    }

    [Fact]
    public void StrongerWeapons_CostMoreThanWeakerWeapons()
    {
        var makarov = ItemCatalog.Get("Makarov");
        var ppsh = ItemCatalog.Get("PPSH");
        var ak74 = ItemCatalog.Get("AK74");
        var svds = ItemCatalog.Get("SVDS");
        var ak47 = ItemCatalog.Get("AK47");
        var pkp = ItemCatalog.Get("PKP");

        Assert.Equal(ItemType.Weapon, makarov.Type);
        Assert.True(ppsh.Value > makarov.Value);
        Assert.True(ak74.Value > ppsh.Value);
        Assert.True(ak47.Value > ak74.Value);
        Assert.True(svds.Value > ak47.Value);
        Assert.True(pkp.Value > ak47.Value);
    }

    [Fact]
    public void StrongerArmor_CostsMoreThanWeakerArmor()
    {
        var starterArmor = ItemCatalog.Get("6B2 body armor");
        var uncommonArmor = ItemCatalog.Get("BNTI Kirasa-N");
        var assaultArmor = ItemCatalog.Get("6B13 assault armor");
        var epicArmor = ItemCatalog.Get("FORT Defender-2");
        var heavyArmor = ItemCatalog.Get("6B43 Zabralo-Sh body armor");
        var thorArmor = ItemCatalog.Get("NFM THOR");

        Assert.Equal(ItemType.Armor, starterArmor.Type);
        Assert.True(uncommonArmor.Value > starterArmor.Value);
        Assert.True(assaultArmor.Value > uncommonArmor.Value);
        Assert.True(epicArmor.Value > assaultArmor.Value);
        Assert.True(heavyArmor.Value > epicArmor.Value);
        Assert.True(thorArmor.Value > heavyArmor.Value);
    }

    [Fact]
    public void HigherTierBackpacks_CostMoreThanLowerTierBackpacks()
    {
        var smallBackpack = ItemCatalog.Get("Small Backpack");
        var largeBackpack = ItemCatalog.Get("Large Backpack");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");
        var trooperBackpack = ItemCatalog.Get("Tasmanian Tiger Trooper 35");
        var raidBackpack = ItemCatalog.Get("6Sh118");

        Assert.True(largeBackpack.Value > smallBackpack.Value);
        Assert.True(tacticalBackpack.Value > largeBackpack.Value);
        Assert.True(trooperBackpack.Value > tacticalBackpack.Value);
        Assert.True(raidBackpack.Value > trooperBackpack.Value);
    }

    [Theory]
    [InlineData("Bandage")]
    [InlineData("Ammo Box")]
    [InlineData("Medkit")]
    [InlineData("Makarov")]
    [InlineData("AK74")]
    [InlineData("SVDS")]
    [InlineData("AK47")]
    [InlineData("PKP")]
    [InlineData("6B13 assault armor")]
    [InlineData("FORT Defender-2")]
    [InlineData("6B43 Zabralo-Sh body armor")]
    [InlineData("NFM THOR")]
    [InlineData("Tasmanian Tiger Trooper 35")]
    [InlineData("6Sh118")]
    public void RepresentativeItems_SellForAboutQuarterOfBuyPrice(string itemName)
    {
        var item = ItemCatalog.Get(itemName);
        var buyPrice = CombatBalance.GetBuyPrice(itemName);
        var ratio = (double)item.Value / buyPrice;

        Assert.InRange(ratio, 0.24d, 0.26d);
    }

    [Fact]
    public void NewHighTierItems_UseRequestedDisplayRarity()
    {
        Assert.Equal(DisplayRarity.Epic, ItemCatalog.Get("FORT Defender-2").DisplayRarity);
        Assert.Equal(DisplayRarity.Epic, ItemCatalog.Get("SVDS").DisplayRarity);
        Assert.Equal(DisplayRarity.Epic, ItemCatalog.Get("Tasmanian Tiger Trooper 35").DisplayRarity);
        Assert.Equal(DisplayRarity.Legendary, ItemCatalog.Get("NFM THOR").DisplayRarity);
        Assert.Equal(DisplayRarity.Legendary, ItemCatalog.Get("PKP").DisplayRarity);
        Assert.Equal(DisplayRarity.Legendary, ItemCatalog.Get("6Sh118").DisplayRarity);
    }

    [Fact]
    public void SellOnlyItems_AreMarkedAsSellOnly()
    {
        Assert.Equal(DisplayRarity.SellOnly, ItemCatalog.Get("Bandage").DisplayRarity);
        Assert.Equal(DisplayRarity.SellOnly, ItemCatalog.Get("Ammo Box").DisplayRarity);
        Assert.Equal(DisplayRarity.SellOnly, ItemCatalog.Get("Scrap Metal").DisplayRarity);
        Assert.Equal(DisplayRarity.SellOnly, ItemCatalog.Get("Rare Scope").DisplayRarity);
        Assert.Equal(DisplayRarity.SellOnly, ItemCatalog.Get("Legendary Trigger Group").DisplayRarity);
    }

    [Fact]
    public void Ak47_UsesRareDisplayAndLootTier()
    {
        var ak47 = ItemCatalog.Get("AK47");

        Assert.Equal(Rarity.Rare, ak47.Rarity);
        Assert.Equal(DisplayRarity.Rare, ak47.DisplayRarity);
    }

    [Fact]
    public void Kirasa_UsesUncommonDisplayAndLootTier()
    {
        var kirasa = ItemCatalog.Get("BNTI Kirasa-N");

        Assert.Equal(Rarity.Uncommon, kirasa.Rarity);
        Assert.Equal(DisplayRarity.Uncommon, kirasa.DisplayRarity);
        Assert.Equal(ItemType.Armor, kirasa.Type);
    }

    [Fact]
    public void BackpackTierProgression_UsesCommonThenUncommonThenRare()
    {
        var smallBackpack = ItemCatalog.Get("Small Backpack");
        var largeBackpack = ItemCatalog.Get("Large Backpack");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");

        Assert.Equal(Rarity.Common, smallBackpack.Rarity);
        Assert.Equal(DisplayRarity.Common, smallBackpack.DisplayRarity);
        Assert.Equal(Rarity.Uncommon, largeBackpack.Rarity);
        Assert.Equal(DisplayRarity.Uncommon, largeBackpack.DisplayRarity);
        Assert.Equal(Rarity.Rare, tacticalBackpack.Rarity);
    }

    [Fact]
    public void RandomLoadout_UsesAuthoredItemDefinitions()
    {
        var migration = ReadRandomLuckRunLoadoutFunction();

        Assert.Contains("'Makarov'", migration);
        Assert.Contains("'Medkit'", migration);
        Assert.Contains("'Bandage'", migration);
        Assert.Contains("'Ammo Box'", migration);
        Assert.Contains("'Small Backpack'", migration);
        Assert.Contains("'Tactical Backpack'", migration);
    }

    [Fact]
    public void RandomLoadout_DoesNotUseEpicOrLegendaryGear()
    {
        var migration = ReadRandomLuckRunLoadoutFunction();

        Assert.DoesNotContain("'SVDS'", migration);
        Assert.DoesNotContain("'PKP'", migration);
        Assert.DoesNotContain("'NFM THOR'", migration);
        Assert.DoesNotContain("'6Sh118'", migration);
    }

    [Fact]
    public void FallbackKnife_UsesAuthoredItemDefinition()
    {
        var home = new Home();

        SetPrivateField(home, "_mainGame", new GameState([]));
        SetPrivateField(home, "_onPersonItems", new List<OnPersonEntry>());

        InvokePrivate<object?>(home, "EnsureMainCharacterHasWeaponFallback");

        var gameState = GetPrivateField<GameState>(home, "_mainGame");

        Assert.Contains(ItemCatalog.Get("Rusty Knife"), gameState.Stash);
    }

    [Fact]
    public void ExtractableMedkits_UseAuthoredDefinition()
    {
        var inventory = new RaidInventory
        {
            MedkitCount = 2
        };

        var extractableMedkits = inventory
            .GetExtractableItems()
            .Where(item => item.Name == "Medkit")
            .ToList();

        Assert.Equal(2, extractableMedkits.Count);
        Assert.All(extractableMedkits, medkit => Assert.Equal(ItemCatalog.Get("Medkit"), medkit));
    }

    [Fact]
    public void Medkit_Weight_IsIncludedInEncumbrance()
    {
        var items = new[]
        {
            ItemCatalog.Get("Rusty Knife"),
            ItemCatalog.Get("Medkit")
        };

        var totalEncumbrance = CombatBalance.GetTotalEncumbrance(items);

        Assert.Equal(5, totalEncumbrance);
    }

    private static T InvokePrivate<T>(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return (T)method!.Invoke(instance, null)!;
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        return (T)field!.GetValue(instance)!;
    }

    private static string ReadRandomLuckRunLoadoutFunction()
    {
        var migration = File.ReadAllText(RaidStartMigrationPath);
        var start = migration.IndexOf("create or replace function game.random_luck_run_loadout()", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = migration.IndexOf("create or replace function game.random_container_name()", start, StringComparison.Ordinal);
        Assert.True(end > start);
        return migration[start..end];
    }
}
