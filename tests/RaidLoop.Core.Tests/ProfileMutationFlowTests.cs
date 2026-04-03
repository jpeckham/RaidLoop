using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using RaidLoop.Client;
using RaidLoop.Client.Configuration;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

public sealed class ProfileMutationFlowTests
{
    [Fact]
    public async Task SellStashItemAsync_DelegatesToActionApi_And_AppliesReturnedProjections()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("sell-stash-item", request.Action);
                Assert.Equal(0, request.Payload.GetProperty("stashIndex").GetInt32());
                return Response(
                    money: 999,
                    mainStash: [ItemCatalog.Create("Rusty Knife")],
                    onPersonItems: []);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_mainGame", new GameState([ItemCatalog.Create("AK74")]));
        SetField(home, "_money", 0);

        await InvokePrivateAsync(home, "SellStashItemAsync", 0);

        Assert.Single(actionClient.Requests);
        Assert.Equal(999, Assert.IsType<int>(GetField(home, "_money")));
        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        Assert.Equal(["Rusty Knife"], mainGame.Stash.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task OnInitializedAsync_ReportsUnauthorizedBootstrapFailuresAndSignsOut()
    {
        var telemetry = new RecordingTelemetryService();
        var authService = CreateAuthService(telemetry);
        var home = CreateHome(
            profileApiClient: new ThrowingProfileApiClient(() => new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized)),
            telemetry: telemetry,
            authService: authService);

        await InvokePrivateAsync(home, "OnInitializedAsync");

        Assert.Single(telemetry.Errors);
        Assert.Contains("bootstrap", telemetry.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(authService.IsAuthenticated);
        Assert.False(Assert.IsType<bool>(GetField(home, "_isLoading")));
    }

    [Fact]
    public void GetRandomCooldownText_IncludesHours_WhenCooldownExceedsOneHour()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SetField(home, "_randomCharacterAvailableAt", DateTimeOffset.UtcNow.Add(new TimeSpan(1, 52, 38)));

        var text = InvokePrivate<string>(home, "GetRandomCooldownText");

        Assert.StartsWith("1:52:", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnInitializedAsync_ReportsServiceUnavailableBootstrapFailuresWithoutCrashingHome()
    {
        var telemetry = new RecordingTelemetryService();
        var authService = CreateAuthService(telemetry);
        var home = CreateHome(
            profileApiClient: new ThrowingProfileApiClient(() => new HttpRequestException("Service Unavailable", null, HttpStatusCode.ServiceUnavailable)),
            telemetry: telemetry,
            authService: authService);

        await InvokePrivateAsync(home, "OnInitializedAsync");

        Assert.Single(telemetry.Errors);
        Assert.Contains("bootstrap", telemetry.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Assert.IsType<bool>(GetField(home, "_isLoading")));
        Assert.Equal(
            "The profile service is temporarily unavailable. Try again in a moment.",
            Assert.IsType<string>(GetField(home, "_loadErrorMessage")));
    }

    [Fact]
    public async Task SellStashItemAsync_ReportsActionFailuresThroughTelemetryAndRethrows()
    {
        var telemetry = new RecordingTelemetryService();
        var home = CreateHome(
            actionClient: new ThrowingGameActionApiClient(() => new InvalidOperationException("action failed")),
            telemetry: telemetry);

        SetField(home, "_mainGame", new GameState([ItemCatalog.Create("AK74")]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokePrivateAsync(home, "SellStashItemAsync", 0));

        Assert.Equal("action failed", ex.Message);
        Assert.Single(telemetry.Errors);
        Assert.Contains("sell-stash-item", telemetry.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MoveStashToOnPersonAsync_DelegatesToActionApi_And_AppliesReturnedProjections()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("move-stash-to-on-person", request.Action);
                Assert.Equal(0, request.Payload.GetProperty("stashIndex").GetInt32());
                return Response(
                    money: 500,
                    mainStash: [],
                    onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)]);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_mainGame", new GameState([ItemCatalog.Create("AK74")]));
        SetField(home, "_onPersonItems", new List<OnPersonEntry>());

        await InvokePrivateAsync(home, "MoveStashToOnPersonAsync", 0);

        Assert.Single(actionClient.Requests);
        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        Assert.Empty(mainGame.Stash);
        var onPerson = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        var moved = Assert.Single(onPerson);
        Assert.Equal("AK74", moved.Item.Name);
        Assert.True(moved.IsEquipped);
    }

    [Fact]
    public async Task BuyFromShopAsync_DelegatesToActionApi_And_AppliesReturnedProjections()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("buy-from-shop", request.Action);
                Assert.Equal(19, request.Payload.GetProperty("itemDefId").GetInt32());
                return Response(
                    money: 490,
                    mainStash: [],
                    onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("Medkit"), false)]);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_money", 500);
        SetField(home, "_onPersonItems", new List<OnPersonEntry>());

        await InvokePrivateAsync(home, "BuyFromShopAsync", CreateShopStock("medkit"));

        Assert.Single(actionClient.Requests);
        Assert.Equal(490, Assert.IsType<int>(GetField(home, "_money")));
        var onPerson = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        Assert.Equal("Medkit", Assert.Single(onPerson).Item.Name);
    }

    [Fact]
    public async Task MoveStashToOnPersonAsync_RejectsWhenLoadoutWouldExceedStrengthBudget()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = _ => Response(
                money: 500,
                mainStash: [ItemCatalog.Create("6B43 Zabralo-Sh body armor")],
                onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)])
        };
        var home = CreateHome(actionClient);

        SetField(home, "_statsAccepted", true);
        SetField(home, "_acceptedStats", new PlayerStats(8, 8, 8, 8, 8, 8));
        SetField(home, "_mainGame", new GameState([ItemCatalog.Create("6B43 Zabralo-Sh body armor")]));
        SetField(home, "_onPersonItems", new List<OnPersonEntry>
        {
            new(ItemCatalog.Create("6B43 Zabralo-Sh body armor"), true),
            new(ItemCatalog.Create("6B13 assault armor"), true),
            new(ItemCatalog.Create("FORT Defender-2"), true),
            new(ItemCatalog.Create("NFM THOR"), true),
            new(ItemCatalog.Create("Small Backpack"), true),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false)
        });

        await InvokePrivateAsync(home, "MoveStashToOnPersonAsync", 0);

        Assert.Empty(actionClient.Requests);
        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        Assert.Single(mainGame.Stash);
        Assert.Equal("6B43 Zabralo-Sh body armor", mainGame.Stash[0].Name);
    }

    [Fact]
    public async Task BuyFromShopAsync_RejectsWhenLoadoutWouldExceedStrengthBudget()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = _ => Response(
                money: 500,
                mainStash: [],
                onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)])
        };
        var home = CreateHome(actionClient);

        SetField(home, "_money", 500);
        SetField(home, "_statsAccepted", true);
        SetField(home, "_acceptedStats", new PlayerStats(8, 8, 8, 8, 8, 8));
        SetField(home, "_onPersonItems", new List<OnPersonEntry>
        {
            new(ItemCatalog.Create("6B43 Zabralo-Sh body armor"), true),
            new(ItemCatalog.Create("6B13 assault armor"), true),
            new(ItemCatalog.Create("FORT Defender-2"), true),
            new(ItemCatalog.Create("NFM THOR"), true),
            new(ItemCatalog.Create("Small Backpack"), true),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false)
        });

        await InvokePrivateAsync(home, "BuyFromShopAsync", CreateShopStock("6b2_body_armor"));

        Assert.Empty(actionClient.Requests);
        Assert.Equal(500, Assert.IsType<int>(GetField(home, "_money")));
    }

    [Fact]
    public void ShopStock_ContainsTieredWeaponAndArmorChoicesForCharismaUnlocks()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                ShopStock:
                [
                    CreateShopOffer("medkit"),
                    CreateShopOffer("makarov"),
                    CreateShopOffer("6b2_body_armor"),
                    CreateShopOffer("bnti_kirasa_n"),
                    CreateShopOffer("ppsh"),
                    CreateShopOffer("small_backpack"),
                    CreateShopOffer("large_backpack"),
                    CreateShopOffer("ak74"),
                    CreateShopOffer("6b13_assault_armor")
                ],
                PlayerConstitution: 12,
                PlayerMaxHealth: 34,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: null));

        var shopStock = Assert.IsType<List<ShopStock>>(GetField(home, "_shopStock"));
        var itemNames = shopStock.Select(stock => stock.Item.Name).ToArray();

        Assert.Contains("PPSH", itemNames);
        Assert.Contains("6B2 body armor", itemNames);
        Assert.Contains("BNTI Kirasa-N", itemNames);
        Assert.Contains("Large Backpack", itemNames);
        Assert.Contains("6B13 assault armor", itemNames);
    }

    [Fact]
    public void ApplySnapshot_ReplacesShopStockFromBootstrapSnapshot()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                ShopStock: [CreateShopOffer("makarov"), CreateShopOffer("ppsh"), CreateShopOffer("6b2_body_armor")],
                PlayerConstitution: 12,
                PlayerMaxHealth: 34,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: null));

        var shopStock = Assert.IsType<List<ShopStock>>(GetField(home, "_shopStock"));
        Assert.Equal(["Makarov", "PPSH", "6B2 body armor"], shopStock.Select(stock => stock.Item.Name).ToArray());
    }

    [Fact]
    public void VisibleShopStock_FiltersOutItemsAboveCurrentCharismaTier()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                ShopStock:
                [
                    CreateShopOffer("makarov"),
                    CreateShopOffer("6b2_body_armor"),
                    CreateShopOffer("bnti_kirasa_n"),
                    CreateShopOffer("ppsh"),
                    CreateShopOffer("small_backpack"),
                    CreateShopOffer("large_backpack"),
                    CreateShopOffer("ak74"),
                    CreateShopOffer("6b13_assault_armor")
                ],
                AcceptedStats: new PlayerStats(8, 8, 8, 8, 8, 12),
                DraftStats: new PlayerStats(8, 8, 8, 8, 8, 12),
                AvailableStatPoints: 0,
                StatsAccepted: true,
                PlayerConstitution: 8,
                PlayerMaxHealth: 26,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: null));

        var visibleShopStock = Assert.IsAssignableFrom<IReadOnlyList<ShopStock>>(GetPrivatePropertyValue(home, "VisibleShopStock"));
        Assert.Equal(["Makarov", "6B2 body armor", "BNTI Kirasa-N", "PPSH", "Small Backpack", "Large Backpack"], visibleShopStock.Select(stock => stock.Item.Name).ToArray());
    }

    [Fact]
    public void ApplySnapshot_LoadsAcceptedAndDraftStatsAndAvailablePoints()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                PlayerConstitution: 12,
                PlayerMaxHealth: 34,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: null,
                AcceptedStats: new PlayerStats(8, 14, 12, 10, 9, 16),
                DraftStats: new PlayerStats(8, 15, 12, 10, 9, 16),
                AvailableStatPoints: 5,
                StatsAccepted: true));

        Assert.Equal(new PlayerStats(8, 14, 12, 10, 9, 16), Assert.IsType<PlayerStats>(GetField(home, "_acceptedStats")));
        Assert.Equal(new PlayerStats(8, 15, 12, 10, 9, 16), Assert.IsType<PlayerStats>(GetField(home, "_draftStats")));
        Assert.Equal(5, Assert.IsType<int>(GetField(home, "_availableStatPoints")));
        Assert.True(Assert.IsType<bool>(GetField(home, "_statsAccepted")));
    }

    [Fact]
    public void ApplySnapshot_NormalizesMissingStatPayloadToEditableDefaults()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                PlayerConstitution: 8,
                PlayerMaxHealth: 26,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: null));

        Assert.Equal(PlayerStats.Default, Assert.IsType<PlayerStats>(GetField(home, "_acceptedStats")));
        Assert.Equal(PlayerStats.Default, Assert.IsType<PlayerStats>(GetField(home, "_draftStats")));
        Assert.Equal(27, Assert.IsType<int>(GetField(home, "_availableStatPoints")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_statsAccepted")));
    }

    [Fact]
    public void CanStartMainRaid_IsBlockedUntilStatsAreAccepted()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        SetField(home, "_onPersonItems", new List<OnPersonEntry> { new(ItemCatalog.Create("AK74"), true) });
        SetField(home, "_statsAccepted", false);

        Assert.False(GetPrivateProperty<bool>(home, "CanStartMainRaid"));

        SetField(home, "_statsAccepted", true);

        Assert.True(GetPrivateProperty<bool>(home, "CanStartMainRaid"));
    }

    [Fact]
    public void RaidBlockReason_CombinesExistingReasonsWithLoadoutWeightWarning()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        SetField(home, "_statsAccepted", true);
        SetField(home, "_acceptedStats", new PlayerStats(8, 8, 8, 8, 8, 8));
        SetField(home, "_onPersonItems", new List<OnPersonEntry>
        {
            new(ItemCatalog.Create("6B43 Zabralo-Sh body armor"), true),
            new(ItemCatalog.Create("6B13 assault armor"), true),
            new(ItemCatalog.Create("FORT Defender-2"), true),
            new(ItemCatalog.Create("NFM THOR"), true),
            new(ItemCatalog.Create("Small Backpack"), true),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false),
            new(ItemCatalog.Create("Medkit"), false)
        });

        var reason = GetPrivateProperty<string?>(home, "RaidBlockReason");

        Assert.NotNull(reason);
        Assert.Contains("weapon", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weight", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AcceptStatsAsync_DelegatesToProfileActionApi_And_AppliesReturnedStatProjection()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("accept-stats", request.Action);
                return Response(
                    money: 500,
                    mainStash: [],
                    onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                    acceptedStats: new PlayerStats(8, 14, 12, 10, 9, 16),
                    draftStats: new PlayerStats(8, 14, 12, 10, 9, 16),
                    availableStatPoints: 0,
                    statsAccepted: true);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_draftStats", new PlayerStats(8, 14, 12, 10, 9, 16));
        SetField(home, "_acceptedStats", PlayerStats.Default);
        SetField(home, "_availableStatPoints", 0);
        SetField(home, "_statsAccepted", false);

        await InvokePrivateAsync(home, "AcceptStatsAsync");

        Assert.True(Assert.IsType<bool>(GetField(home, "_statsAccepted")));
        Assert.Equal(new PlayerStats(8, 14, 12, 10, 9, 16), Assert.IsType<PlayerStats>(GetField(home, "_acceptedStats")));
    }

    [Fact]
    public void ApplyActionResult_PlayerProjection_UpdatesDerivedHealthFields()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SetField(home, "_acceptedStats", PlayerStats.Default);
        SetField(home, "_playerConstitution", 8);
        SetField(home, "_maxHealth", 26);

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            Response(
                money: 500,
                mainStash: [],
                onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                acceptedStats: new PlayerStats(14, 12, 12, 10, 9, 16),
                draftStats: new PlayerStats(14, 12, 12, 10, 9, 16),
                availableStatPoints: 0,
                statsAccepted: true,
                playerConstitution: 12,
                playerMaxHealth: 34));

        Assert.Equal(new PlayerStats(14, 12, 12, 10, 9, 16), Assert.IsType<PlayerStats>(GetField(home, "_acceptedStats")));
        Assert.Equal(12, Assert.IsType<int>(GetField(home, "_playerConstitution")));
        Assert.Equal(34, Assert.IsType<int>(GetField(home, "_maxHealth")));
    }

    [Fact]
    public void DraftStatEditing_IsDisabledAfterStatsAccepted()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        SetField(home, "_statsAccepted", true);
        SetField(home, "_raid", null);
        SetField(home, "_availableStatPoints", 10);
        SetField(home, "_draftStats", new PlayerStats(8, 14, 12, 10, 9, 16));

        Assert.False(InvokePrivateBool(home, "CanIncreaseDraftStat", "STR"));
        Assert.False(InvokePrivateBool(home, "CanDecreaseDraftStat", "DEX"));
    }

    [Fact]
    public void CanReallocateStats_IsEnabledAfterAcceptanceWhenNotInRaidAndPlayerCanAffordIt()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        SetField(home, "_raid", null);
        SetField(home, "_statsAccepted", true);
        SetField(home, "_money", 1);

        Assert.True(GetPrivateProperty<bool>(home, "CanReallocateStats"));
    }

    [Fact]
    public async Task ReallocateStatsAsync_IsBlockedInRaid_And_ChargesWhenAllowed()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("reallocate-stats", request.Action);
                return Response(
                    money: 5000,
                    mainStash: [],
                    onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("AK74"), true)],
                    acceptedStats: PlayerStats.Default,
                    draftStats: PlayerStats.Default,
                    availableStatPoints: 27,
                    statsAccepted: false);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_money", 10001);
        SetField(home, "_statsAccepted", true);
        SetField(home, "_acceptedStats", new PlayerStats(8, 14, 12, 10, 9, 16));
        SetField(home, "_draftStats", new PlayerStats(8, 14, 12, 10, 9, 16));
        SetField(home, "_raid", new RaidState(26, new RaidInventory()));

        await InvokePrivateAsync(home, "ReallocateStatsAsync");

        Assert.Empty(actionClient.Requests);

        SetField(home, "_raid", null);

        await InvokePrivateAsync(home, "ReallocateStatsAsync");

        Assert.Single(actionClient.Requests);
        Assert.False(Assert.IsType<bool>(GetField(home, "_statsAccepted")));
        Assert.Equal(27, Assert.IsType<int>(GetField(home, "_availableStatPoints")));
    }

    [Fact]
    public async Task SellLuckRunItemAsync_DelegatesToActionApi_And_AppliesReturnedProjections()
    {
        var cooldown = DateTimeOffset.Parse("2026-03-18T06:00:00Z");
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("sell-luck-run-item", request.Action);
                Assert.Equal(0, request.Payload.GetProperty("luckIndex").GetInt32());
                return Response(
                    money: 520,
                    mainStash: [],
                    onPersonItems: [],
                    randomCharacterAvailableAt: cooldown,
                    randomCharacter: null);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_money", 500);
        SetField(home, "_randomCharacter", new RandomCharacterState("Ghost-101", [ItemCatalog.Create("Bandage")], PlayerStats.Default));

        await InvokePrivateAsync(home, "SellLuckRunItemAsync", 0);

        Assert.Single(actionClient.Requests);
        Assert.Equal(520, Assert.IsType<int>(GetField(home, "_money")));
        Assert.Null(GetField(home, "_randomCharacter"));
        Assert.Equal(cooldown, Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
    }

    [Fact]
    public async Task ReloadAsync_DelegatesToRaidActionOutsideCombat_WhenWeaponUsesAmmo()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("reload", request.Action);
                return new GameActionResult("RaidUpdated", null, null, null);
            }
        };
        var home = CreateHome(actionClient);
        var inventory = RaidInventory.FromItems([ItemCatalog.Create("AK74")], [], backpackCapacity: 3);

        SetField(home, "_raid", new RaidState(24, inventory));
        SetField(home, "_encounterType", EncounterType.Neutral);

        await InvokePrivateAsync(home, "ReloadAsync");

        Assert.Single(actionClient.Requests);
        Assert.Equal("reload", actionClient.Requests[0].Action);
    }

    [Fact]
    public void CombatAvailability_IgnoresWeaponMalfunctionState()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var inventory = RaidInventory.FromItems([ItemCatalog.Create("AK74")], [], backpackCapacity: 3);

        SetField(home, "_raid", new RaidState(24, inventory));
        SetField(home, "_ammo", 30);

        Assert.True(GetPrivateProperty<bool>(home, "CanAttack"));
        Assert.True(GetPrivateProperty<bool>(home, "CanBurstFire"));
        Assert.True(GetPrivateProperty<bool>(home, "CanFullAuto"));
    }

    [Fact]
    public void CombatActionVisibility_StaysTrueWhileEnabledStateTracksAmmoThresholds()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var inventory = RaidInventory.FromItems([ItemCatalog.Create("AK74")], [], backpackCapacity: 3);

        SetField(home, "_raid", new RaidState(24, inventory));
        SetField(home, "_ammo", 0);

        Assert.True(GetPrivateProperty<bool>(home, "CanAttack"));
        Assert.True(GetPrivateProperty<bool>(home, "CanBurstFire"));
        Assert.True(GetPrivateProperty<bool>(home, "CanFullAuto"));
        Assert.False(GetPrivateProperty<bool>(home, "CanAttackEnabled"));
        Assert.False(GetPrivateProperty<bool>(home, "CanBurstFireEnabled"));
        Assert.False(GetPrivateProperty<bool>(home, "CanFullAutoEnabled"));

        SetField(home, "_ammo", 2);

        Assert.True(GetPrivateProperty<bool>(home, "CanAttackEnabled"));
        Assert.False(GetPrivateProperty<bool>(home, "CanBurstFireEnabled"));
        Assert.False(GetPrivateProperty<bool>(home, "CanFullAutoEnabled"));

        SetField(home, "_ammo", 3);

        Assert.True(GetPrivateProperty<bool>(home, "CanBurstFireEnabled"));
        Assert.False(GetPrivateProperty<bool>(home, "CanFullAutoEnabled"));

        SetField(home, "_ammo", 10);

        Assert.True(GetPrivateProperty<bool>(home, "CanFullAutoEnabled"));
    }

    [Fact]
    public void ApplyActionResult_AppliesEconomyStashLoadoutLuckRunAndRaidProjections()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var eventJson = System.Text.Json.JsonDocument.Parse("""
            {
              "enemyDamage": 2,
              "playerDamage": 3,
              "ammoSpent": 1,
              "weaponMalfunctioned": true
            }
            """);
        var projectionJson = System.Text.Json.JsonDocument.Parse("""
            {
              "economy": {
                "money": 640
              },
              "stash": {
                "mainStash": [
                  { "Name": "Makarov", "Type": 0, "Value": 60, "Slots": 1, "Rarity": 0, "DisplayRarity": 1 }
                ]
              },
              "loadout": {
                "onPersonItems": [
                  {
                    "Item": { "Name": "AK74", "Type": 0, "Value": 320, "Slots": 1, "Rarity": 2, "DisplayRarity": 3 },
                    "IsEquipped": true
                  }
                ]
              },
              "luckRun": {
                "randomCharacterAvailableAt": "2026-03-20T06:00:00Z",
                "randomCharacter": {
                  "Name": "Ghost-101",
                  "Inventory": [
                    { "Name": "Bandage", "Type": 4, "Value": 15, "Slots": 1, "Rarity": 0, "DisplayRarity": 0 }
                  ],
                  "Stats": {
                    "Strength": 12,
                    "Dexterity": 11,
                    "Constitution": 10,
                    "Intelligence": 9,
                    "Wisdom": 8,
                    "Charisma": 13
                  }
                }
              },
                "raid": {
                  "health": 21,
                  "backpackCapacity": 6,
                  "encumbrance": 40,
                  "maxEncumbrance": 100,
                  "ammo": 4,
                  "weaponMalfunction": true,
                  "medkits": 1,
                  "lootSlots": 0,
                  "challenge": 2,
                  "distanceFromExtract": 3,
                  "encounterType": "Combat",
                "encounterTitle": "Combat Encounter",
                "encounterDescription": "Enemy contact on your position.",
                "enemyName": "Scav",
                "enemyHealth": 6,
                "lootContainer": "",
                "awaitingDecision": false,
                "discoveredLoot": [],
                "carriedLoot": [],
                "equippedItems": [
                  { "Name": "AK74", "Type": 0, "Value": 320, "Slots": 1, "Rarity": 2, "DisplayRarity": 3 }
                ],
                "logEntries": [
                  "You hit Scav for 2."
                ]
              }
            }
            """);

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "CombatResolved",
                eventJson.RootElement.Clone(),
                projectionJson.RootElement.Clone(),
                "Action resolved."));

        Assert.Equal(640, Assert.IsType<int>(GetField(home, "_money")));
        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        Assert.Equal(["Makarov"], mainGame.Stash.Select(item => item.Name).ToArray());
        var onPersonItems = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        Assert.Single(onPersonItems);
        Assert.Equal("AK74", onPersonItems[0].Item.Name);
        Assert.True(onPersonItems[0].IsEquipped);
        Assert.Equal(DateTimeOffset.Parse("2026-03-20T06:00:00Z"), Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
        var randomCharacter = Assert.IsType<RandomCharacterState>(GetField(home, "_randomCharacter"));
        Assert.Equal("Ghost-101", randomCharacter.Name);
        Assert.Equal("Bandage", Assert.Single(randomCharacter.Inventory).Name);
        Assert.Equal(new PlayerStats(12, 11, 10, 9, 8, 13), randomCharacter.Stats);
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal(21, raid.Health);
        Assert.Equal(6, raid.BackpackCapacity);
        Assert.Equal("40/100 lbs", InvokePrivate<string>(home, "GetRaidEncumbranceText"));
        Assert.Equal(4, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(2, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        Assert.Equal("Scavenger", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(6, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal("Combat", Assert.IsType<EncounterType>(GetField(home, "_encounterType")).ToString());
        Assert.Equal("Action resolved.", Assert.IsType<string>(GetField(home, "_resultMessage")));
    }

    [Fact]
    public void ReadRandomCharacter_HydratesStatsFromProjection()
    {
        using var document = JsonDocument.Parse("""
        {
          "name": "Ghost-101",
          "inventory": [
            { "name": "Bandage", "type": 4, "value": 15, "slots": 1, "rarity": 0, "displayRarity": 0, "weight": 1 }
          ],
          "stats": {
            "strength": 12,
            "dexterity": 11,
            "constitution": 10,
            "intelligence": 9,
            "wisdom": 8,
            "charisma": 13
          }
        }
        """);

        var method = typeof(Home).GetMethod("TryReadRandomCharacter", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var args = new object?[] { document.RootElement, null };
        var result = Assert.IsType<bool>(method!.Invoke(null, args));
        Assert.True(result);

        var parsedRandomCharacter = Assert.IsType<RandomCharacterState>(args[1]);
        var statsProperty = parsedRandomCharacter.GetType().GetProperty("Stats", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(statsProperty);
        Assert.Equal(new PlayerStats(12, 11, 10, 9, 8, 13), Assert.IsType<PlayerStats>(statsProperty!.GetValue(parsedRandomCharacter)));
    }

    [Fact]
    public void ApplyActionResult_AppliesProjectionsWithoutSnapshotFallback()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "ProfileMutated",
                null,
                System.Text.Json.JsonDocument.Parse("""
                    {
                      "economy": {
                        "money": 910
                      },
                      "stash": {
                        "mainStash": [
                          { "Name": "Makarov", "Type": 0, "Value": 60, "Slots": 1, "Rarity": 0, "DisplayRarity": 1 }
                        ]
                      },
                      "loadout": {
                        "onPersonItems": [
                          {
                            "Item": { "Name": "Medkit", "Type": 3, "Value": 10, "Slots": 1, "Rarity": 0, "DisplayRarity": 1 },
                            "IsEquipped": false
                          }
                        ]
                      },
                      "luckRun": {
                        "randomCharacterAvailableAt": "2026-03-20T08:00:00Z",
                        "randomCharacter": {
                          "Name": "Ghost-303",
                          "Inventory": [
                            { "Name": "Bandage", "Type": 4, "Value": 15, "Slots": 1, "Rarity": 0, "DisplayRarity": 0 }
                          ],
                          "Stats": {
                            "Strength": 12,
                            "Dexterity": 11,
                            "Constitution": 10,
                            "Intelligence": 9,
                            "Wisdom": 8,
                            "Charisma": 13
                          }
                        }
                      }
                    }
                    """).RootElement.Clone(),
                null));

        Assert.Equal(910, Assert.IsType<int>(GetField(home, "_money")));
        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        Assert.Equal(["Makarov"], mainGame.Stash.Select(item => item.Name).ToArray());
        var onPersonItems = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        Assert.Single(onPersonItems);
        Assert.Equal("Medkit", onPersonItems[0].Item.Name);
        Assert.Equal(DateTimeOffset.Parse("2026-03-20T08:00:00Z"), Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
        var randomCharacter = Assert.IsType<RandomCharacterState>(GetField(home, "_randomCharacter"));
        Assert.Equal("Ghost-303", randomCharacter.Name);
        Assert.Equal(new PlayerStats(12, 11, 10, 9, 8, 13), randomCharacter.Stats);
    }

    [Fact]
    public void ApplyActionResult_DoesNotFallbackToSnapshotWhenProjectionsAreMissing()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SetField(home, "_money", 123);
        SetField(home, "_mainGame", new GameState([ItemCatalog.Create("AK74")]));
        SetField(home, "_onPersonItems", new List<OnPersonEntry> { new(ItemCatalog.Create("Makarov"), true) });

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "ProfileMutated",
                null,
                null,
                null));

        Assert.Equal(123, Assert.IsType<int>(GetField(home, "_money")));
        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        Assert.Equal(["AK74"], mainGame.Stash.Select(item => item.Name).ToArray());
        var onPersonItems = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        Assert.Single(onPersonItems);
        Assert.Equal("Makarov", onPersonItems[0].Item.Name);
        Assert.Equal(DateTimeOffset.MinValue, Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
        Assert.Null(GetField(home, "_randomCharacter"));
    }

    [Fact]
    public void ApplyActionResult_PatchesTrimmedRaidProjection_WithoutClearingUntouchedRaidState()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var existingInventory = RaidInventory.FromItems(
            [ItemCatalog.Create("AK74"), ItemCatalog.Create("6B13 assault armor"), ItemCatalog.Create("Tactical Backpack")],
            [ItemCatalog.Create("Ammo Box")],
            backpackCapacity: 9);
        existingInventory.MedkitCount = 2;
        existingInventory.DiscoveredLoot.Add(ItemCatalog.Create("Bandage"));
        var existingRaid = new RaidState(26, existingInventory);

        SetField(home, "_raid", existingRaid);
        SetField(home, "_inRaid", true);
        SetField(home, "_awaitingDecision", true);
        SetField(home, "_challenge", 1);
        SetField(home, "_distanceFromExtract", 2);
        SetField(home, "_ammo", 5);
        SetField(home, "_encounterType", EncounterType.Loot);
        SetField(home, "_encounterDescription", "A searchable container appears.");
        SetField(home, "_enemyName", "Patrol Guard");
        SetField(home, "_enemyHealth", 11);
        SetField(home, "_lootContainer", "Weapons Crate");
        SetField(home, "_log", new List<string> { "Old log entry." });

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "CombatResolved",
                null,
                System.Text.Json.JsonDocument.Parse("""
                    {
                      "raid": {
                        "health": 18,
                        "enemyHealth": 4,
                        "enemyConstitution": 12,
                        "enemyStrength": 7,
                        "ammo": 3,
                        "weaponMalfunction": true,
                        "logEntries": [
                          "You hit Patrol Guard for 2.",
                          "Patrol Guard hits you for 3."
                        ]
                      }
                    }
                    """).RootElement.Clone(),
                null));

        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal(18, raid.Health);
        Assert.Equal(9, raid.BackpackCapacity);
        Assert.Equal("AK74", raid.Inventory.EquippedWeapon!.Name);
        Assert.Equal("6B13 assault armor", raid.Inventory.EquippedArmor!.Name);
        Assert.Equal("Tactical Backpack", raid.Inventory.EquippedBackpack!.Name);
        Assert.Equal("Ammo Box", Assert.Single(raid.Inventory.CarriedItems).Name);
        Assert.Equal(2, raid.Inventory.MedkitCount);
        Assert.Equal("Bandage", Assert.Single(raid.Inventory.DiscoveredLoot).Name);
        Assert.False(raid.IsDead);
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.True(Assert.IsType<bool>(GetField(home, "_awaitingDecision")));
        Assert.Equal(1, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(2, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        Assert.Equal(EncounterType.Loot, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal("Weapons Crate", Assert.IsType<string>(GetField(home, "_lootContainer")));
        Assert.Equal("Patrol Guard", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(4, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal(12, Assert.IsType<int>(GetField(home, "_enemyConstitution")));
        Assert.Equal(7, Assert.IsType<int>(GetField(home, "_enemyStrength")));
        Assert.Equal("A searchable container appears.", Assert.IsType<string>(GetField(home, "_encounterDescription")));
        Assert.Equal(["You hit Patrol Guard for 2.", "Patrol Guard hits you for 3."], Assert.IsType<List<string>>(GetField(home, "_log")));
    }

    [Fact]
    public void ApplyActionResult_FreshRaidPartialProjection_ResetsOmittedRaidFields()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SetField(home, "_raid", null);
        SetField(home, "_inRaid", false);
        SetField(home, "_awaitingDecision", true);
        SetField(home, "_challenge", 2);
        SetField(home, "_distanceFromExtract", 1);
        SetField(home, "_ammo", 7);
        SetField(home, "_encounterType", EncounterType.Combat);
        SetField(home, "_encounterDescription", "stale encounter");
        SetField(home, "_enemyName", "Old Guard");
        SetField(home, "_enemyHealth", 13);
        SetField(home, "_lootContainer", "Old Crate");
        SetField(home, "_log", new List<string> { "stale log" });

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "RaidStarted",
                null,
                System.Text.Json.JsonDocument.Parse("""
                    {
                      "raid": {
                        "health": 18,
                        "enemyHealth": 4,
                        "logEntries": [
                          "Fresh raid log."
                        ]
                      }
                    }
                    """).RootElement.Clone(),
                null));

        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal(18, raid.Health);
        Assert.Equal(0, raid.BackpackCapacity);
        Assert.Empty(raid.Inventory.CarriedItems);
        Assert.Empty(raid.Inventory.DiscoveredLoot);
        Assert.Null(raid.Inventory.EquippedWeapon);
        Assert.Null(raid.Inventory.EquippedArmor);
        Assert.Null(raid.Inventory.EquippedBackpack);
        Assert.Equal(0, raid.Inventory.MedkitCount);
        Assert.True(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_awaitingDecision")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(EncounterType.Neutral, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_encounterDescription")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(4, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_lootContainer")));
        Assert.Equal(["Fresh raid log."], Assert.IsType<List<string>>(GetField(home, "_log")));
    }

    [Fact]
    public void ApplyActionResult_AppendsRaidLogEntriesAdded_WithoutClearingExistingHistory()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var existingInventory = RaidInventory.FromItems(
            [ItemCatalog.Create("AK74")],
            [],
            backpackCapacity: 3);

        SetField(home, "_raid", new RaidState(26, existingInventory));
        SetField(home, "_inRaid", true);
        SetField(home, "_ammo", 8);
        SetField(home, "_enemyHealth", 12);
        SetField(home, "_log", new List<string> { "Raid started as Main Character." });

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "CombatResolved",
                null,
                System.Text.Json.JsonDocument.Parse("""
                    {
                      "raid": {
                        "ammo": 7,
                        "enemyHealth": 8,
                        "logEntriesAdded": [
                          "You hit Scav for 4.",
                          "Scav hits you for 3."
                        ]
                      }
                    }
                    """).RootElement.Clone(),
                null));

        Assert.Equal(7, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(8, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal(
            ["Raid started as Main Character.", "You hit Scavenger for 4.", "Scavenger hits you for 3."],
            Assert.IsType<List<string>>(GetField(home, "_log")));
    }

    [Fact]
    public void ApplyActionResult_ClearsRaidState_WhenRaidProjectionIsNull()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var existingInventory = RaidInventory.FromItems(
            [ItemCatalog.Create("AK74")],
            [ItemCatalog.Create("Bandage")],
            backpackCapacity: 3);

        SetField(home, "_raid", new RaidState(24, existingInventory));
        SetField(home, "_inRaid", true);
        SetField(home, "_awaitingDecision", true);
        SetField(home, "_challenge", 2);
        SetField(home, "_distanceFromExtract", 1);
        SetField(home, "_ammo", 5);
        SetField(home, "_encounterType", EncounterType.Extraction);
        SetField(home, "_encounterDescription", "Extraction route open.");
        SetField(home, "_enemyName", "Final Guard");
        SetField(home, "_enemyHealth", 10);
        SetField(home, "_lootContainer", "Dead Body");
        SetField(home, "_log", new List<string> { "Raid started as Main Character.", "Extraction completed." });

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "RaidFinished",
                null,
                System.Text.Json.JsonDocument.Parse("""
                    {
                      "raid": null,
                      "loadout": {
                        "onPersonItems": [
                          {
                            "Item": { "Name": "AK74", "Type": 0, "Value": 320, "Slots": 1, "Rarity": 2, "DisplayRarity": 3 },
                            "IsEquipped": true
                          },
                          {
                            "Item": { "Name": "Bandage", "Type": 4, "Value": 15, "Slots": 1, "Rarity": 0, "DisplayRarity": 0 },
                            "IsEquipped": false
                          }
                        ]
                      }
                    }
                    """).RootElement.Clone(),
                "Killed in raid. Loadout lost."));

        Assert.Null(GetField(home, "_raid"));
        Assert.False(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_awaitingDecision")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(EncounterType.Neutral, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_encounterDescription")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_lootContainer")));
        Assert.Empty(Assert.IsType<List<string>>(GetField(home, "_log")));
        Assert.Equal("Killed in raid. Loadout lost.", Assert.IsType<string>(GetField(home, "_resultMessage")));

        var onPersonItems = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        Assert.Equal(["AK74", "Bandage"], onPersonItems.Select(entry => entry.Item.Name).ToArray());
    }

    [Fact]
    public void ApplyActionResult_SkipsMalformedInventoryEntries_WithoutCorruptingState()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SetField(home, "_mainGame", new GameState([ItemCatalog.Create("AK74")]));
        SetField(home, "_onPersonItems", new List<OnPersonEntry> { new(ItemCatalog.Create("Makarov"), true) });

        var existingInventory = RaidInventory.FromItems(
            [ItemCatalog.Create("AK74"), ItemCatalog.Create("6B13 assault armor"), ItemCatalog.Create("Tactical Backpack")],
            [ItemCatalog.Create("Ammo Box")],
            backpackCapacity: 9);
        existingInventory.MedkitCount = 2;
        existingInventory.DiscoveredLoot.Add(ItemCatalog.Create("Bandage"));
        SetField(home, "_raid", new RaidState(26, existingInventory));
        SetField(home, "_inRaid", true);

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "ProfileMutated",
                null,
                System.Text.Json.JsonDocument.Parse("""
                    {
                      "stash": {
                        "mainStash": [
                          { "bogus": true }
                        ]
                      },
                      "loadout": {
                        "onPersonItems": [
                          { "isEquipped": true }
                        ]
                      },
                      "raid": {
                        "health": 22,
                        "equippedItems": [
                          { "bogus": 1 }
                        ],
                        "carriedLoot": [
                          { "bogus": 2 }
                        ],
                        "discoveredLoot": [
                          { "bogus": 3 }
                        ]
                      }
                    }
                    """).RootElement.Clone(),
                null));

        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        Assert.Equal(["AK74"], mainGame.Stash.Select(item => item.Name).ToArray());

        var onPersonItems = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        Assert.Single(onPersonItems);
        Assert.Equal("Makarov", onPersonItems[0].Item.Name);
        Assert.True(onPersonItems[0].IsEquipped);

        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal(22, raid.Health);
        Assert.Equal("AK74", raid.Inventory.EquippedWeapon!.Name);
        Assert.Equal("6B13 assault armor", raid.Inventory.EquippedArmor!.Name);
        Assert.Equal("Tactical Backpack", raid.Inventory.EquippedBackpack!.Name);
        Assert.Equal("Ammo Box", Assert.Single(raid.Inventory.CarriedItems).Name);
        Assert.Equal("Bandage", Assert.Single(raid.Inventory.DiscoveredLoot).Name);
        Assert.Equal(2, raid.Inventory.MedkitCount);
        Assert.Equal(9, raid.BackpackCapacity);
        Assert.Equal(EncounterType.Neutral, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.True(Assert.IsType<bool>(GetField(home, "_inRaid")));
    }

    [Fact]
    public void ApplyActionResult_SkipsRandomCharacterProjection_WhenStatsAreMissing()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var expectedRandomCharacter = new RandomCharacterState(
            "Ghost-101",
            [ItemCatalog.Create("Bandage")],
            new PlayerStats(12, 11, 10, 9, 8, 13));

        SetField(home, "_randomCharacter", expectedRandomCharacter);

        InvokePrivateVoid(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "ProfileMutated",
                null,
                System.Text.Json.JsonDocument.Parse("""
                    {
                      "luckRun": {
                        "randomCharacter": {
                          "name": "Ghost-101",
                          "inventory": [
                            { "name": "Bandage", "type": 4, "value": 15, "slots": 1, "rarity": 0, "displayRarity": 0 }
                          ]
                        }
                      }
                    }
                    """).RootElement.Clone(),
                null));

        Assert.Same(expectedRandomCharacter, GetField(home, "_randomCharacter"));
    }

    [Fact]
    public void ApplySnapshot_ClearsEmptyRandomCharacter_And_LeavesReadyStateWhenCooldownMissing()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SetField(home, "_contactState", "PlayerAmbush");
        SetField(home, "_surpriseSide", "Player");
        SetField(home, "_initiativeWinner", "Player");
        SetField(home, "_openingActionsRemaining", 2);
        SetField(home, "_surprisePersistenceEligible", true);

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [],
                PlayerConstitution: 10,
                PlayerMaxHealth: 30,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: new RandomCharacterSnapshot("Ghost-101", [], PlayerStats.Default),
                ActiveRaid: null));

        Assert.Null(GetField(home, "_randomCharacter"));
        Assert.Equal(DateTimeOffset.MinValue, Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
    }

    [Fact]
    public void ApplySnapshot_ClearsEmptyRandomCharacter_And_PreservesExistingCooldown()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var expectedCooldown = DateTimeOffset.UtcNow.AddMinutes(3);

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [],
                PlayerConstitution: 10,
                PlayerMaxHealth: 30,
                RandomCharacterAvailableAt: expectedCooldown,
                RandomCharacter: new RandomCharacterSnapshot("Ghost-101", [], PlayerStats.Default),
                ActiveRaid: null));

        Assert.Null(GetField(home, "_randomCharacter"));
        Assert.Equal(expectedCooldown, Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
    }

    [Fact]
    public void ApplySnapshot_AppliesOpeningPhaseStateFromActiveRaidSnapshot()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [],
                PlayerConstitution: 10,
                PlayerMaxHealth: 30,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: new RaidSnapshot(
                    Health: 30,
                    BackpackCapacity: 3,
                    Ammo: 8,
                    WeaponMalfunction: false,
                    Medkits: 1,
                    LootSlots: 0,
                    Challenge: 0,
                    DistanceFromExtract: 0,
                    EncounterType: "Combat",
                    EncounterTitle: "Ambush",
                    EncounterDescription: "They spotted you first.",
                    EnemyName: "Scav",
                    EnemyHealth: 12,
                    EnemyDexterity: 9,
                    EnemyConstitution: 11,
                    EnemyStrength: 8,
                    LootContainer: string.Empty,
                    AwaitingDecision: false,
                    ContactState: "PlayerAmbush",
                    SurpriseSide: "Player",
                    InitiativeWinner: "None",
                    OpeningActionsRemaining: 1,
                    SurprisePersistenceEligible: true,
                    DiscoveredLoot: [],
                    CarriedLoot: [],
                    EquippedItems: [],
                    LogEntries: [])));

        Assert.Equal("PlayerAmbush", Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal("Player", Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal("None", Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(1, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.True(Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
    }

    [Fact]
    public void ApplySnapshot_NormalizesMissingOpeningPhaseState_WhenActiveRaidSnapshotHasNullValues()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [],
                PlayerConstitution: 10,
                PlayerMaxHealth: 30,
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: new RaidSnapshot(
                    Health: 30,
                    BackpackCapacity: 3,
                    Ammo: 8,
                    WeaponMalfunction: false,
                    Medkits: 1,
                    LootSlots: 0,
                    Challenge: 0,
                    DistanceFromExtract: 0,
                    EncounterType: "Combat",
                    EncounterTitle: "Ambush",
                    EncounterDescription: "They spotted you first.",
                    EnemyName: "Scav",
                    EnemyHealth: 12,
                    EnemyDexterity: 9,
                    EnemyConstitution: 11,
                    EnemyStrength: 8,
                    LootContainer: string.Empty,
                    AwaitingDecision: false,
                    ContactState: null!,
                    SurpriseSide: null!,
                    InitiativeWinner: null!,
                    OpeningActionsRemaining: 0,
                    SurprisePersistenceEligible: false,
                    DiscoveredLoot: [],
                    CarriedLoot: [],
                    EquippedItems: [],
                    LogEntries: [])));

        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
    }

    [Fact]
    public void TryReadItem_KnownAuthoredItemWithoutWeight_UsesCatalogWeight()
    {
        using var document = JsonDocument.Parse("""
        {
            "name": "Makarov",
            "type": 123,
            "value": 456,
            "slots": 789
        }
        """);

        var method = typeof(Home).GetMethod("TryReadItem", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var args = new object?[] { document.RootElement, null };
        var parsed = Assert.IsType<bool>(method!.Invoke(null, args));

        Assert.True(parsed);
        Assert.Equal(ItemCatalog.Get("Makarov"), Assert.IsType<Item>(args[1]));
    }

    [Fact]
    public void TryReadItem_KnownItemDefinitionPrefersClientOwnedIdentityOverLegacyName()
    {
        var itemDefId = ItemCatalog.Get("AK74").ItemDefId;

        using var document = JsonDocument.Parse("""
        {
            "name": "Server-authored alias",
            "itemDefId": __ITEM_DEF_ID__,
            "type": 0,
            "value": 777,
            "slots": 9,
            "weight": 13
        }
        """.Replace("__ITEM_DEF_ID__", itemDefId.ToString()));

        var method = typeof(Home).GetMethod("TryReadItem", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var args = new object?[] { document.RootElement, null };
        var parsed = Assert.IsType<bool>(method!.Invoke(null, args));

        Assert.True(parsed);
        Assert.Equal(ItemCatalog.Get("AK74"), Assert.IsType<Item>(args[1]));
    }

    [Fact]
    public void TryReadItem_UsesLegacyNameWhenItemDefinitionIdIsMissing()
    {
        using var document = JsonDocument.Parse("""
        {
            "name": "Legacy label",
            "type": 0,
            "value": 777,
            "slots": 9,
            "weight": 13
        }
        """);

        var method = typeof(Home).GetMethod("TryReadItem", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var args = new object?[] { document.RootElement, null };
        var parsed = Assert.IsType<bool>(method!.Invoke(null, args));

        Assert.True(parsed);
        var item = Assert.IsType<Item>(args[1]);
        Assert.Equal("Legacy label", item.Name);
        Assert.Equal(0, item.ItemDefId);
        Assert.Equal(ItemType.Weapon, item.Type);
        Assert.Equal(777, item.Value);
        Assert.Equal(9, item.Slots);
        Assert.Equal(13, item.Weight);
        Assert.NotEqual(ItemCatalog.Get("Makarov"), item);
        Assert.NotEqual(ItemCatalog.Get("AK74"), item);
    }

    [Fact]
    public void TryReadItem_PreservesUnknownItemDefinitionIdInsteadOfHydratingLegacyName()
    {
        var itemDefId = ItemCatalog.Get("AK74").ItemDefId + 9999;

        using var document = JsonDocument.Parse("""
        {
            "name": "AK74",
            "itemDefId": __ITEM_DEF_ID__,
            "type": 0,
            "value": 777,
            "slots": 9,
            "weight": 13
        }
        """.Replace("__ITEM_DEF_ID__", itemDefId.ToString()));

        var method = typeof(Home).GetMethod("TryReadItem", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var args = new object?[] { document.RootElement, null };
        var parsed = Assert.IsType<bool>(method!.Invoke(null, args));

        Assert.True(parsed);
        var item = Assert.IsType<Item>(args[1]);
        Assert.Equal("AK74", item.Name);
        Assert.Equal(itemDefId, item.ItemDefId);
        Assert.Equal(ItemType.Weapon, item.Type);
        Assert.Equal(777, item.Value);
        Assert.Equal(9, item.Slots);
        Assert.Equal(13, item.Weight);
        Assert.NotEqual(ItemCatalog.Get("AK74"), item);
    }

    private static Home CreateHome(
        IProfileApiClient? profileApiClient = null,
        IGameActionApiClient? actionClient = null,
        IClientTelemetryService? telemetry = null,
        SupabaseAuthService? authService = null)
    {
        var home = new Home();
        SetProperty(home, "Profiles", profileApiClient ?? new FakeProfileApiClient());
        SetProperty(home, "Actions", actionClient ?? new FakeGameActionApiClient());

        if (telemetry is not null)
        {
            SetProperty(home, "Telemetry", telemetry);
        }

        if (authService is not null)
        {
            SetProperty(home, "AuthService", authService);
        }

        return home;
    }

    private static Home CreateHome(FakeGameActionApiClient actionClient)
    {
        var home = new Home();
        SetProperty(home, "Profiles", new FakeProfileApiClient());
        SetProperty(home, "Actions", actionClient);
        return home;
    }

    private static GameActionResult Response(
        int money,
        IReadOnlyList<Item> mainStash,
        IReadOnlyList<OnPersonSnapshot> onPersonItems,
        PlayerStats? acceptedStats = null,
        PlayerStats? draftStats = null,
        int? availableStatPoints = null,
        bool? statsAccepted = null,
        DateTimeOffset? randomCharacterAvailableAt = null,
        RandomCharacterSnapshot? randomCharacter = null,
        int? playerConstitution = null,
        int? playerMaxHealth = null)
    {
        var projections = new Dictionary<string, object?>
        {
            ["economy"] = new Dictionary<string, object?>
            {
                ["money"] = money
            },
            ["stash"] = new Dictionary<string, object?>
            {
                ["mainStash"] = mainStash
            },
            ["loadout"] = new Dictionary<string, object?>
            {
                ["onPersonItems"] = onPersonItems
            },
            ["player"] = new Dictionary<string, object?>
            {
                ["acceptedStats"] = acceptedStats,
                ["draftStats"] = draftStats,
                ["playerConstitution"] = playerConstitution,
                ["playerMaxHealth"] = playerMaxHealth,
                ["availableStatPoints"] = availableStatPoints,
                ["statsAccepted"] = statsAccepted
            }
        };

        if (randomCharacterAvailableAt.HasValue || randomCharacter is not null)
        {
            projections["luckRun"] = new Dictionary<string, object?>
            {
                ["randomCharacterAvailableAt"] = randomCharacterAvailableAt ?? DateTimeOffset.MinValue,
                ["randomCharacter"] = randomCharacter
            };
        }

        return new GameActionResult(
            "ProfileMutated",
            null,
            System.Text.Json.JsonSerializer.SerializeToElement(projections),
            Message: null);
    }

    private static ShopOfferSnapshot CreateShopOffer(string itemKey, int stock = 1)
    {
        var item = ItemCatalog.GetByKey(itemKey);
        return new ShopOfferSnapshot(item.ItemDefId, CombatBalance.GetBuyPrice(item), stock);
    }

    private static ShopStock CreateShopStock(string itemKey, int stock = 1)
    {
        var item = ItemCatalog.GetByKey(itemKey);
        return new ShopStock(CreateShopOffer(itemKey, stock), item);
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static object? GetField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field!.GetValue(instance);
    }

    private static T GetPrivateProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<T>(property!.GetValue(instance));
    }

    private static object? GetPrivatePropertyValue(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return property!.GetValue(instance);
    }

    private static bool InvokePrivateBool(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<bool>(method!.Invoke(instance, args));
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<T>(method!.Invoke(instance, args));
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var task = method!.Invoke(instance, args) as Task;
        Assert.NotNull(task);
        await task!;
    }

    private static void InvokePrivateVoid(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(instance, args);
    }

    private static SupabaseAuthService CreateAuthService(IClientTelemetryService telemetry)
    {
        return new SupabaseAuthService(
            new FakeJsRuntime(),
            new TestNavigationManager(),
            telemetry,
            Options.Create(new SupabaseOptions
            {
                Url = "https://dblgbpzlrglcdwqyagnx.supabase.co",
                PublishableKey = "publishable-key"
            }));
    }

    private sealed class FakeProfileApiClient : IProfileApiClient
    {
        public Task<AuthBootstrapResponse> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthBootstrapResponse(
                true,
                "player@example.com",
                new PlayerSnapshot(500, [], [], 10, 30, DateTimeOffset.MinValue, null, null)));
        }
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://example.com/", "https://example.com/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }

    private sealed class RecordingTelemetryService : IClientTelemetryService
    {
        public List<(string Message, object? Details)> Errors { get; } = [];

        public ValueTask ReportErrorAsync(string message, object? details = null, CancellationToken cancellationToken = default)
        {
            Errors.Add((message, details));
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingProfileApiClient(Func<Exception> exceptionFactory) : IProfileApiClient
    {
        public Task<AuthBootstrapResponse> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<AuthBootstrapResponse>(exceptionFactory());
        }
    }

    private sealed class ThrowingGameActionApiClient(Func<Exception> exceptionFactory) : IGameActionApiClient
    {
        public List<GameActionRequest> Requests { get; } = [];

        public Task<GameActionResult> SendAsync(string action, object payload, CancellationToken cancellationToken = default)
        {
            Requests.Add(new GameActionRequest(action, System.Text.Json.JsonSerializer.SerializeToElement(payload)));
            return Task.FromException<GameActionResult>(exceptionFactory());
        }
    }

    private sealed class FakeGameActionApiClient : IGameActionApiClient
    {
        public List<GameActionRequest> Requests { get; } = [];

        public Func<GameActionRequest, GameActionResult> ResponseFactory { get; set; } =
            _ => throw new InvalidOperationException("No response configured.");

        public Task<GameActionResult> SendAsync(string action, object payload, CancellationToken cancellationToken = default)
        {
            var jsonPayload = System.Text.Json.JsonSerializer.SerializeToElement(payload);
            var request = new GameActionRequest(action, jsonPayload);
            Requests.Add(request);
            return Task.FromResult(ResponseFactory(request));
        }
    }
}
