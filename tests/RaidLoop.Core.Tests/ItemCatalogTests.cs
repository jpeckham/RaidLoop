using RaidLoop.Core;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using System.Reflection;

namespace RaidLoop.Core.Tests;

public sealed class ItemCatalogTests
{
    [Fact]
    public void KeyItems_HaveAuthoredValuesGreaterThanOne()
    {
        Assert.True(ItemCatalog.Get("Makarov").Value > 1);
        Assert.True(ItemCatalog.Get("Medkit").Value > 1);
        Assert.True(ItemCatalog.Get("Small Backpack").Value > 1);
    }

    [Fact]
    public void LargerBackpacks_CostMoreThanSmallerBackpacks()
    {
        var smallBackpack = ItemCatalog.Get("Small Backpack");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");

        Assert.True(tacticalBackpack.Slots > smallBackpack.Slots);
        Assert.True(tacticalBackpack.Value > smallBackpack.Value);
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
        Assert.True(svds.Value > ak74.Value);
        Assert.True(ak47.Value > svds.Value);
        Assert.True(pkp.Value > ak47.Value);
    }

    [Fact]
    public void StrongerArmor_CostsMoreThanWeakerArmor()
    {
        var starterArmor = ItemCatalog.Get("6B2 body armor");
        var assaultArmor = ItemCatalog.Get("6B13 assault armor");
        var epicArmor = ItemCatalog.Get("FORT Defender-2");
        var heavyArmor = ItemCatalog.Get("6B43 Zabralo-Sh body armor");
        var thorArmor = ItemCatalog.Get("NFM THOR");

        Assert.Equal(ItemType.Armor, starterArmor.Type);
        Assert.True(assaultArmor.Value > starterArmor.Value);
        Assert.True(epicArmor.Value > assaultArmor.Value);
        Assert.True(heavyArmor.Value > epicArmor.Value);
        Assert.True(thorArmor.Value > heavyArmor.Value);
    }

    [Fact]
    public void HigherTierBackpacks_CostMoreThanLowerTierBackpacks()
    {
        var smallBackpack = ItemCatalog.Get("Small Backpack");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");
        var trooperBackpack = ItemCatalog.Get("Tasmanian Tiger Trooper 35");
        var raidBackpack = ItemCatalog.Get("6Sh118");

        Assert.True(tacticalBackpack.Value > smallBackpack.Value);
        Assert.True(trooperBackpack.Value > tacticalBackpack.Value);
        Assert.True(raidBackpack.Value > trooperBackpack.Value);
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
        Assert.Equal(DisplayRarity.SellOnly, ItemCatalog.Get("Legendary Trigger Group").DisplayRarity);
    }

    [Fact]
    public void RandomLoadout_UsesAuthoredItemDefinitions()
    {
        var home = new Home();

        var loadout = InvokePrivate<List<Item>>(home, "GenerateRandomLoadout");

        Assert.Contains(ItemCatalog.Get("Makarov"), loadout);
        Assert.Contains(ItemCatalog.Get("Medkit"), loadout);
        Assert.Contains(ItemCatalog.Get("Bandage"), loadout);
        Assert.Contains(ItemCatalog.Get("Ammo Box"), loadout);
        Assert.Contains(loadout, item =>
            item == ItemCatalog.Get("Small Backpack")
            || item == ItemCatalog.Get("Tactical Backpack"));
    }

    [Fact]
    public void RandomLoadout_DoesNotUseEpicOrLegendaryGear()
    {
        var home = new Home();

        var loadout = InvokePrivate<List<Item>>(home, "GenerateRandomLoadout");

        Assert.DoesNotContain(loadout, item =>
            item.DisplayRarity is DisplayRarity.Epic or DisplayRarity.Legendary);
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
}
