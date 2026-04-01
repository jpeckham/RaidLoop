using System.IO;
using System.Linq;
namespace RaidLoop.Core.Tests;

public sealed class HomeMarkupBindingTests
{
    private static readonly string AppMarkupPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "App.razor"));
    private static readonly string ProgramPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Program.cs"));
    private static readonly string ClientProjectPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "RaidLoop.Client.csproj"));
    private static readonly string ClientAppSettingsPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "appsettings.json"));
    private static readonly string ClientLocalAppSettingsPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "appsettings.Local.json"));
    private static readonly string SupabaseConfigPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "config.toml"));
    private static readonly string InventoryMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026031806_game_inventory_functions.sql"));
    private static readonly string RaidActionMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026031809_game_raid_action_functions.sql"));
    private static readonly string RandomCharacterTimestampMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026031812_fix_random_character_timestamp_format.sql"));
    private static readonly string EmptyLuckRunReadyMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026031813_empty_luck_run_means_ready.sql"));
    private static readonly string BootstrapNormalizationMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026031814_normalize_profile_bootstrap.sql"));
    private static readonly string SellPriceRebalanceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032010_rebalance_sell_prices.sql"));
    private static readonly string LootRarityWeightsMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032111_fix_loot_rarity_weights.sql"));
    private static readonly string D20HitRollMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032201_add_d20_hit_rolls.sql"));
    private static readonly string DexterityMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032202_add_dexterity_stats.sql"));
    private static readonly string ConstitutionMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032304_add_constitution_and_health.sql"));
    private static readonly string MaxHealthHotfixMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032401_fix_zero_player_max_health.sql"));
    private static readonly string WeaponArmorPenetrationMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032203_add_weapon_armor_penetration.sql"));
    private static readonly string D20GunDamageMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032205_remove_gun_malfunctions_and_clear_jams.sql"));
    private static readonly string CombatOutcomeFlavorMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032402_add_combat_outcome_flavor.sql"));
    private static readonly string AuthoredSurpriseEncounterStylesMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032501_add_authored_surprise_encounter_styles.sql"));
    private static readonly string ChallengeDistanceProdUpgradeMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032601_fix_challenge_distance_prod_upgrade.sql"));
    private static readonly string PlayerStatSystemMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032603_add_player_stat_system.sql"));
    private static readonly string RaidSessionPersistenceHotfixMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032606_restore_raid_session_persistence_for_stat_aware_raid_start.sql"));
    private static readonly string ItemIdentityMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032904_add_item_identity_keys.sql"));
    private static readonly string StrengthEncumbranceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032701_add_strength_encumbrance.sql"));
    private static readonly string D20EncumbranceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032703_apply_d20_encumbrance_thresholds.sql"));
    private static readonly string D20EncumbranceCombatMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032704_apply_d20_encumbrance_penalties_to_raid_combat.sql"));
    private static readonly string AuthoritativeShopPurchaseMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032705_fix_authoritative_shop_purchases.sql"));
    private static readonly string HalfCashRespecMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032706_half_cash_respec_cost.sql"));
    private static readonly string ChallengeZeroTravelRebalanceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032801_rebalance_challenge_zero_travel_and_enemy_progression.sql"));
    private static readonly string DerivedMaxHealthMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032802_derive_player_max_health_from_constitution.sql"));
    private static readonly string ExtractHoldBalanceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032803_extract_hold_balance.sql"));
    private static readonly string ItemWeightRebalanceMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032708_rebalance_item_weights.sql"));
    private static readonly string ReallocateStatsJsonNullGuardMigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032702_fix_reallocate_stats_json_null_guard.sql"));
    private static readonly string SupabaseAuthServicePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "SupabaseAuthService.cs"));
    private static readonly string ClientTelemetryServicePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "ClientTelemetryService.cs"));
    private static readonly string ClientTelemetryInterfacePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "IClientTelemetryService.cs"));
    private static readonly string ProfileApiClientPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "ProfileApiClient.cs"));
    private static readonly string ProfileApiInterfacePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "IProfileApiClient.cs"));
    private static readonly string ProfileSaveHandlerPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "functions", "profile-save", "handler.mjs"));
    private static readonly string ItemPresentationCatalogResxPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "ItemPresentationCatalog.resx"));
    private static readonly string HomeMarkupPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Home.razor"));
    private static readonly string ClientIndexPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "index.html"));
    private static readonly string HomeCodeBehindPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Home.razor.cs"));
    private static readonly string ClientCssPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "css", "app.css"));
    private static readonly string StorageScriptPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "js", "storage.js"));
    private static readonly string TelemetryScriptPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "js", "telemetry.js"));
    private static readonly string AuthGatePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "AuthGate.razor"));
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
    private static readonly string CounterPagePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Counter.razor"));
    private static readonly string WeatherPagePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Weather.razor"));
    private static readonly string PublishedServiceWorkerPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "service-worker.published.js"));
    private static readonly string WeatherSampleDataPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "sample-data", "weather.json"));
    private static readonly string ContinuousDeliveryWorkflowPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".github", "workflows", "continuous-delivery-dotnet-blazor-github-pages.yml"));
    private static readonly string SupabaseDeployWorkflowPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".github", "workflows", "supabase-deploy.yml"));
    private static readonly string ReadmePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "README.md"));
    private static readonly string LaunchSettingsPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Properties", "launchSettings.json"));

    [Fact]
    public void HomePassesDynamicStringParametersAsExpressions()
    {
        var markup = File.ReadAllText(HomeMarkupPath);

        Assert.Contains("RaidBlockReason=\"@RaidBlockReason\"", markup);
        Assert.Contains("LuckRunBlockReason=\"@LuckRunBlockReason\"", markup);
        Assert.Contains("EncumbranceText=\"@GetPreRaidEncumbranceText()\"", markup);
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
    public void BlazorErrorUiUsesDarkReadableTheme()
    {
        var css = File.ReadAllText(Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "css", "app.css")));

        Assert.Contains("#blazor-error-ui", css);
        Assert.Contains("background: #111827;", css);
        Assert.Contains("color: #f8fafc;", css);
        Assert.Contains("color: #93c5fd;", css);
        Assert.DoesNotContain("background: #fef3c7;", css);
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

        Assert.Contains("Buy ($@GetBuyPrice(stock.Item))", markup);
        Assert.DoesNotContain("<small>$@stock.Price</small>", markup);
    }

    [Fact]
    public void ClientItemLabelsComeFromAResourceBoundary()
    {
        Assert.True(File.Exists(ItemPresentationCatalogResxPath));
        Assert.DoesNotContain("@entry.Item.Name", File.ReadAllText(LoadoutPanelPath));
        Assert.DoesNotContain("@item.Name", File.ReadAllText(StashPanelPath));
        Assert.DoesNotContain("@item.Name", File.ReadAllText(PreRaidPanelPath));
        Assert.DoesNotContain("@stock.Item.Name", File.ReadAllText(ShopPanelPath));
    }

    [Fact]
    public void HomeWiresPreRaidEncumbranceIntoLoadoutAndInventoryPanels()
    {
        var markup = File.ReadAllText(HomeMarkupPath);
        var loadoutMarkup = File.ReadAllText(LoadoutPanelPath);
        var stashMarkup = File.ReadAllText(StashPanelPath);
        var shopMarkup = File.ReadAllText(ShopPanelPath);

        Assert.Contains("EncumbranceText=\"@GetPreRaidEncumbranceText()\"", markup);
        Assert.Contains("MaxEncumbrance=\"GetMainCharacterMaxEncumbrance()\"", markup);
        Assert.Contains("CanMoveToLoadoutItem=\"CanAddOnPersonItem\"", markup);
        Assert.Contains("CanPurchaseItem=\"CanAddOnPersonItem\"", markup);
        Assert.Contains("<span class=\"encumbrance-label\">Encumbrance:</span>", loadoutMarkup);
        Assert.Contains("<span class=\"encumbrance-value @GetEncumbranceSeverityCssClass()\">@EncumbranceText</span>", loadoutMarkup);
        Assert.Contains("<span class=\"encumbrance-tier @GetEncumbranceSeverityCssClass()\">@GetEncumbranceTierLabel()</span>", loadoutMarkup);
        Assert.Contains("disabled=\"@(!CanMoveToLoadoutItem(item))\"", stashMarkup);
        Assert.Contains("disabled=\"@(!CanBuyItem(stock.Item) || !CanPurchaseItem(stock.Item) || Money < stock.Price)\"", shopMarkup);
    }

    [Fact]
    public void HomeShowsCompactStatStripInPreparingForRaidHeader()
    {
        var homeMarkup = File.ReadAllText(HomeMarkupPath);
        var preRaidMarkup = File.ReadAllText(PreRaidPanelPath);
        var authGateMarkup = File.ReadAllText(AuthGatePath);

        Assert.Contains("DraftStats=\"_draftStats\"", homeMarkup);
        Assert.Contains("AvailableStatPoints=\"_availableStatPoints\"", homeMarkup);
        Assert.Contains("StatsAccepted=\"_statsAccepted\"", homeMarkup);
        Assert.Contains("CanReallocateStats=\"CanReallocateStats\"", homeMarkup);
        Assert.Contains("OnAcceptStats=\"AcceptStatsAsync\"", homeMarkup);
        Assert.Contains("OnReallocateStats=\"ReallocateStatsAsync\"", homeMarkup);
        Assert.Contains("prep-header", homeMarkup);
        Assert.Contains("prep-header-copy", homeMarkup);
        Assert.Contains("prep-header-stats", homeMarkup);
        Assert.Contains("account-strip", homeMarkup);
        Assert.Contains("stat-strip-inline", homeMarkup);
        Assert.Contains("stat-column", homeMarkup);
        Assert.Contains("stat-adjust stat-adjust-up", homeMarkup);
        Assert.Contains("stat-adjust stat-adjust-down", homeMarkup);
        Assert.Contains("Remaining: @_availableStatPoints", homeMarkup);
        Assert.Contains("Accept Stats", homeMarkup);
        Assert.Contains("Re-Allocate (@GetReallocateStatCostLabel())", homeMarkup);
        Assert.DoesNotContain("stat-strip", preRaidMarkup);
        Assert.DoesNotContain("Signed in:", authGateMarkup);
        var topBarSection = ExtractSection(homeMarkup, "<section class=\"panel top-status-bar\">", "</section>");
        Assert.DoesNotContain("stat-strip-inline", topBarSection);
    }

    [Fact]
    public void HomeShowsResultMessageInTopStatusBarInsteadOfBottomStatusParagraph()
    {
        var homeMarkup = File.ReadAllText(HomeMarkupPath);
        var topBarSection = ExtractSection(homeMarkup, "<section class=\"panel top-status-bar\">", "</section>");

        Assert.Contains("status-strip", homeMarkup);
        Assert.Contains("@if (!string.IsNullOrWhiteSpace(_resultMessage))", topBarSection);
        Assert.Contains("<span class=\"status status-strip\">@_resultMessage</span>", topBarSection);
        Assert.DoesNotContain("<p class=\"status\">@_resultMessage</p>", homeMarkup);
    }

    [Fact]
    public void StrengthEncumbranceMigration_WiresWeightLookupEncumbranceAndOverweightChecks()
    {
        var migration = File.ReadAllText(StrengthEncumbranceMigrationPath);

        Assert.Contains("alter table game.item_defs", migration);
        Assert.Contains("add column if not exists weight int not null default 0", migration);
        Assert.Contains("create or replace function game.item_weight(item_name text)", migration);
        Assert.Contains("create or replace function game.max_encumbrance(strength int)", migration);
        Assert.Contains("create or replace function game.current_encumbrance(items jsonb, medkits int default 0)", migration);
        Assert.Contains("create or replace function game.random_luck_run_stats()", migration);
        Assert.Contains("create or replace function game.random_luck_run_loadout_valid(loadout jsonb, stats jsonb)", migration);
        Assert.Contains("create or replace function game.random_luck_run_character()", migration);
        Assert.Contains("create or replace function game.build_raid_snapshot(loadout jsonb, raider_name text, player_max_health int, accepted_stats jsonb default null)", migration);
        Assert.Contains("'encumbrance', game.current_encumbrance(equipped_items || carried_loot, medkits)", migration);
        Assert.Contains("'maxEncumbrance', game.max_encumbrance(coalesce((resolved_stats->>'strength')::int, 8))", migration);
        Assert.Contains("if action = 'take-loot' then", migration);
        Assert.Contains("prospective_encumbrance := current_encumbrance + game.item_weight(coalesce(selected_item->>'name', item_name));", migration);
        Assert.Contains("elsif action in ('equip-from-discovered', 'equip-from-carried') then", migration);
        Assert.Matches(@"if action = 'equip-from-discovered' then\s+prospective_encumbrance := current_encumbrance\s+\+\s+game\.item_weight\(coalesce\(selected_item->>'name', item_name\)\)\s+-\s+coalesce\(game\.item_weight\(previous_item->>'name'\), 0\);", migration);
        Assert.Matches(@"else\s+prospective_encumbrance := current_encumbrance\s+-\s+coalesce\(game\.item_weight\(previous_item->>'name'\), 0\);", migration);
        Assert.Contains("create or replace function game.perform_raid_action_with_encumbrance(action text, payload jsonb, target_user_id uuid default auth.uid())", migration);
        Assert.Contains("when action in ('start-main-raid', 'start-random-raid')", migration);
        Assert.Contains("then game.perform_raid_action_with_encumbrance(action, payload, auth.uid())", migration);
    }

    [Fact]
    public void D20EncumbranceMigration_UsesReducedTableMathAndCombatPenalties()
    {
        Assert.True(File.Exists(D20EncumbranceMigrationPath));

        var migration = File.ReadAllText(D20EncumbranceMigrationPath);

        Assert.Contains("create or replace function game.max_encumbrance(strength int)", migration);
        Assert.Contains("create or replace function game.encumbrance_tier(strength int, carried_weight int)", migration);
        Assert.Contains("create or replace function game.encumbrance_attack_penalty(encumbrance text)", migration);
        Assert.Contains("create or replace function game.encumbrance_max_dex_bonus(encumbrance text)", migration);
        Assert.Contains("heavy_loads int[]", migration);
        Assert.Contains("array[10, 20, 30, 40, 50, 60, 70, 80, 90, 100", migration);
        Assert.Contains("return game.max_encumbrance(strength) / 3;", migration);
        Assert.Contains("return (game.max_encumbrance(strength) * 2) / 3;", migration);
        Assert.Contains("return game.max_encumbrance(strength - 10) * 4;", migration);
        Assert.Contains("when encumbrance = 'Heavy' then 6", migration);
        Assert.Contains("when encumbrance = 'Medium' then 3", migration);
        Assert.Contains("when encumbrance = 'Heavy' then 1", migration);
        Assert.Contains("when encumbrance = 'Medium' then 3", migration);
        Assert.Contains("'encumbranceTier', game.encumbrance_tier(", migration);
        Assert.Contains("player_encumbrance text", migration);
        Assert.Contains("player_attack_penalty int", migration);
        Assert.Contains("player_max_dex_bonus int", migration);
        Assert.Contains("game.encumbrance_attack_penalty(player_encumbrance)", migration);
        Assert.Contains("game.encumbrance_max_dex_bonus(player_encumbrance)", migration);
        Assert.Contains("least(game.ability_modifier(player_dexterity), player_max_dex_bonus)", migration);
    }

    [Fact]
    public void D20EncumbranceCombatMigration_AppliesRuntimeAttackAndDefensePenalties()
    {
        Assert.True(File.Exists(D20EncumbranceCombatMigrationPath));

        var migration = File.ReadAllText(D20EncumbranceCombatMigrationPath);

        Assert.Contains("create or replace function game.perform_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid())", migration);
        Assert.Contains("current_encumbrance int;", migration);
        Assert.Contains("player_encumbrance text;", migration);
        Assert.Contains("player_attack_penalty int;", migration);
        Assert.Contains("player_max_dex_bonus int;", migration);
        Assert.Contains("player_effective_dex_bonus int;", migration);
        Assert.Contains("current_encumbrance := game.current_encumbrance(equipped_items || carried_loot, medkits);", migration);
        Assert.Contains("player_encumbrance := game.encumbrance_tier(", migration);
        Assert.Contains("player_attack_penalty := game.encumbrance_attack_penalty(player_encumbrance);", migration);
        Assert.Contains("player_max_dex_bonus := game.encumbrance_max_dex_bonus(player_encumbrance);", migration);
        Assert.Contains("player_effective_dex_bonus := least(game.ability_modifier(player_dexterity), player_max_dex_bonus);", migration);
        Assert.Contains("attack_bonus := player_effective_dex_bonus - player_attack_penalty;", migration);
        Assert.Contains("attack_bonus := player_effective_dex_bonus - game.weapon_burst_attack_penalty(equipped_weapon_name) - player_attack_penalty;", migration);
        Assert.Contains("attack_bonus := player_effective_dex_bonus - 4 - player_attack_penalty;", migration);
        Assert.Contains("enemy_attack_total,", migration);
        Assert.Contains("player_effective_dex_bonus,", migration);
        Assert.Contains("raid_payload := jsonb_set(raid_payload, '{encumbrance}', to_jsonb(current_encumbrance), true);", migration);
        Assert.Contains("raid_payload := jsonb_set(raid_payload, '{maxEncumbrance}', to_jsonb(game.max_encumbrance(", migration);
        Assert.Contains("raid_payload := jsonb_set(raid_payload, '{encumbranceTier}', to_jsonb(player_encumbrance), true);", migration);
    }

    [Fact]
    public void AuthoritativeShopPurchaseMigration_AlignsBackendItemLookupPriceAndCharismaGating()
    {
        Assert.True(File.Exists(AuthoritativeShopPurchaseMigrationPath));

        var migration = File.ReadAllText(AuthoritativeShopPurchaseMigrationPath);

        Assert.Contains("alter table game.item_defs", migration);
        Assert.Contains("add column if not exists shop_price int not null default 0", migration);
        Assert.Contains("create or replace function game.shop_max_rarity(charisma int)", migration);
        Assert.Contains("create or replace function game.current_shop_charisma(target_user_id uuid default auth.uid())", migration);
        Assert.Contains("create or replace function game.shop_item(item_name text)", migration);
        Assert.Contains("where item_defs.enabled", migration);
        Assert.Contains("and item_defs.shop_enabled", migration);
        Assert.Contains("item_defs.rarity <= game.shop_max_rarity(game.current_shop_charisma())", migration);
        Assert.Contains("create or replace function game.shop_item_price(item_name text)", migration);
        Assert.Contains("item_defs.shop_price", migration);
        Assert.Contains("round((item_defs.shop_price * (1 - (0.05 * greatest(game.ability_modifier(game.current_shop_charisma()), 0))))::numeric)", migration);
    }

    [Fact]
    public void HomeTopBarOmitsStatHeadingAndModifierText()
    {
        var homeMarkup = File.ReadAllText(HomeMarkupPath);
        var preRaidMarkup = File.ReadAllText(PreRaidPanelPath);

        Assert.Contains("CanIncreaseDraftStat=\"CanIncreaseDraftStat\"", homeMarkup);
        Assert.Contains("CanDecreaseDraftStat=\"CanDecreaseDraftStat\"", homeMarkup);
        Assert.Contains("OnIncrementStat=\"IncrementDraftStat\"", homeMarkup);
        Assert.Contains("OnDecrementStat=\"DecrementDraftStat\"", homeMarkup);
        Assert.Contains("@foreach (var statKey in StatOrder)", homeMarkup);
        Assert.Contains("@GetDraftStatValue(statKey)", homeMarkup);
        Assert.Contains("disabled=\"@(!CanIncreaseDraftStat(statKey))\"", homeMarkup);
        Assert.Contains("disabled=\"@(!CanDecreaseDraftStat(statKey))\"", homeMarkup);
        Assert.DoesNotContain("@FormatModifier(GetDraftModifier(statKey))", homeMarkup);
        Assert.DoesNotContain("<h2>Stats</h2>", preRaidMarkup);
    }

    [Fact]
    public void RaidViewDoesNotRenderAcceptedStats()
    {
        var homeMarkup = File.ReadAllText(HomeMarkupPath);
        var raidHudMarkup = File.ReadAllText(RaidHudPath);
        var raidHudInvocation = ExtractSection(homeMarkup, "<RaidHUD", "/>");

        Assert.DoesNotContain("AcceptedStats=\"_acceptedStats\"", raidHudInvocation);
        Assert.DoesNotContain("STR @AcceptedStats.Strength", raidHudMarkup);
        Assert.DoesNotContain("DEX @AcceptedStats.Dexterity", raidHudMarkup);
        Assert.DoesNotContain("CON @AcceptedStats.Constitution", raidHudMarkup);
        Assert.DoesNotContain("INT @AcceptedStats.Intelligence", raidHudMarkup);
        Assert.DoesNotContain("WIS @AcceptedStats.Wisdom", raidHudMarkup);
        Assert.DoesNotContain("CHA @AcceptedStats.Charisma", raidHudMarkup);
    }

    [Fact]
    public void HomePassesRaidEncumbranceAndWeightWarningsToRaidHud()
    {
        var homeMarkup = File.ReadAllText(HomeMarkupPath);
        var raidHudMarkup = File.ReadAllText(RaidHudPath);
        var clientCss = File.ReadAllText(ClientCssPath);

        Assert.Contains("RaidEncumbranceText=\"@GetRaidEncumbranceText()\"", homeMarkup);
        Assert.Contains("CanEquipItem=\"CanEquipRaidItem\"", homeMarkup);
        Assert.Contains("<span class=\"encumbrance-label\">Encumbrance:</span>", raidHudMarkup);
        Assert.Contains("<span class=\"encumbrance-value @GetEncumbranceSeverityCssClass()\">@RaidEncumbranceText</span>", raidHudMarkup);
        Assert.Contains("<span class=\"encumbrance-tier @GetEncumbranceSeverityCssClass()\">@GetEncumbranceTierLabel()</span>", raidHudMarkup);
        Assert.Contains("Too heavy to carry", raidHudMarkup);
        Assert.Contains("Too heavy to equip", raidHudMarkup);
        Assert.Contains(".encumbrance-value.encumbrance-medium", clientCss);
        Assert.Contains(".encumbrance-tier.encumbrance-medium", clientCss);
        Assert.Contains(".encumbrance-value.encumbrance-heavy", clientCss);
        Assert.Contains(".encumbrance-tier.encumbrance-heavy", clientCss);
    }

    [Fact]
    public void ShopPanelUsesCharismaAwarePriceDelegateAndRarityGate()
    {
        var homeMarkup = File.ReadAllText(HomeMarkupPath);
        var shopMarkup = File.ReadAllText(ShopPanelPath);

        Assert.Contains("Stock=\"VisibleShopStock\"", homeMarkup);
        Assert.Contains("CanBuyItem=\"CanBuyItem\"", homeMarkup);
        Assert.Contains("disabled=\"@(!CanBuyItem(stock.Item) || !CanPurchaseItem(stock.Item) || Money < stock.Price)\"", shopMarkup);
    }

    [Fact]
    public void HomeHydratesShopStockFromBootstrapSnapshotInsteadOfHardcodingInventory()
    {
        var codeBehind = File.ReadAllText(HomeCodeBehindPath);
        var migration = File.ReadAllText(PlayerStatSystemMigrationPath.Replace("2026032603_add_player_stat_system.sql", "2026032604_author_shop_stock_from_item_defs.sql"));

        Assert.Contains("_shopStock = snapshot.ShopStock", codeBehind);
        Assert.Contains("new ShopStock(offer, item)", codeBehind);
        Assert.DoesNotContain("new(ItemCatalog.Create(\"Makarov\"))", codeBehind);
        Assert.Contains("add column if not exists shop_enabled boolean not null default false", migration);
        Assert.Contains("create or replace function game.shop_stock()", migration);
        Assert.Contains("jsonb_build_object('ShopStock', game.shop_stock())", migration);
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
    public void RaidHudDefinesIndependentAttackBurstAndFullAutoActions()
    {
        var markup = File.ReadAllText(RaidHudPath);

        Assert.Contains(">Attack</button>", markup);
        Assert.Contains("Use Skill: Burst Fire", markup);
        Assert.Contains("Use Skill: Full Auto", markup);
        Assert.Contains("disabled=\"@(!CanAttackEnabled)\"", markup);
        Assert.Contains("disabled=\"@(!CanBurstFireEnabled)\"", markup);
        Assert.Contains("disabled=\"@(!CanFullAutoEnabled)\"", markup);
        Assert.Contains("public bool CanAttackEnabled", markup);
        Assert.Contains("public bool CanBurstFireEnabled", markup);
        Assert.Contains("public bool CanFullAuto", markup);
        Assert.Contains("public bool CanFullAutoEnabled", markup);
        Assert.Contains("public EventCallback OnFullAuto", markup);
    }

    [Fact]
    public void RaidHudUsesExplicitExtractActions()
    {
        var markup = File.ReadAllText(RaidHudPath);
        var extractionBlockStart = markup.IndexOf("else if (EncounterType == EncounterType.Extraction)", StringComparison.Ordinal);
        var neutralBlockStart = markup.IndexOf("else if (EncounterType == EncounterType.Neutral)", extractionBlockStart, StringComparison.Ordinal);
        var extractionBlock = markup.Substring(extractionBlockStart, neutralBlockStart - extractionBlockStart);
        var neutralBlockEnd = markup.IndexOf("@if (AwaitingDecision || EncounterType == EncounterType.Loot)", neutralBlockStart, StringComparison.Ordinal);
        var neutralBlock = markup.Substring(neutralBlockStart, neutralBlockEnd - neutralBlockStart);
        var extractAttemptIndex = extractionBlock.IndexOf("Attempt Extraction", StringComparison.Ordinal);
        var holdIndex = extractionBlock.IndexOf("Hold at Extract", StringComparison.Ordinal);
        var goDeeperIndex = neutralBlock.IndexOf("Go Deeper", StringComparison.Ordinal);
        var moveTowardIndex = neutralBlock.IndexOf("Move Toward Extract", StringComparison.Ordinal);

        Assert.True(extractionBlockStart >= 0);
        Assert.True(neutralBlockStart > extractionBlockStart);
        Assert.True(neutralBlockEnd > neutralBlockStart);
        Assert.Contains("Attempt Extraction", extractionBlock);
        Assert.Contains("Hold at Extract", extractionBlock);
        Assert.DoesNotContain("Go Deeper", extractionBlock);
        Assert.DoesNotContain("Move Toward Extract", extractionBlock);
        Assert.True(extractAttemptIndex >= 0);
        Assert.True(holdIndex > extractAttemptIndex);
        Assert.Contains("Go Deeper", neutralBlock);
        Assert.Contains("Move Toward Extract", neutralBlock);
        Assert.DoesNotContain("Attempt Extraction", neutralBlock);
        Assert.DoesNotContain("Hold at Extract", neutralBlock);
        Assert.True(goDeeperIndex >= 0);
        Assert.True(moveTowardIndex > goDeeperIndex);
        var continueSearching = string.Concat("Continue", " Searching");
        Assert.DoesNotContain(continueSearching, extractionBlock);
        Assert.DoesNotContain(continueSearching, neutralBlock);
    }

    [Fact]
    public void RaidHudNoLongerShowsMalfunctionStatusOrFixCopy()
    {
        var markup = File.ReadAllText(RaidHudPath);

        Assert.DoesNotContain("Malfunctioned", markup);
        Assert.DoesNotContain("Operational", markup);
        Assert.DoesNotContain("Fix Malfunction", markup);
    }

    [Fact]
    public void HomePassesIndependentCombatActionsToRaidHud()
    {
        var markup = File.ReadAllText(HomeMarkupPath);

        Assert.Contains("CanAttack=\"CanAttack\"", markup);
        Assert.Contains("CanAttackEnabled=\"CanAttackEnabled\"", markup);
        Assert.Contains("CanBurstFire=\"CanBurstFire\"", markup);
        Assert.Contains("CanBurstFireEnabled=\"CanBurstFireEnabled\"", markup);
        Assert.Contains("CanFullAuto=\"CanFullAuto\"", markup);
        Assert.Contains("CanFullAutoEnabled=\"CanFullAutoEnabled\"", markup);
        Assert.Contains("OnAttack=\"AttackAsync\"", markup);
        Assert.Contains("OnBurstFire=\"BurstFireAsync\"", markup);
        Assert.Contains("OnFullAuto=\"FullAutoAsync\"", markup);
    }

    [Fact]
    public void HomePassesOpeningPhaseStateToRaidHud()
    {
        var markup = File.ReadAllText(HomeMarkupPath);

        Assert.Contains("ContactState=\"@_contactState\"", markup);
        Assert.Contains("SurpriseSide=\"@_surpriseSide\"", markup);
        Assert.Contains("InitiativeWinner=\"@_initiativeWinner\"", markup);
        Assert.Contains("OpeningActionsRemaining=\"_openingActionsRemaining\"", markup);
        Assert.Contains("SurprisePersistenceEligible=\"_surprisePersistenceEligible\"", markup);
    }

    [Fact]
    public void RaidHudShowsCompactOpeningPhaseSummaryDuringCombat()
    {
        var markup = File.ReadAllText(RaidHudPath);

        Assert.Contains("class=\"opening-phase\"", markup);
        Assert.Contains("You spotted them first.", markup);
        Assert.Contains("They ambushed you.", markup);
        Assert.Contains("You won initiative.", markup);
        Assert.Contains("SurpriseSide", markup);
        Assert.Contains("InitiativeWinner", markup);
    }

    [Fact]
    public void HomeWiresFullAutoToRaidAction()
    {
        var codeBehind = File.ReadAllText(HomeCodeBehindPath);

        Assert.Contains("private async Task FullAutoAsync()", codeBehind);
        Assert.Contains("ExecuteRaidActionAsync(\"full-auto\"", codeBehind);
    }

    [Fact]
    public void AppUsesAuthGateForGoogleLogin()
    {
        var markup = File.ReadAllText(AppMarkupPath);

        Assert.Contains("<AuthGate>", markup);
        Assert.Contains("</AuthGate>", markup);
    }

    [Fact]
    public void AuthGateSupportsLocalEmailPasswordAuthAndKeepsGoogleForNonLocal()
    {
        var markup = File.ReadAllText(AuthGatePath);
        var localBranchStart = markup.IndexOf("@if (HostEnvironment.Environment == \"Local\")", StringComparison.Ordinal);
        var localBranchEnd = markup.IndexOf("else", localBranchStart, StringComparison.Ordinal);
        var localBranch = markup.Substring(localBranchStart, localBranchEnd - localBranchStart);

        Assert.Contains("@inject Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment HostEnvironment", markup);
        Assert.Contains("HostEnvironment.Environment == \"Local\"", markup);
        Assert.Contains("SignInWithGoogleAsync", localBranch);
        Assert.Contains("Sign in with Google", localBranch);
        Assert.Contains("type=\"email\"", markup);
        Assert.Contains("type=\"password\"", markup);
        Assert.Contains(">Sign in</button>", markup);
        Assert.Contains(">Sign up</button>", markup);
        Assert.Contains("_authErrorMessage", markup);
    }

    [Fact]
    public void ProgramRegistersSupabaseAuthService()
    {
        var program = File.ReadAllText(ProgramPath);

        Assert.Contains("AddScoped<SupabaseAuthService>", program);
    }

    [Fact]
    public void ProgramRegistersClientTelemetryService()
    {
        var program = File.ReadAllText(ProgramPath);
        var telemetryService = File.ReadAllText(ClientTelemetryServicePath);
        var telemetryInterface = File.ReadAllText(ClientTelemetryInterfacePath);

        Assert.Contains("AddScoped<IClientTelemetryService, ClientTelemetryService>()", program);
        Assert.Contains("IJSRuntime", telemetryService);
        Assert.Contains("RaidLoopTelemetry.reportError", telemetryService);
        Assert.Contains("ReportErrorAsync", telemetryInterface);
    }

    [Fact]
    public void ClientTelemetryServiceDoesNotThrowWhenJsReportingFails()
    {
        var telemetryService = File.ReadAllText(ClientTelemetryServicePath);

        Assert.Contains("try", telemetryService);
        Assert.Contains("catch", telemetryService);
        Assert.Contains("Console.Error.WriteLine", telemetryService);
    }

    [Fact]
    public void ClientAppSettingsIncludesPostHogConfiguration()
    {
        var appSettings = File.ReadAllText(ClientAppSettingsPath);

        Assert.Contains("\"PostHog\"", appSettings);
        Assert.Contains("\"ProjectKey\": \"phc_UfelMasDJpt4iUgbFqDg8i0PkbDGDXFpicrSg6SOojb\"", appSettings);
        Assert.Contains("\"Host\": \"https://us.i.posthog.com\"", appSettings);
        Assert.Contains("\"SessionReplayEnabled\": true", appSettings);
    }

    [Fact]
    public void LocalAppSettingsPointToLocalSupabaseAndTestAppSettingsAreRemoved()
    {
        var localSettings = File.ReadAllText(ClientLocalAppSettingsPath);

        Assert.Contains("\"Url\": \"http://127.0.0.1:54321\"", localSettings);
        Assert.Contains("\"PublishableKey\": \"sb_publishable_ACJWlzQHlZjBrEguHvfOxg_3BJgxAaH\"", localSettings);
        Assert.Contains("\"ProjectKey\": \"phc_UfelMasDJpt4iUgbFqDg8i0PkbDGDXFpicrSg6SOojb\"", localSettings);
        Assert.False(File.Exists(Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "appsettings.Test.json"))));
    }

    [Fact]
    public void LaunchSettingsUseLocalEnvironmentByDefault()
    {
        var launchSettings = File.ReadAllText(LaunchSettingsPath);

        Assert.Contains("\"ASPNETCORE_ENVIRONMENT\": \"Local\"", launchSettings);
        Assert.DoesNotContain("\"ASPNETCORE_ENVIRONMENT\": \"Development\"", launchSettings);
    }

    [Fact]
    public void ClientProjectPinsStandaloneWasmEnvironmentToLocalForDebugAndProductionForRelease()
    {
        var project = File.ReadAllText(ClientProjectPath);

        Assert.Contains("<PropertyGroup Condition=\"'$(Configuration)' == 'Debug'\">", project);
        Assert.Contains("<WasmApplicationEnvironmentName>Local</WasmApplicationEnvironmentName>", project);
        Assert.Contains("<PropertyGroup Condition=\"'$(Configuration)' == 'Release'\">", project);
        Assert.Contains("<WasmApplicationEnvironmentName>Production</WasmApplicationEnvironmentName>", project);
    }

    [Fact]
    public void ReadmeDocumentsLocalSupabaseTddWorkflowInContributingSection()
    {
        var readme = File.ReadAllText(ReadmePath);

        Assert.Contains("## Contributing", readme);
        Assert.Contains("npx supabase start", readme);
        Assert.Contains("npx supabase db reset", readme);
        Assert.Contains("write the smallest failing test first", readme);
        Assert.Contains("dotnet test RaidLoop.sln", readme);
        Assert.Contains("hosted Supabase", readme);
    }

    [Fact]
    public void SupabaseAuthServiceUsesPkceFlowForOAuthCallback()
    {
        var authService = File.ReadAllText(SupabaseAuthServicePath);

        Assert.Contains("FlowType = Constants.OAuthFlowType.PKCE", authService);
        Assert.Contains("providerState.PKCEVerifier", authService);
        Assert.Contains("ExchangeCodeForSession", authService);
        Assert.Contains("RedirectTo = GetCurrentUriWithoutQueryOrFragment()", authService);
        Assert.Contains("_navigationManager.NavigateTo(GetCurrentPathWithoutQueryOrFragment(), replace: true);", authService);
        Assert.DoesNotContain("RedirectTo = _navigationManager.BaseUri", authService);
    }

    [Fact]
    public void SupabaseAuthServiceSupportsEmailPasswordSignInAndSignUp()
    {
        var authService = File.ReadAllText(SupabaseAuthServicePath);

        Assert.Contains("public async Task SignInWithEmailPasswordAsync(string email, string password)", authService);
        Assert.Contains("public async Task SignUpWithEmailPasswordAsync(string email, string password)", authService);
        Assert.Contains("SignIn(email, password)", authService);
        Assert.Contains("SignUp(email, password", authService);
        Assert.Contains("await PersistSessionAsync", authService);
    }

    [Fact]
    public void SupabaseAuthServiceForcesLocalSignOutState()
    {
        var authService = File.ReadAllText(SupabaseAuthServicePath);

        Assert.Contains("_isSignedOutLocally", authService);
        Assert.Contains("public bool IsAuthenticated => !_isSignedOutLocally && _client?.Auth.CurrentSession is not null;", authService);
        Assert.Contains("catch", authService);
        Assert.Contains("await ClearPersistedSessionAsync()", authService);
    }

    [Fact]
    public void SupabaseAuthServiceRefreshesSessionOnlyWhenAccessTokenIsNearExpiry()
    {
        var authService = File.ReadAllText(SupabaseAuthServicePath);

        Assert.Contains("session?.ExpiresAt().Subtract(TimeSpan.FromMinutes(1)) <= DateTime.UtcNow", authService);
        Assert.Contains("await _client.Auth.RefreshSession();", authService);
        Assert.Contains("await ClearPersistedSessionAsync();", authService);
        Assert.Contains("_isSignedOutLocally = true;", authService);
        Assert.DoesNotContain("await _client.Auth.RetrieveSessionAsync();", authService);
        Assert.Contains("public async Task<string> GetAccessTokenAsync()", authService);
    }

    [Fact]
    public void SupabaseFunctionsDisableGatewayJwtVerification()
    {
        var config = File.ReadAllText(SupabaseConfigPath);

        Assert.Contains("[functions.profile-bootstrap]", config);
        Assert.Contains("[functions.profile-save]", config);
        Assert.Contains("[functions.game-action]", config);
        Assert.Contains("verify_jwt = false", config);
    }

    [Fact]
    public void GameActionMigrationRoutesNewRaidMovementActionsServerSide()
    {
        var migration = File.ReadAllText(RaidActionMigrationPath);

        Assert.Contains("attack", migration);
        Assert.Contains("burst-fire", migration);
        Assert.Contains("reload", migration);
        Assert.Contains("flee", migration);
        Assert.Contains("use-medkit", migration);
        Assert.Contains("take-loot", migration);
        Assert.Contains("drop-carried", migration);
        Assert.Contains("drop-equipped", migration);
        Assert.Contains("equip-from-discovered", migration);
        Assert.Contains("equip-from-carried", migration);
        Assert.Contains("go-deeper", migration);
        Assert.Contains("move-toward-extract", migration);
        Assert.Contains("stay-at-extract", migration);
        Assert.Contains("attempt-extract", migration);
        Assert.Contains("challenge", migration);
        Assert.Contains("distanceFromExtract", migration);
        Assert.Contains("challenge := challenge + 1", migration);
        Assert.Contains("distanceFromExtract := greatest(distanceFromExtract - 1, 0)", migration);
        Assert.Contains("if distanceFromExtract = 0 then", migration);
        Assert.Contains("elsif action = 'attempt-extract' then", migration);
        Assert.DoesNotContain("continue-searching", migration);
        Assert.Contains("select raid_sessions.profile, raid_sessions.payload", migration);
    }

    [Fact]
    public void RaidEncounterMigrationTracksChallengeDistanceAndDrift()
    {
        var migration = File.ReadAllText(AuthoredSurpriseEncounterStylesMigrationPath);

        Assert.Contains("create or replace function game.generate_raid_encounter", migration);
        Assert.Contains("moving_to_extract boolean default false", migration);
        Assert.Contains("challenge", migration);
        Assert.Contains("distanceFromExtract", migration);
        Assert.Contains("random() < 0.1", migration);
        Assert.Contains("distanceFromExtract := distanceFromExtract + 1", migration);
        Assert.Contains("You drifted one step away from extract.", migration);
        Assert.DoesNotContain(string.Concat("extract", "Progress"), migration);
        Assert.DoesNotContain("continue-searching", migration);
    }

    [Fact]
    public void LuckRunSettlementIsServerAuthoritative()
    {
        var inventoryMigration = File.ReadAllText(InventoryMigrationPath);
        var raidActionMigration = File.ReadAllText(RaidActionMigrationPath);

        Assert.Contains("create or replace function game.settle_random_character", inventoryMigration);
        Assert.Contains("jsonb_array_length(coalesce(normalized_random_character->'inventory', '[]'::jsonb)) = 0", inventoryMigration);
        Assert.Contains("timezone('utc', now()) + interval '5 minutes'", inventoryMigration);
        Assert.Contains("game.settle_random_character(", inventoryMigration);
        Assert.Contains("game.settle_random_character(", raidActionMigration);
        Assert.Contains("extractable_items", raidActionMigration);
    }

    [Fact]
    public void LuckRunSettlementNormalizesUtcTimestampStringsForClientContracts()
    {
        var migration = File.ReadAllText(RandomCharacterTimestampMigrationPath);

        Assert.Contains("replace(trim(both '\"' from normalized_available_at::text), ' ', 'T')", migration);
        Assert.Contains("|| '+00:00'", migration);
        Assert.Contains("position('+' in available_at_text) = 0", migration);
    }

    [Fact]
    public void EmptyLuckRunSettlementResetsToReadyStateInsteadOfStartingCooldown()
    {
        var migration = File.ReadAllText(EmptyLuckRunReadyMigrationPath);

        Assert.Contains("normalized_random_character := null;", migration);
        Assert.Contains("normalized_available_at := to_jsonb('0001-01-01T00:00:00+00:00'::text);", migration);
        Assert.DoesNotContain("interval '5 minutes'", migration);
    }

    [Fact]
    public void ProfileBootstrapNormalizesSavedPayloadBeforeReturningSnapshot()
    {
        var migration = File.ReadAllText(BootstrapNormalizationMigrationPath);

        Assert.Contains("create or replace function public.profile_bootstrap()", migration);
        Assert.Contains("game.normalize_save_payload(game.bootstrap_player(auth.uid()))", migration);
    }

    [Fact]
    public void SellPriceRebalanceMigrationDefinesQuarterSellValuesForKeyItems()
    {
        var migration = File.ReadAllText(SellPriceRebalanceMigrationPath);

        Assert.Contains("when 'Medkit' then jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 30", migration);
        Assert.Contains("when 'AK74' then jsonb_build_object('name', 'AK74', 'type', 0, 'value', 320", migration);
        Assert.Contains("when 'NFM THOR' then jsonb_build_object('name', 'NFM THOR', 'type', 1, 'value', 650", migration);
    }

    [Fact]
    public void LootRarityWeightsMigrationReplacesFlatHighTierRaidLootRolls()
    {
        var migration = File.ReadAllText(LootRarityWeightsMigrationPath);

        Assert.Contains("create or replace function game.random_enemy_loadout()", migration);
        Assert.Contains("create or replace function game.random_loot_items_for_container(container_name text)", migration);
        Assert.Contains("floor(random() * 70)::int", migration);
        Assert.Contains("floor(random() * 63)::int", migration);
        Assert.Contains("roll < 55", migration);
        Assert.Contains("roll < 40", migration);
        Assert.Contains("game.authored_item('SVDS')", migration);
        Assert.Contains("game.authored_item('FORT Defender-2')", migration);
        Assert.DoesNotContain("case floor(random() * 5)::int", migration);
        Assert.DoesNotContain("case floor(random() * 4)::int", migration);
    }

    [Fact]
    public void D20HitRollMigrationAddsNaturalOneNaturalTwentyAndMissHandlingToLiveRaidCombat()
    {
        var migration = File.ReadAllText(D20HitRollMigrationPath);

        Assert.Contains("create or replace function game.roll_attack_d20", migration);
        Assert.Contains("floor(random() * 20)::int + 1", migration);
        Assert.Contains("if roll = 1 then", migration);
        Assert.Contains("if roll = 20 then", migration);
        Assert.Contains("roll + coalesce(attack_bonus, 0) >= coalesce(defense, 10)", migration);
        Assert.Contains("create or replace function game.perform_raid_action", migration);
        Assert.Contains("game.roll_attack_d20(0, 10)", migration);
        Assert.Contains("You miss", migration);
        Assert.Contains(" misses you.", migration);
        Assert.Contains("You hit", migration);
        Assert.Contains(" hits you for ", migration);
    }

    [Fact]
    public void DexterityMigrationBackfillsPlayerDexterityAndWiresDexToLiveCombat()
    {
        var migration = File.ReadAllText(DexterityMigrationPath);

        Assert.Contains("create or replace function game.normalize_save_payload(payload jsonb)", migration);
        Assert.Contains("'playerDexterity', coalesce((payload->>'playerDexterity')::int, (payload->>'PlayerDexterity')::int, 10)", migration);
        Assert.Contains("create or replace function game.default_save_payload()", migration);
        Assert.Contains("'playerDexterity', 10", migration);
        Assert.Contains("create or replace function game.build_raid_snapshot(loadout jsonb, raider_name text)", migration);
        Assert.Contains("'enemyDexterity',", migration);
        Assert.Contains("create or replace function game.ability_modifier(score int)", migration);
        Assert.Contains("return floor((score - 10) / 2.0)::int", migration);
        Assert.Contains("create or replace function game.roll_attack_d20(attack_bonus int default 0, defense int default 10)", migration);
        Assert.Contains("game.roll_attack_d20(game.ability_modifier(", migration);
        Assert.Contains("10 + game.ability_modifier(", migration);
    }

    [Fact]
    public void ConstitutionMigrationBackfillsPlayerConstitutionAndDerivedMaxHealth()
    {
        var migration = File.ReadAllText(ConstitutionMigrationPath);

        Assert.Contains("create or replace function game.normalize_save_payload(payload jsonb)", migration);
        Assert.Contains("'playerConstitution', coalesce((payload->>'playerConstitution')::int, (payload->>'PlayerConstitution')::int, 10)", migration);
        Assert.Contains("'playerMaxHealth', coalesce((payload->>'playerMaxHealth')::int, (payload->>'PlayerMaxHealth')::int,", migration);
        Assert.Contains("10 + (2 * coalesce((payload->>'playerConstitution')::int, (payload->>'PlayerConstitution')::int, 10))", migration);
        Assert.Contains("create or replace function game.default_save_payload()", migration);
        Assert.Contains("'playerConstitution', 10", migration);
        Assert.Contains("'playerMaxHealth', 30", migration);
    }

    [Fact]
    public void RaidSqlUsesPersistedPlayerMaxHealthInsteadOfHardCodedThirty()
    {
        var raidStartMigration = File.ReadAllText(Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026031807_game_raid_start_functions.sql")));
        var raidActionMigration = File.ReadAllText(RaidActionMigrationPath);

        Assert.Contains("player_max_health", raidStartMigration);
        Assert.DoesNotContain("'health', 30", raidStartMigration);
        Assert.Contains("player_max_health", raidActionMigration);
        Assert.DoesNotContain("health := greatest(coalesce((raid_payload->>'health')::int, 30), 0);", raidActionMigration);
        Assert.DoesNotContain("health := least(30, health + 10);", raidActionMigration);
    }

    [Fact]
    public void MaxHealthHotfixMigrationRepairsNonPositiveSavedMaxHealthValues()
    {
        var migration = File.ReadAllText(MaxHealthHotfixMigrationPath);

        Assert.Contains("create or replace function game.normalize_save_payload(payload jsonb)", migration);
        Assert.Contains("normalized_player_constitution int", migration);
        Assert.Contains("derived_player_max_health int", migration);
        Assert.Contains("normalized_player_max_health int", migration);
        Assert.Contains("coalesce(nullif(greatest(coalesce((payload->>'playerMaxHealth')::int, (payload->>'PlayerMaxHealth')::int, 0), 0), 0), derived_player_max_health)", migration);
        Assert.Contains("update public.game_saves", migration);
        Assert.Contains("payload = game.normalize_save_payload(payload)", migration);
        Assert.Contains("payload is distinct from game.normalize_save_payload(payload)", migration);
    }

    [Fact]
    public void PlayerStatSystemMigrationBackfillsEditableStatPayloadAndRaidAcceptanceGate()
    {
        Assert.True(File.Exists(PlayerStatSystemMigrationPath));

        var migration = File.ReadAllText(PlayerStatSystemMigrationPath);

        Assert.Contains("create or replace function game.normalize_save_payload(payload jsonb)", migration);
        Assert.Contains("accepted_stats_source jsonb := coalesce(payload->'acceptedStats', payload->'AcceptedStats', '{}'::jsonb);", migration);
        Assert.Contains("draft_stats_source jsonb := coalesce(payload->'draftStats', payload->'DraftStats', '{}'::jsonb);", migration);
        Assert.Contains("'acceptedStats', jsonb_build_object(", migration);
        Assert.Contains("'draftStats', jsonb_build_object(", migration);
        Assert.Contains("'strength', coalesce((accepted_stats_source->>'strength')::int, (accepted_stats_source->>'Strength')::int, 8)", migration);
        Assert.Contains("'dexterity', coalesce((accepted_stats_source->>'dexterity')::int, (accepted_stats_source->>'Dexterity')::int, 8)", migration);
        Assert.Contains("'constitution', coalesce((accepted_stats_source->>'constitution')::int, (accepted_stats_source->>'Constitution')::int, 8)", migration);
        Assert.Contains("'intelligence', coalesce((accepted_stats_source->>'intelligence')::int, (accepted_stats_source->>'Intelligence')::int, 8)", migration);
        Assert.Contains("'wisdom', coalesce((accepted_stats_source->>'wisdom')::int, (accepted_stats_source->>'Wisdom')::int, 8)", migration);
        Assert.Contains("'charisma', coalesce((accepted_stats_source->>'charisma')::int, (accepted_stats_source->>'Charisma')::int, 8)", migration);
        Assert.Contains("'availableStatPoints', coalesce((payload->>'availableStatPoints')::int, (payload->>'AvailableStatPoints')::int, 27)", migration);
        Assert.Contains("'statsAccepted', coalesce((payload->>'statsAccepted')::boolean, (payload->>'StatsAccepted')::boolean, false)", migration);
        Assert.Contains("create or replace function game.default_save_payload()", migration);
        Assert.Contains("'availableStatPoints', 27", migration);
        Assert.Contains("'statsAccepted', false", migration);
        Assert.Contains("update public.game_saves", migration);
        Assert.Contains("payload = game.normalize_save_payload(payload)", migration);
        Assert.Contains("payload is distinct from game.normalize_save_payload(payload)", migration);
        Assert.Contains("if not coalesce((save_payload->>'statsAccepted')::boolean, false) then", migration);
        Assert.Contains("return save_payload;", migration);
        Assert.Contains("accepted_stats := coalesce(save_payload->'acceptedStats', jsonb_build_object(", migration);
        Assert.Contains("create or replace function game.apply_profile_action(action text, payload jsonb, target_user_id uuid default auth.uid())", migration);
        Assert.Contains("when 'accept-stats' then", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{acceptedStats}', normalized_draft_stats, true);", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{statsAccepted}', 'true'::jsonb, true);", migration);
        Assert.Contains("when 'reallocate-stats' then", migration);
        Assert.Contains("coalesce((save_payload->>'money')::int, 0) >= 5000", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{availableStatPoints}', to_jsonb(27), true);", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{statsAccepted}', 'false'::jsonb, true);", migration);
    }

    [Fact]
    public void RaidSessionPersistenceHotfixRestoresSessionWritesToStatAwareRaidStart()
    {
        Assert.True(File.Exists(RaidSessionPersistenceHotfixMigrationPath));

        var migration = File.ReadAllText(RaidSessionPersistenceHotfixMigrationPath);

        Assert.Contains("create or replace function game.start_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid())", migration);
        Assert.Contains("accepted_stats jsonb;", migration);
        Assert.Contains("if not coalesce((save_payload->>'statsAccepted')::boolean, false) then", migration);
        Assert.Contains("raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health, accepted_stats);", migration);
        Assert.Contains("insert into public.raid_sessions (user_id, profile, payload)", migration);
        Assert.Contains("values (target_user_id, 'main', raid_snapshot)", migration);
        Assert.Contains("values (target_user_id, 'random', raid_snapshot)", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{activeRaid}', raid_snapshot, true);", migration);
        Assert.Contains("update public.game_saves", migration);
    }

    [Fact]
    public void ReallocateStatsHotfixTreatsJsonNullActiveRaidAsOutOfRaid()
    {
        Assert.True(File.Exists(ReallocateStatsJsonNullGuardMigrationPath));

        var migration = File.ReadAllText(ReallocateStatsJsonNullGuardMigrationPath);

        Assert.Contains("create or replace function game.apply_profile_action(action text, payload jsonb, target_user_id uuid default auth.uid())", migration);
        Assert.Contains("active_raid := coalesce(save_payload->'activeRaid', 'null'::jsonb);", migration);
        Assert.Contains("when 'reallocate-stats' then", migration);
        Assert.Contains("if active_raid = 'null'::jsonb and coalesce((save_payload->>'money')::int, 0) >= 5000 then", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{draftStats}', normalized_draft_stats, true);", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{availableStatPoints}', to_jsonb(27), true);", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{statsAccepted}', 'false'::jsonb, true);", migration);
    }

    [Fact]
    public void HalfCashRespecMigration_ReplacesFixedRespecFeeWithRoundedHalfCash()
    {
        Assert.True(File.Exists(HalfCashRespecMigrationPath));

        var migration = File.ReadAllText(HalfCashRespecMigrationPath);

        Assert.Contains("create or replace function game.apply_profile_action(action text, payload jsonb, target_user_id uuid default auth.uid())", migration);
        Assert.Contains("respec_cost int;", migration);
        Assert.Contains("respec_cost := round((coalesce((save_payload->>'money')::int, 0) / 2.0)::numeric)::int;", migration);
        Assert.Contains("if active_raid = 'null'::jsonb and coalesce((save_payload->>'money')::int, 0) >= respec_cost then", migration);
        Assert.Contains("save_payload := jsonb_set(save_payload, '{money}', to_jsonb(coalesce((save_payload->>'money')::int, 0) - respec_cost), true);", migration);
        Assert.DoesNotContain(">= 5000", migration);
        Assert.DoesNotContain("- 5000", migration);
    }

    [Fact]
    public void ItemWeightRebalanceMigration_UpdatesCurrentAuthoritativeWeights()
    {
        Assert.True(File.Exists(ItemWeightRebalanceMigrationPath));

        var migration = File.ReadAllText(ItemWeightRebalanceMigrationPath);

        Assert.Contains("update game.item_defs", migration);
        Assert.Contains("when 'Rusty Knife' then 1", migration);
        Assert.Contains("when 'Makarov' then 2", migration);
        Assert.Contains("when 'PPSH' then 12", migration);
        Assert.Contains("when 'AK74' then 7", migration);
        Assert.Contains("when '6B2 body armor' then 9", migration);
        Assert.Contains("when '6B13 assault armor' then 7", migration);
        Assert.Contains("when 'Tactical Backpack' then 2", migration);
        Assert.Contains("when '6Sh118' then 8", migration);
        Assert.Contains("when 'Small Backpack' then 1", migration);
        Assert.Contains("when 'Medkit' then 1", migration);
        Assert.Contains("when 'Ammo Box' then 4", migration);
        Assert.Contains("when 'Scrap Metal' then 10", migration);
        Assert.Contains("when 'Legendary Trigger Group' then 1", migration);
    }

    [Fact]
    public void ChallengeZeroTravelRebalanceMigration_AuthorsTieredEnemiesAndLowTierExtractLoot()
    {
        Assert.True(File.Exists(ChallengeZeroTravelRebalanceMigrationPath));

        var migration = File.ReadAllText(ChallengeZeroTravelRebalanceMigrationPath);

        Assert.Contains("challenge_0_enemy_loadout", migration);
        Assert.Contains("challenge_1_enemy_loadout", migration);
        Assert.Contains("challenge_2_enemy_loadout", migration);
        Assert.Contains("challenge_3_enemy_loadout", migration);
        Assert.Contains("challenge_4_enemy_loadout", migration);
        Assert.Contains("challenge_5_enemy_loadout", migration);
        Assert.Contains("challenge_0_travel_cache", migration);
        Assert.Contains("challenge_0_extract_cache", migration);
        Assert.Contains("alter table game.encounter_table_entries", migration);
        Assert.Contains("add column if not exists challenge_min int not null default 0", migration);
        Assert.Contains("add column if not exists challenge_max_exclusive int not null default 2147483647", migration);
        Assert.Contains("add column if not exists enemy_dexterity int not null default 10", migration);
        Assert.Contains("add column if not exists enemy_constitution int not null default 10", migration);
        Assert.Contains("add column if not exists enemy_strength int not null default 10", migration);
        Assert.Contains("create or replace function game.challenge_enemy_loadout_table(challenge int)", migration);
        Assert.Contains("create or replace function game.challenge_enemy_stats(challenge int)", migration);
        Assert.Contains("create or replace function game.challenge_encounter_loot_table(entry_key text, loot_table_key text, challenge int)", migration);
        Assert.Contains("'makarov'", migration);
        Assert.DoesNotContain("'ppsh', 10),\r\n    ('challenge0", migration);
        Assert.Contains("'small_backpack'", migration);
        Assert.Contains("'large_backpack'", migration);
        Assert.DoesNotContain("'ppsh', 'challenge_0_enemy_loadout'", migration);
        Assert.DoesNotContain("'6b2_body_armor', 'challenge_0_enemy_loadout'", migration);
        Assert.Contains("'6b2_body_armor'", migration);
        Assert.Contains("'bnti_kirasa_n'", migration);
        Assert.Contains("'ak74'", migration);
        Assert.Contains("'fort_defender_2'", migration);
        Assert.Contains("'pkp'", migration);
        Assert.Contains("weight = case entry_key", migration);
        Assert.Contains("when 'raid_combat_travel_player_spots_camp' then 60", migration);
        Assert.Contains("when 'raid_neutral_travel_area_clear' then 80", migration);
        Assert.Contains("when 'raid_loot_travel_filing_cache' then 140", migration);
        Assert.Contains("when 'raid_combat_extract_player_spots_guard' then 70", migration);
        Assert.Contains("when 'raid_neutral_extract_route_clear' then 80", migration);
        Assert.Contains("when 'raid_loot_extract_abandoned_cache' then 120", migration);
        Assert.Contains("challenge_min = 0", migration);
        Assert.Contains("challenge_max_exclusive = 2147483647", migration);
        Assert.Contains("enemy_loadout := game.random_enemy_loadout_from_table(game.challenge_enemy_loadout_table(challenge));", migration);
        Assert.Contains("enemy_stats := game.challenge_enemy_stats(challenge);", migration);
        Assert.Contains("selected_loot_table_key := game.challenge_encounter_loot_table(", migration);
    }

    [Fact]
    public void DerivedMaxHealthMigration_RecomputesMaxHealthFromAcceptedConstitution()
    {
        Assert.True(File.Exists(DerivedMaxHealthMigrationPath));

        var migration = File.ReadAllText(DerivedMaxHealthMigrationPath);

        Assert.Contains("create or replace function game.normalize_save_payload(payload jsonb)", migration);
        Assert.Contains("accepted_stats_source jsonb := coalesce(payload->'acceptedStats', payload->'AcceptedStats', '{}'::jsonb);", migration);
        Assert.Contains("normalized_player_constitution int := greatest(coalesce((accepted_stats_source->>'constitution')::int, (accepted_stats_source->>'Constitution')::int, 8), 0);", migration);
        Assert.Contains("derived_player_max_health int := 10 + (2 * normalized_player_constitution);", migration);
        Assert.Contains("'playerMaxHealth', derived_player_max_health", migration);
        Assert.Contains("update public.game_saves", migration);
        Assert.Contains("create or replace function game.apply_profile_action(action text, payload jsonb, target_user_id uuid default auth.uid())", migration);
        Assert.Contains("'playerConstitution', coalesce((coalesce(save_payload->'acceptedStats', accepted_stats)->>'constitution')::int, 8)", migration);
        Assert.Contains("'playerMaxHealth', 10 + (2 * coalesce((coalesce(save_payload->'acceptedStats', accepted_stats)->>'constitution')::int, 8))", migration);
        Assert.DoesNotContain("coalesce(nullif(greatest(coalesce((payload->>'playerMaxHealth')::int, (payload->>'PlayerMaxHealth')::int, 0), 0), 0), derived_player_max_health)", migration);
        Assert.DoesNotContain("'playerMaxHealth', coalesce((save_payload->>'playerMaxHealth')::int, 26)", migration);
    }

    [Fact]
    public void SupabaseMigrationVersionsAreUnique()
    {
        var migrationDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations"));

        var duplicateVersions = Directory.GetFiles(migrationDirectory, "*.sql")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name!)
            .GroupBy(static name => name.Split('_')[0], StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.OrderBy(name => name, StringComparer.Ordinal))}")
            .ToArray();

        Assert.Empty(duplicateVersions);
    }

    [Fact]
    public void WeaponArmorPenetrationMigrationAddsReusablePenetrationAndArmorMitigationHelpers()
    {
        var migration = File.ReadAllText(WeaponArmorPenetrationMigrationPath);

        Assert.Contains("create or replace function game.weapon_armor_penetration", migration);
        Assert.Contains("when weapon_name = 'Makarov' then", migration);
        Assert.Contains("when weapon_name = 'AK47' then", migration);
        Assert.Contains("when weapon_name = 'SVDS' then", migration);
        Assert.Contains("create or replace function game.armor_damage_reduction", migration);
        Assert.Contains("when armor_name = 'NFM THOR' then 6", migration);
        Assert.Contains("when armor_name = '6B43 Zabralo-Sh body armor' then 5", migration);
        Assert.Contains("when armor_name = 'FORT Defender-2' then 4", migration);
        Assert.Contains("when armor_name = '6B13 assault armor' then 3", migration);
        Assert.Contains("when armor_name = '6B2 body armor' then 1", migration);
        Assert.Contains("create or replace function game.apply_armor_damage_reduction", migration);
        Assert.Contains("greatest(0,", migration);
        Assert.Contains("greatest(1,", migration);
        Assert.Contains("equipped_weapon_name := coalesce(equipped_weapon->>'name', 'Rusty Knife');", migration);
        Assert.Contains("enemy_armor_name := coalesce(game.raid_find_equipped_item(enemy_loadout, 1)->>'name', '');", migration);
        Assert.Contains("enemy_weapon_name := coalesce(game.raid_find_equipped_item(enemy_loadout, 0)->>'name', 'Rusty Knife');", migration);
        Assert.Contains("game.weapon_armor_penetration(equipped_weapon_name)", migration);
        Assert.Contains("game.weapon_armor_penetration(enemy_weapon_name)", migration);
        Assert.Contains("game.apply_armor_damage_reduction(damage, enemy_armor_name, game.weapon_armor_penetration(equipped_weapon_name))", migration);
        Assert.Contains("game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name))", migration);
        Assert.DoesNotContain("reduced_damage := greatest(1, incoming - case", migration);
        Assert.Contains("Burst Fire deals %s.", migration);
    }

    [Fact]
    public void D20GunDamageMigrationAddsFireModeHelpersAndFullAutoRaidAction()
    {
        var migration = File.ReadAllText(D20GunDamageMigrationPath);

        Assert.Contains("create or replace function public.game_action(action text, payload jsonb)", migration);
        Assert.Contains("create or replace function game.weapon_supports_single_shot", migration);
        Assert.Contains("create or replace function game.weapon_supports_burst_fire", migration);
        Assert.Contains("create or replace function game.weapon_supports_full_auto", migration);
        Assert.Contains("create or replace function game.weapon_burst_attack_penalty", migration);
        Assert.Contains("create or replace function game.roll_weapon_damage_d20", migration);
        Assert.Contains("when 'Makarov' then 6", migration);
        Assert.Contains("when 'PPSH' then 4", migration);
        Assert.Contains("when 'AK74' then 8", migration);
        Assert.Contains("when 'AK47' then 10", migration);
        Assert.Contains("when 'SVDS' then 12", migration);
        Assert.Contains("when 'PKP' then 12", migration);
        Assert.Contains("action in ('attack', 'burst-fire', 'full-auto')", migration);
        Assert.Contains("ammo < 3", migration);
        Assert.Contains("ammo < 10", migration);
        Assert.Contains("'full-auto'", migration);
        Assert.Contains("when 'Makarov' then 3", migration);
        Assert.Contains("else 2", migration);
        Assert.Contains("game.weapon_burst_attack_penalty(equipped_weapon_name)", migration);
        Assert.Contains("game.roll_attack_d20(game.ability_modifier(player_dexterity) - 4", migration);
        Assert.Contains("elsif action = 'reload' then", migration);
        Assert.DoesNotContain("weapon_malfunction", migration);
        Assert.DoesNotContain("Weapon malfunctioned", migration);
        Assert.DoesNotContain("Weapon is malfunctioned", migration);
        Assert.Contains("update public.raid_sessions", migration);
        Assert.Contains("payload #- '{weaponMalfunction}'", migration);
        Assert.Contains("update public.game_saves", migration);
        Assert.Contains("payload #- '{activeRaid,weaponMalfunction}'", migration);
    }

    [Fact]
    public void D20GunDamageMigrationPinsCombatOutcomeFlavorForMissEvadeArmorAndHit()
    {
        var migration = File.ReadAllText(CombatOutcomeFlavorMigrationPath);

        Assert.Contains("armor_hit_bonus int not null default 0", migration);
        Assert.Contains("create or replace function game.armor_hit_bonus", migration);
        Assert.Contains("create or replace function game.classify_attack_outcome", migration);
        Assert.Contains("create or replace function game.describe_player_attack_outcome", migration);
        Assert.Contains("create or replace function game.describe_enemy_attack_outcome", migration);
        Assert.Contains("attack_total < 10", migration);
        Assert.Contains("attack_total < 10 + dodge_bonus", migration);
        Assert.Contains("attack_total < 10 + dodge_bonus + armor_bonus", migration);
        Assert.Contains("when 'miss'", migration);
        Assert.Contains("when 'evaded'", migration);
        Assert.Contains("when 'armor-absorbed'", migration);
        Assert.Contains("when 'hit'", migration);
        Assert.Contains("enemy_armor_bonus", migration);
        Assert.Contains("player_armor_bonus", migration);
        Assert.Contains("player_attack_total", migration);
        Assert.Contains("enemy_attack_total", migration);
        Assert.Contains("attack_outcome := game.classify_attack_outcome(", migration);
        Assert.Contains("game.describe_player_attack_outcome", migration);
        Assert.Contains("game.describe_enemy_attack_outcome", migration);
        Assert.Contains("evades your attack", migration);
        Assert.Contains("absorbed by armor", migration);
        Assert.Contains("armor absorbs", migration);
        Assert.Contains("6b43_zabralo_sh_body_armor", migration);
    }

    [Fact]
    public void AuthoredSurpriseEncounterStylesMigrationPinsContactStatesAndFamilies()
    {
        var migration = File.ReadAllText(AuthoredSurpriseEncounterStylesMigrationPath);

        Assert.Contains("add column if not exists contact_state text not null default 'MutualContact'", migration);
        Assert.Contains("encounter_table_entries_contact_state_check", migration);
        Assert.Contains("update game.encounter_table_entries", migration);
        Assert.Contains("default_raid_travel", migration);
        Assert.Contains("loot_interruption", migration);
        Assert.Contains("extract_approach", migration);
        Assert.Contains("PlayerAmbush", migration);
        Assert.Contains("EnemyAmbush", migration);
        Assert.Contains("MutualContact", migration);
        Assert.Contains("raid_combat_travel_player_spots_camp", migration);
        Assert.Contains("raid_neutral_travel_area_clear", migration);
        Assert.Contains("raid_loot_travel_filing_cache", migration);
        Assert.Contains("raid_combat_loot_player_hears_movement", migration);
        Assert.Contains("raid_combat_extract_mutual_contact", migration);
        Assert.Contains("raid_neutral_extract_route_clear", migration);
        Assert.Contains("raid_loot_extract_abandoned_cache", migration);
        Assert.Contains("You spot an enemy camp before they see you.", migration);
        Assert.Contains("The route is quiet for the moment.", migration);
        Assert.Contains("You find a small cache while moving through the area.", migration);
        Assert.Contains("The final stretch to extract is quiet.", migration);
        Assert.Contains("You spot an abandoned cache near the extraction route.", migration);
        Assert.Contains("You hear movement while looting and catch them before they spot you.", migration);
        Assert.Contains("You and a guard on the extraction route notice each other at the same time.", migration);
        Assert.Contains("selected_combat_table_key text", migration);
        Assert.Contains("selected_combat_table_key := 'extract_approach';", migration);
        Assert.Contains("selected_combat_table_key := 'loot_interruption';", migration);
        Assert.Contains("selected_combat_table_key := 'default_raid_travel';", migration);
        Assert.Contains("where entries.table_key = selected_combat_table_key", migration);
    }

    [Fact]
    public void ChallengeDistanceProdUpgradeMigrationPinsForwardUpgradeForLiveEnvironments()
    {
        Assert.True(File.Exists(ChallengeDistanceProdUpgradeMigrationPath));

        var migration = File.ReadAllText(ChallengeDistanceProdUpgradeMigrationPath);

        Assert.Contains("create or replace function game.build_raid_snapshot", migration);
        Assert.Contains("'challenge', 0", migration);
        Assert.Contains("'distanceFromExtract', 3", migration);
        Assert.Contains("create or replace function game.generate_raid_encounter", migration);
        Assert.Contains("create or replace function game.perform_raid_action", migration);
        Assert.Contains("action = 'go-deeper'", migration);
        Assert.Contains("action = 'stay-at-extract'", migration);
        Assert.Contains("distance_from_extract = 0", migration);
        Assert.Contains("create or replace function public.game_action", migration);
        Assert.Contains("'stay-at-extract'", migration);
        var encounterSelectionBlock = ExtractSection(
            migration,
            "if selected_entry.encounter_type = 'Combat' then",
            "if selected_entry.encounter_type = 'Loot' then");
        Assert.Contains("challenge", encounterSelectionBlock);
        Assert.Contains("selected_combat_table_key", encounterSelectionBlock);
        Assert.Contains("updated_payload := jsonb_set(updated_payload, '{enemyConstitution}'", migration);
        Assert.Contains("updated_payload := jsonb_set(updated_payload, '{enemyStrength}'", migration);
        var deathDropBlock = ExtractSection(
            migration,
            "if enemy_health <= 0 then",
            "attack_roll := floor(random() * 20)::int + 1;");
        Assert.Contains("enemy_dropped_items", deathDropBlock);
        Assert.Contains("enemy_loadout", deathDropBlock);
        Assert.DoesNotContain("game.random_enemy_loadout()", deathDropBlock);
    }

    [Fact]
    public void ItemIdentityMigrationAddsSurrogateKeyAndKeyedPayloadBackfill()
    {
        Assert.True(File.Exists(ItemIdentityMigrationPath), $"Expected item identity migration at '{ItemIdentityMigrationPath}'.");

        var migration = File.ReadAllText(ItemIdentityMigrationPath);

        Assert.Contains("add column if not exists item_def_id int generated always as identity", migration);
        Assert.Contains("item_key text not null", migration);
        Assert.Contains("unique (item_key)", migration);
        Assert.Contains("update game.item_defs", migration);
        Assert.Contains("update public.game_saves", migration);
        Assert.Contains("update public.raid_sessions", migration);
        Assert.Contains("itemKey", migration);
    }

    [Fact]
    public void ExtractHoldBalanceMigrationDefinesExtractHoldActionsAndState()
    {
        Assert.True(File.Exists(ExtractHoldBalanceMigrationPath));

        var migration = File.ReadAllText(ExtractHoldBalanceMigrationPath);

        Assert.Contains("start-extract-hold", migration);
        Assert.Contains("resolve-extract-hold", migration);
        Assert.Contains("'{extractHoldActive}'", migration);
        Assert.Contains("'{holdAtExtractUntil}'", migration);
        Assert.Contains("Extract Hold Encounter Table", migration);
        Assert.Contains("extract_hold", migration);
        Assert.Contains("pg_get_functiondef", migration);
    }

    [Fact]
    public void ClientDoesNotExposeGenericProfileSnapshotSavePath()
    {
        var profileApiClient = File.ReadAllText(ProfileApiClientPath);
        var profileApiInterface = File.ReadAllText(ProfileApiInterfacePath);
        var codeBehind = File.ReadAllText(HomeCodeBehindPath);
        var profileSaveHandler = File.ReadAllText(ProfileSaveHandlerPath);

        Assert.DoesNotContain("Task SaveAsync", profileApiInterface);
        Assert.DoesNotContain("profile-save", profileApiClient);
        Assert.DoesNotContain("SaveAllAsync", codeBehind);
        Assert.DoesNotContain("BuildSnapshot()", codeBehind);
        Assert.Contains("Profile saving is no longer supported", profileSaveHandler);
    }

    [Fact]
    public void HomeInjectsProfileApiClientInsteadOfStashStorage()
    {
        var markup = File.ReadAllText(HomeMarkupPath);

        Assert.Contains("@inject IProfileApiClient Profiles", markup);
        Assert.Contains("@inject IGameActionApiClient Actions", markup);
        Assert.Contains("@inject IClientTelemetryService Telemetry", markup);
        Assert.DoesNotContain("@inject StashStorage Storage", markup);
    }

    [Fact]
    public void HomeBootstrapsFromProfileApiClient()
    {
        var codeBehind = File.ReadAllText(HomeCodeBehindPath);

        Assert.Contains("await Profiles.BootstrapAsync()", codeBehind);
        Assert.DoesNotContain("Storage.LoadAsync()", codeBehind);
    }

    [Fact]
    public void HomeHandlesUnauthorizedBootstrapBySigningOut()
    {
        var codeBehind = File.ReadAllText(HomeCodeBehindPath);

        Assert.Contains("catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)", codeBehind);
        Assert.Contains("Telemetry.ReportErrorAsync", codeBehind);
        Assert.Contains("await AuthService.SignOutAsync()", codeBehind);
    }

    [Fact]
    public void HomeReportsActionFailuresAndBootstrapFailuresThroughTelemetry()
    {
        var codeBehind = File.ReadAllText(HomeCodeBehindPath);

        Assert.Contains("Telemetry.ReportErrorAsync", codeBehind);
        Assert.Contains("ExecuteProfileActionAsync", codeBehind);
        Assert.Contains("ExecuteRaidActionAsync", codeBehind);
    }

    [Fact]
    public void SupabaseAuthServiceReportsSessionFailuresThroughTelemetry()
    {
        var authService = File.ReadAllText(SupabaseAuthServicePath);

        Assert.Contains("IClientTelemetryService", authService);
        Assert.Contains("Telemetry.ReportErrorAsync", authService);
        Assert.Contains("RefreshSession", authService);
        Assert.Contains("SignOutAsync", authService);
    }

    [Fact]
    public void DebugStorageScriptDoesNotProbeAuthUserEndpoint()
    {
        var script = File.ReadAllText(StorageScriptPath);

        Assert.DoesNotContain("/auth/v1/user", script);
        Assert.DoesNotContain("authUserProbe", script);
    }

    [Fact]
    public void ClientIndexLoadsTelemetryScriptAndTelemetryScriptInstallsErrorHandlers()
    {
        var index = File.ReadAllText(ClientIndexPath);
        var telemetry = File.ReadAllText(TelemetryScriptPath);

        Assert.Contains("js/telemetry.js", index);
        Assert.Contains("fetch(", telemetry);
        Assert.Contains("appsettings.json", telemetry);
        Assert.Contains("window.onerror", telemetry);
        Assert.Contains("unhandledrejection", telemetry);
        Assert.Contains("console.error", telemetry);
        Assert.Contains("posthog", telemetry, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TelemetryScriptCapturesBlazorFatalBannerState()
    {
        var telemetry = File.ReadAllText(TelemetryScriptPath);

        Assert.Contains("client_blazor_fatal", telemetry);
        Assert.Contains("MutationObserver", telemetry);
        Assert.Contains("blazor-error-ui", telemetry);
    }

    [Fact]
    public void ProgramRegistersProfileApiClientInsteadOfStashStorage()
    {
        var program = File.ReadAllText(ProgramPath);

        Assert.Contains("AddScoped<IProfileApiClient>", program);
        Assert.Contains("AddScoped<IGameActionApiClient>", program);
        Assert.DoesNotContain("AddScoped<StashStorage>", program);
    }

    [Fact]
    public void PreRaidPanelTreatsEmptyLuckRunInventoryAsStartableState()
    {
        var markup = File.ReadAllText(PreRaidPanelPath);

        Assert.Contains("var hasLuckRunLoot = RandomCharacter is not null && RandomCharacter.Inventory.Count > 0;", markup);
        Assert.Contains("@if (!hasLuckRunLoot)", markup);
        Assert.DoesNotContain("@if (RandomCharacter is null)", markup);
    }

    [Fact]
    public void ContinuousDeliveryWorkflowCoordinatesSupabaseAndPagesInOnePushFlow()
    {
        var workflow = File.ReadAllText(ContinuousDeliveryWorkflowPath);
        var supabaseWorkflow = File.ReadAllText(SupabaseDeployWorkflowPath);

        Assert.DoesNotContain("workflow_run:", workflow);
        Assert.Contains("deploy-supabase:", workflow);
        Assert.Contains("continuous-delivery:", workflow);
        Assert.Contains("needs: [detect-supabase-changes, deploy-supabase]", workflow);
        Assert.Contains("always()", workflow);
        Assert.Contains("needs.detect-supabase-changes.outputs.supabase_changed != 'true'", workflow);
        Assert.Contains("needs.deploy-supabase.result == 'success'", workflow);
        Assert.Contains("needs.deploy-supabase.result == 'skipped'", workflow);
        Assert.DoesNotContain("push:", supabaseWorkflow);
        Assert.Contains("workflow_dispatch:", supabaseWorkflow);
    }

    [Fact]
    public void NavMenuDoesNotLinkToBlazorStarterPages()
    {
        var markup = File.ReadAllText(Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Layout", "NavMenu.razor")));

        Assert.DoesNotContain("href=\"counter\"", markup);
        Assert.DoesNotContain("href=\"weather\"", markup);
        Assert.DoesNotContain("> Counter", markup);
        Assert.DoesNotContain("> Weather", markup);
    }

    [Fact]
    public void BlazorStarterPagesAndWeatherSampleAreRemoved()
    {
        Assert.False(File.Exists(CounterPagePath));
        Assert.False(File.Exists(WeatherPagePath));
        Assert.False(File.Exists(WeatherSampleDataPath));
    }

    [Fact]
    public void PublishedServiceWorkerDerivesBasePathFromServiceWorkerLocation()
    {
        var serviceWorker = File.ReadAllText(PublishedServiceWorkerPath);

        Assert.Contains("const base = self.location.pathname.replace(/service-worker\\.js$/, '');", serviceWorker);
        Assert.DoesNotContain("const base = \"/RaidLoop/\";", serviceWorker);
        Assert.DoesNotContain("const base = \"/\";", serviceWorker);
    }

    [Fact]
    public void ClientIndexSetsBasePathFromCurrentLocation()
    {
        var index = File.ReadAllText(ClientIndexPath);

        Assert.Contains("const basePath = window.location.pathname.startsWith('/RaidLoop/') ? '/RaidLoop/' : '/';", index);
        Assert.Contains("baseElement.href = basePath;", index);
        Assert.DoesNotContain("<base href=\"/RaidLoop/\" />", index);
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

    private static string ExtractSection(string text, string startMarker, string endMarker)
    {
        var startIndex = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        Assert.True(startIndex >= 0, $"Could not find start marker: {startMarker}");
        startIndex += startMarker.Length;

        var endIndex = text.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
        Assert.True(endIndex > startIndex, $"Could not find end marker: {endMarker}");

        return text[startIndex..endIndex];
    }
}
