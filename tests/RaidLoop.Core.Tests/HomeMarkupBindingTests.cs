using System.IO;

namespace RaidLoop.Core.Tests;

public sealed class HomeMarkupBindingTests
{
    private static readonly string AppMarkupPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "App.razor"));
    private static readonly string ProgramPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Program.cs"));
    private static readonly string ClientAppSettingsPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "appsettings.json"));
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
    private static readonly string SupabaseAuthServicePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "SupabaseAuthService.cs"));
    private static readonly string ProfileApiClientPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "ProfileApiClient.cs"));
    private static readonly string ProfileApiInterfacePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Services", "IProfileApiClient.cs"));
    private static readonly string ProfileSaveHandlerPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "functions", "profile-save", "handler.mjs"));
    private static readonly string HomeMarkupPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Home.razor"));
    private static readonly string ClientIndexPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "index.html"));
    private static readonly string HomeCodeBehindPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Home.razor.cs"));
    private static readonly string StorageScriptPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "js", "storage.js"));
    private static readonly string TelemetryScriptPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "wwwroot", "js", "telemetry.js"));
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
    private static readonly string ContinuousDeliveryWorkflowPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".github", "workflows", "continuous-delivery-dotnet-blazor-github-pages.yml"));
    private static readonly string SupabaseDeployWorkflowPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".github", "workflows", "supabase-deploy.yml"));

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
    public void GameActionMigrationRoutesCombatAndLootActionsServerSide()
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
        Assert.Contains("continue-searching", migration);
        Assert.Contains("move-toward-extract", migration);
        Assert.Contains("attempt-extract", migration);
        Assert.Contains("select raid_sessions.profile, raid_sessions.payload", migration);
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
        Assert.Contains("await AuthService.SignOutAsync()", codeBehind);
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
