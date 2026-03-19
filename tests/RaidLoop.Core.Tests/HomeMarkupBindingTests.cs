using System.IO;

namespace RaidLoop.Core.Tests;

public sealed class HomeMarkupBindingTests
{
    private static readonly string AppMarkupPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "App.razor"));
    private static readonly string ProgramPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Program.cs"));
    private static readonly string HomeMarkupPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Home.razor"));
    private static readonly string LoadoutPanelPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "LoadoutPanel.razor"));
    private static readonly string StashPanelPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "StashPanel.razor"));
    private static readonly string PreRaidPanelPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "PreRaidPanel.razor"));
    private static readonly string RaidHudPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "RaidHUD.razor"));
    private static readonly string ShopPanelPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "ShopPanel.razor"));
    private static readonly string ItemTypeIconPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "ItemTypeIcon.razor"));

    [Fact]
    public void HomePassesDynamicStringParametersAsExpressions()
    {
        var markup = File.ReadAllText(HomeMarkupPath);

        Assert.Contains("RaidBlockReason=\"@RaidBlockReason\"", markup);
        Assert.Contains("LuckRunBlockReason=\"@LuckRunBlockReason\"", markup);
        Assert.Contains("AmmoHudText=\"@GetAmmoHudText()\"", markup);
        Assert.Contains("EncounterTitle=\"@GetEncounterTitle()\"", markup);
        Assert.Contains("EncounterDescription=\"@_encounterDescription\"", markup);
        Assert.Contains("EnemyName=\"@_enemyName\"", markup);
        Assert.Contains("LootContainer=\"@_lootContainer\"", markup);
    }

    [Fact]
    public void HomeUsesDisplayRarityColorPalette()
    {
        var markup = File.ReadAllText(HomeMarkupPath);

        Assert.Contains(".rarity-sellonly { color: #808080; }", markup);
        Assert.Contains(".rarity-common { color: #ffffff; }", markup);
        Assert.Contains(".rarity-uncommon { color: #1eff00; }", markup);
        Assert.Contains(".rarity-rare { color: #0070dd; }", markup);
        Assert.Contains(".rarity-epic { color: #ffd100; }", markup);
        Assert.Contains(".rarity-legendary { color: #ff8000; }", markup);
    }

    [Fact]
    public void RarityMarkupUsesDisplayRarityClasses()
    {
        AssertUsesDisplayRarityMarkup(LoadoutPanelPath);
        AssertUsesDisplayRarityMarkup(StashPanelPath);
        AssertUsesDisplayRarityMarkup(PreRaidPanelPath);
        AssertUsesDisplayRarityMarkup(RaidHudPath);
        AssertUsesDisplayRarityMarkup(ShopPanelPath);
    }

    [Fact]
    public void PriceMarkupUsesDollarCurrencyLabels()
    {
        AssertContainsDollarCurrency(HomeMarkupPath);
        AssertContainsDollarCurrency(LoadoutPanelPath);
        AssertContainsDollarCurrency(StashPanelPath);
        AssertContainsDollarCurrency(PreRaidPanelPath);
        AssertContainsDollarCurrency(ShopPanelPath);
    }

    [Fact]
    public void ItemRowsUseIconsInsteadOfTextTypeLabels()
    {
        AssertUsesTypeIcons(LoadoutPanelPath);
        AssertUsesTypeIcons(StashPanelPath);
        AssertUsesTypeIcons(PreRaidPanelPath);
        AssertUsesTypeIcons(RaidHudPath);
    }

    [Fact]
    public void StoragePanelDoesNotShowStandaloneValueText()
    {
        var markup = File.ReadAllText(StashPanelPath);

        Assert.DoesNotContain("$@item.Value", markup);
    }

    [Fact]
    public void ShopPanelShowsBuyPriceInsideButton()
    {
        var markup = File.ReadAllText(ShopPanelPath);

        Assert.Contains("Buy ($@GetBuyPrice(stock.Item.Name))", markup);
        Assert.DoesNotContain("<small>$@GetBuyPrice(stock.Item.Name)</small>", markup);
    }

    [Fact]
    public void HomeDoesNotRenderExtractionSettlementSection()
    {
        var markup = File.ReadAllText(HomeMarkupPath);

        Assert.DoesNotContain("Extraction Settlement", markup);
        Assert.DoesNotContain("_lastExtractedItems", markup);
        Assert.DoesNotContain("_lastExtractedTotalValue", markup);
    }

    [Fact]
    public void RaidHudShowsEquipBeforeLootInDiscoveredLootRows()
    {
        var markup = File.ReadAllText(RaidHudPath);
        var rowActionsIndex = markup.IndexOf("<div class=\"row-actions\">", StringComparison.Ordinal);
        var equipIndex = markup.IndexOf("OnEquipFromDiscovered.InvokeAsync(lootItem)", StringComparison.Ordinal);
        var lootIndex = markup.IndexOf("OnTakeLoot.InvokeAsync(lootItem)", StringComparison.Ordinal);

        Assert.True(rowActionsIndex >= 0);
        Assert.True(equipIndex > rowActionsIndex);
        Assert.True(lootIndex > equipIndex);
    }

    [Fact]
    public void AppUsesAuthGateForGoogleLogin()
    {
        var markup = File.ReadAllText(AppMarkupPath);

        Assert.Contains("<AuthGate>", markup);
        Assert.Contains("</AuthGate>", markup);
    }

    [Fact]
    public void ProgramRegistersSupabaseAuthService()
    {
        var program = File.ReadAllText(ProgramPath);

        Assert.Contains("AddScoped<SupabaseAuthService>", program);
    }

    private static void AssertUsesDisplayRarityMarkup(string path)
    {
        var markup = File.ReadAllText(path);

        Assert.Contains("DisplayRarity.ToString().ToLower()", markup);
        Assert.DoesNotContain(".Rarity.ToString().ToLower()", markup);
    }

    private static void AssertContainsDollarCurrency(string path)
    {
        var markup = File.ReadAllText(path);

        Assert.DoesNotContain("g)</button>", markup);
        Assert.DoesNotContain("&#160;g</small>", markup);
        Assert.DoesNotContain("}g\")", markup);
        Assert.Contains("$", markup);
    }

    private void AssertUsesTypeIcons(string path)
    {
        var markup = File.ReadAllText(path);

        Assert.Contains("<ItemTypeIcon", markup);
        Assert.DoesNotContain("@item.Type", markup);
        Assert.DoesNotContain("@entry.Item.Type", markup);
        Assert.DoesNotContain("@equipped.Type", markup);
        Assert.DoesNotContain("@carried.Type", markup);
        Assert.DoesNotContain("@lootItem.Type", markup);
    }
}
