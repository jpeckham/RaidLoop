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
    public void RaidHudPlacesAttemptExtractionAfterContinueSearching()
    {
        var markup = File.ReadAllText(RaidHudPath);
        var extractionBlockStart = markup.IndexOf("else if (EncounterType == EncounterType.Extraction)", StringComparison.Ordinal);
        var extractionBlockEnd = markup.IndexOf("else if (EncounterType == EncounterType.Neutral)", extractionBlockStart, StringComparison.Ordinal);
        var extractionBlock = markup.Substring(extractionBlockStart, extractionBlockEnd - extractionBlockStart);
        var continueIndex = extractionBlock.IndexOf("OnContinueSearching.InvokeAsync()", StringComparison.Ordinal);
        var attemptIndex = extractionBlock.IndexOf("OnAttemptExtract.InvokeAsync()", StringComparison.Ordinal);

        Assert.True(extractionBlockStart >= 0);
        Assert.True(continueIndex >= 0);
        Assert.True(attemptIndex > continueIndex);
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
}
