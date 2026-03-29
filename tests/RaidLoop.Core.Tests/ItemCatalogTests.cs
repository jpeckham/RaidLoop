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
    private static readonly string StrengthEncumbranceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032701_add_strength_encumbrance.sql"));
    private static readonly string ItemWeightRebalanceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032708_rebalance_item_weights.sql"));
    private static readonly string ForwardRenameMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032804_forward_rename_authored_items.sql"));

    [Fact]
    public void KeyItems_HaveAuthoredValuesGreaterThanOne()
    {
        Assert.True(ItemCatalog.Get("Light Pistol").Value > 1);
        Assert.True(ItemCatalog.Get("Medkit").Value > 1);
        Assert.True(ItemCatalog.Get("Small Backpack").Value > 1);
    }

    [Fact]
    public void AuthoredItems_HaveRequestedWeights()
    {
        Assert.Equal(1, ItemCatalog.Get("Rusty Knife").Weight);
        Assert.Equal(2, ItemCatalog.Get("Light Pistol").Weight);
        Assert.Equal(12, ItemCatalog.Get("Drum SMG").Weight);
        Assert.Equal(7, ItemCatalog.Get("Field Carbine").Weight);
        Assert.Equal(10, ItemCatalog.Get("Marksman Rifle").Weight);
        Assert.Equal(10, ItemCatalog.Get("Battle Rifle").Weight);
        Assert.Equal(18, ItemCatalog.Get("Support Machine Gun").Weight);
        Assert.Equal(9, ItemCatalog.Get("Soft Armor Vest").Weight);
        Assert.Equal(7, ItemCatalog.Get("Reinforced Vest").Weight);
        Assert.Equal(1, ItemCatalog.Get("Small Backpack").Weight);
        Assert.Equal(1, ItemCatalog.Get("Large Backpack").Weight);
        Assert.Equal(2, ItemCatalog.Get("Tactical Backpack").Weight);
        Assert.Equal(2, ItemCatalog.Get("Hiking Backpack").Weight);
        Assert.Equal(8, ItemCatalog.Get("Raid Backpack").Weight);
        Assert.Equal(7, ItemCatalog.Get("Light Plate Carrier").Weight);
        Assert.Equal(22, ItemCatalog.Get("Medium Plate Carrier").Weight);
        Assert.Equal(28, ItemCatalog.Get("Heavy Plate Carrier").Weight);
        Assert.Equal(32, ItemCatalog.Get("Assault Plate Carrier").Weight);
        Assert.Equal(1, ItemCatalog.Get("Medkit").Weight);
        Assert.Equal(1, ItemCatalog.Get("Bandage").Weight);
        Assert.Equal(4, ItemCatalog.Get("Ammo Box").Weight);
        Assert.Equal(10, ItemCatalog.Get("Scrap Metal").Weight);
        Assert.Equal(1, ItemCatalog.Get("Rare Scope").Weight);
        Assert.Equal(1, ItemCatalog.Get("Legendary Trigger Group").Weight);
    }

    [Fact]
    public void HeavierGear_HasHigherWeightThanLighterGear()
    {
        var knife = ItemCatalog.Get("Rusty Knife");
        var field_carbine = ItemCatalog.Get("Field Carbine");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");
        var raidBackpack = ItemCatalog.Get("Raid Backpack");

        Assert.True(field_carbine.Weight > knife.Weight);
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
        var lightPistol = ItemCatalog.Get("Light Pistol");
        var drumSmg = ItemCatalog.Get("Drum SMG");
        var fieldCarbine = ItemCatalog.Get("Field Carbine");
        var battleRifle = ItemCatalog.Get("Battle Rifle");
        var marksmanRifle = ItemCatalog.Get("Marksman Rifle");
        var supportMachineGun = ItemCatalog.Get("Support Machine Gun");

        Assert.Equal(ItemType.Weapon, lightPistol.Type);
        Assert.True(drumSmg.Value > lightPistol.Value);
        Assert.True(fieldCarbine.Value > drumSmg.Value);
        Assert.True(battleRifle.Value > fieldCarbine.Value);
        Assert.True(marksmanRifle.Value > battleRifle.Value);
        Assert.True(supportMachineGun.Value > marksmanRifle.Value);
    }

    [Fact]
    public void StrongerArmor_CostsMoreThanWeakerArmor()
    {
        var softArmorVest = ItemCatalog.Get("Soft Armor Vest");
        var reinforcedVest = ItemCatalog.Get("Reinforced Vest");
        var lightPlateCarrier = ItemCatalog.Get("Light Plate Carrier");
        var mediumPlateCarrier = ItemCatalog.Get("Medium Plate Carrier");
        var heavyPlateCarrier = ItemCatalog.Get("Heavy Plate Carrier");
        var assaultPlateCarrier = ItemCatalog.Get("Assault Plate Carrier");

        Assert.Equal(ItemType.Armor, softArmorVest.Type);
        Assert.True(reinforcedVest.Value > softArmorVest.Value);
        Assert.True(lightPlateCarrier.Value > reinforcedVest.Value);
        Assert.True(mediumPlateCarrier.Value > lightPlateCarrier.Value);
        Assert.True(heavyPlateCarrier.Value > mediumPlateCarrier.Value);
        Assert.True(assaultPlateCarrier.Value > heavyPlateCarrier.Value);
        Assert.True(assaultPlateCarrier.Weight > heavyPlateCarrier.Weight);
    }

    [Fact]
    public void HigherTierBackpacks_CostMoreThanLowerTierBackpacks()
    {
        var smallBackpack = ItemCatalog.Get("Small Backpack");
        var largeBackpack = ItemCatalog.Get("Large Backpack");
        var tacticalBackpack = ItemCatalog.Get("Tactical Backpack");
        var hikingBackpack = ItemCatalog.Get("Hiking Backpack");
        var raidBackpack = ItemCatalog.Get("Raid Backpack");

        Assert.True(largeBackpack.Value > smallBackpack.Value);
        Assert.True(tacticalBackpack.Value > largeBackpack.Value);
        Assert.True(hikingBackpack.Value > tacticalBackpack.Value);
        Assert.True(raidBackpack.Value > hikingBackpack.Value);
    }

    [Theory]
    [InlineData("Bandage")]
    [InlineData("Ammo Box")]
    [InlineData("Medkit")]
    [InlineData("Light Pistol")]
    [InlineData("Field Carbine")]
    [InlineData("Marksman Rifle")]
    [InlineData("Battle Rifle")]
    [InlineData("Support Machine Gun")]
    [InlineData("Light Plate Carrier")]
    [InlineData("Medium Plate Carrier")]
    [InlineData("Heavy Plate Carrier")]
    [InlineData("Assault Plate Carrier")]
    [InlineData("Hiking Backpack")]
    [InlineData("Raid Backpack")]
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
        Assert.Equal(DisplayRarity.Epic, ItemCatalog.Get("Medium Plate Carrier").DisplayRarity);
        Assert.Equal(DisplayRarity.Epic, ItemCatalog.Get("Marksman Rifle").DisplayRarity);
        Assert.Equal(DisplayRarity.Epic, ItemCatalog.Get("Hiking Backpack").DisplayRarity);
        Assert.Equal(DisplayRarity.Legendary, ItemCatalog.Get("Assault Plate Carrier").DisplayRarity);
        Assert.Equal(DisplayRarity.Legendary, ItemCatalog.Get("Support Machine Gun").DisplayRarity);
        Assert.Equal(DisplayRarity.Legendary, ItemCatalog.Get("Raid Backpack").DisplayRarity);
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
        var battle_rifle = ItemCatalog.Get("Battle Rifle");

        Assert.Equal(Rarity.Rare, battle_rifle.Rarity);
        Assert.Equal(DisplayRarity.Rare, battle_rifle.DisplayRarity);
    }

    [Fact]
    public void Kirasa_UsesUncommonDisplayAndLootTier()
    {
        var kirasa = ItemCatalog.Get("Reinforced Vest");

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

        Assert.Contains("'Light Pistol'", migration);
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

        Assert.DoesNotContain("'Marksman Rifle'", migration);
        Assert.DoesNotContain("'Support Machine Gun'", migration);
        Assert.DoesNotContain("'Assault Plate Carrier'", migration);
        Assert.DoesNotContain("'Raid Backpack'", migration);
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

        Assert.Equal(2, totalEncumbrance);
    }

    [Fact]
    public void ItemWeightRebalanceMigration_UpdatesCurrentAuthoritativeWeights()
    {
        var migration = File.ReadAllText(ItemWeightRebalanceMigrationPath);

        Assert.Contains("update game.item_defs", migration);
        Assert.Contains("when 'Rusty Knife' then 1", migration);
        Assert.Contains("when 'Light Pistol' then 2", migration);
        Assert.Contains("when 'Drum SMG' then 12", migration);
        Assert.Contains("when 'Field Carbine' then 7", migration);
        Assert.Contains("when 'Soft Armor Vest' then 9", migration);
        Assert.Contains("when 'Light Plate Carrier' then 7", migration);
        Assert.Contains("when 'Tactical Backpack' then 2", migration);
        Assert.Contains("when 'Raid Backpack' then 8", migration);
        Assert.Contains("when 'Small Backpack' then 1", migration);
        Assert.Contains("when 'Medkit' then 1", migration);
        Assert.Contains("when 'Ammo Box' then 4", migration);
        Assert.Contains("when 'Scrap Metal' then 10", migration);
        Assert.Contains("when 'Legendary Trigger Group' then 1", migration);
    }

    [Fact]
    public void StrengthEncumbranceMigration_UsesAuthoredWeightsAndLuckRunValidation()
    {
        var migration = File.ReadAllText(StrengthEncumbranceMigrationPath);

        Assert.Contains("update game.item_defs", migration);
        Assert.Contains("game.authored_item('Light Pistol')", migration);
        Assert.Contains("game.authored_item('Small Backpack')", migration);
        Assert.Contains("game.authored_item('Tactical Backpack')", migration);
        Assert.Contains("game.authored_item('Medkit')", migration);
        Assert.Contains("game.authored_item('Bandage')", migration);
        Assert.Contains("game.authored_item('Ammo Box')", migration);
        Assert.DoesNotContain("jsonb_build_object('name', 'Light Pistol'", migration);
        Assert.DoesNotContain("jsonb_build_object('name', 'Medkit'", migration);
        Assert.Contains("create or replace function game.item_weight(item_name text)", migration);
        Assert.Contains("create or replace function game.random_luck_run_stats()", migration);
        Assert.Contains("create or replace function game.random_luck_run_loadout_valid(loadout jsonb, stats jsonb)", migration);
        Assert.Contains("create or replace function game.random_luck_run_character()", migration);
    }

    [Fact]
    public void ForwardRenameMigration_UpdatesLegacyItemKeysAndPersistedPayloads()
    {
        var migration = File.ReadAllText(ForwardRenameMigrationPath);

        Assert.Contains("'makarov', 'light_pistol', 'Makarov', 'Light Pistol'", migration);
        Assert.Contains("'nfm_thor', 'assault_plate_carrier', 'NFM THOR', 'Assault Plate Carrier'", migration);
        Assert.Contains("update public.game_saves", migration);
        Assert.Contains("rename_legacy_active_raid", migration);
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
