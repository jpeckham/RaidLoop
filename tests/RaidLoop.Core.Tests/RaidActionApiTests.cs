using System.IO;
using System.Reflection;
using System.Text.Json;
using RaidLoop.Client;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

public sealed class RaidActionApiTests
{
    [Fact]
    public async Task AttackAsync_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("attack", payload =>
        {
            Assert.Equal("enemy", payload.GetProperty("target").GetString());
            return CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 7,
                    "weaponMalfunction": false,
                    "encounterType": "Combat",
                    "encounterDescription": "Server combat",
                    "contactState": "PlayerAmbush",
                    "surpriseSide": "Player",
                    "initiativeWinner": "None",
                    "openingActionsRemaining": 1,
                    "surprisePersistenceEligible": true,
                    "enemyName": "Scav",
                    "enemyHealth": 11,
                    "lootContainer": "Dead Body",
                    "awaitingDecision": false,
                    "discoveredLoot": [],
                    "carriedLoot": [],
                    "equippedItems": [
                      { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                      { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                    ],
                    "logEntries": ["Raid updated on server."]
                  }
                }
                """);
        });
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "AttackAsync");

        Assert.Single(actionClient.Requests);
        Assert.Equal(7, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(11, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal("Scav", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(34, Assert.IsType<int>(GetField(home, "_maxHealth")));
        AssertOpeningPhaseFields(home, "PlayerAmbush", "Player", "None", 1, true);
    }

    [Fact]
    public async Task TakeLootAsync_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("take-loot", payload =>
        {
            Assert.Equal("Bandage", payload.GetProperty("itemName").GetString());
            return CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 8,
                    "weaponMalfunction": false,
                    "encounterType": "Loot",
                    "encounterDescription": "Server loot",
                    "contactState": "None",
                    "surpriseSide": "None",
                    "initiativeWinner": "None",
                    "openingActionsRemaining": 0,
                    "surprisePersistenceEligible": false,
                    "enemyName": "",
                    "enemyHealth": 0,
                    "lootContainer": "Dead Body",
                    "awaitingDecision": false,
                    "discoveredLoot": [],
                    "carriedLoot": [
                      { "name": "Bandage", "type": 4, "value": 15, "slots": 1, "rarity": 0, "displayRarity": 0 }
                    ],
                    "equippedItems": [
                      { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                      { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                    ],
                    "logEntries": ["Raid updated on server."]
                  }
                }
                """);
        });
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "TakeLootAsync", ItemCatalog.Create("Bandage"));

        Assert.Single(actionClient.Requests);
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal("Bandage", Assert.Single(raid.Inventory.CarriedItems).Name);
        Assert.Empty(raid.Inventory.DiscoveredLoot);
        AssertOpeningPhaseFields(home, "None", "None", "None", 0, false);
    }

    [Fact]
    public async Task GoDeeper_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("go-deeper", _ =>
            CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 8,
                    "weaponMalfunction": false,
                    "encounterType": "Loot",
                    "encounterDescription": "Server loot",
                    "contactState": "None",
                    "surpriseSide": "None",
                    "initiativeWinner": "None",
                    "openingActionsRemaining": 0,
                    "surprisePersistenceEligible": false,
                    "enemyName": "",
                    "enemyHealth": 0,
                    "lootContainer": "Dead Body",
                    "awaitingDecision": false,
                    "challenge": 3,
                    "distanceFromExtract": 4,
                    "discoveredLoot": [
                      { "name": "Scrap Metal", "type": 5, "value": 18, "slots": 1, "rarity": 0, "displayRarity": 0 }
                    ],
                    "carriedLoot": [],
                    "equippedItems": [
                      { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                      { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                    ],
                    "logEntries": ["Raid updated on server."]
                  }
                }
                """));
        var home = CreateHome(actionClient);
        SeedRaid(home);

        InvokePrivate(home, "GoDeeper");

        Assert.Single(actionClient.Requests);
        Assert.Equal(EncounterType.Loot, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(4, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal("Scrap Metal", Assert.Single(raid.Inventory.DiscoveredLoot).Name);
        AssertOpeningPhaseFields(home, "None", "None", "None", 0, false);
    }

    [Fact]
    public async Task MoveTowardExtract_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("move-toward-extract", _ =>
            CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 8,
                    "weaponMalfunction": false,
                    "encounterType": "Neutral",
                    "encounterDescription": "Server travel",
                    "contactState": "None",
                    "surpriseSide": "None",
                    "initiativeWinner": "None",
                    "openingActionsRemaining": 0,
                    "surprisePersistenceEligible": false,
                    "enemyName": "",
                    "enemyHealth": 0,
                    "lootContainer": "",
                    "awaitingDecision": false,
                    "challenge": 3,
                    "distanceFromExtract": 0,
                    "discoveredLoot": [],
                    "carriedLoot": [],
                    "equippedItems": [
                      { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                      { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                    ],
                    "logEntries": ["Moved one step closer to extract."]
                  }
                }
                """));
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "MoveTowardExtract");

        Assert.Single(actionClient.Requests);
        Assert.Equal(EncounterType.Neutral, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
    }

    [Fact]
    public async Task StartExtractHoldAsync_CallsBackend_And_UpdatesExtractionState()
    {
        var actionClient = CreateActionClient("start-extract-hold", _ =>
            CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 8,
                    "weaponMalfunction": false,
                    "encounterType": "Extraction",
                    "encounterDescription": "Server extraction pressure",
                    "contactState": "None",
                    "surpriseSide": "None",
                    "initiativeWinner": "None",
                    "openingActionsRemaining": 0,
                    "surprisePersistenceEligible": false,
                    "enemyName": "",
                    "enemyHealth": 0,
                    "lootContainer": "",
                    "awaitingDecision": false,
                    "challenge": 5,
                    "distanceFromExtract": 1,
                    "discoveredLoot": [],
                    "carriedLoot": [],
                    "equippedItems": [
                      { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                      { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                    ],
                    "logEntries": ["You drifted one step away from extract."]
                  }
                }
                """));
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "StartExtractHoldAsync");

        Assert.Single(actionClient.Requests);
        Assert.Equal(EncounterType.Extraction, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(5, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(1, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
    }

    [Fact]
    public async Task StartExtractHoldAsync_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("start-extract-hold", _ =>
            CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 8,
                    "weaponMalfunction": false,
                    "encounterType": "Extraction",
                    "encounterDescription": "Server extraction hold",
                    "contactState": "None",
                    "surpriseSide": "None",
                    "initiativeWinner": "None",
                    "openingActionsRemaining": 0,
                    "surprisePersistenceEligible": false,
                    "enemyName": "",
                    "enemyHealth": 0,
                    "lootContainer": "",
                    "awaitingDecision": false,
                    "challenge": 5,
                    "distanceFromExtract": 0,
                    "extractHoldActive": true,
                    "holdAtExtractUntil": "2026-03-28T12:34:56Z",
                    "discoveredLoot": [],
                    "carriedLoot": [],
                    "equippedItems": [
                      { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                      { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                    ],
                    "logEntries": ["Raid updated on server."]
                  }
                }
                """));
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "StartExtractHoldAsync");

        Assert.Single(actionClient.Requests);
        Assert.Equal(EncounterType.Extraction, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.True(Assert.IsType<bool>(GetField(home, "_extractHoldActive")));
        Assert.Equal(DateTimeOffset.Parse("2026-03-28T12:34:56Z"), (DateTimeOffset?)GetField(home, "_holdAtExtractUntil"));
    }

    [Fact]
    public void ExpiredExtractHold_IsNotTreatedAsActive()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SeedRaid(home);

        SetField(home, "_extractHoldActive", true);
        SetField(home, "_holdAtExtractUntil", DateTimeOffset.UtcNow.AddSeconds(-1));

        Assert.False(InvokePrivateBool(home, "IsExtractHoldEffectivelyActive"));
    }

    [Fact]
    public async Task StartExtractHoldAsync_DoesNotDispatch_WhenExtractHoldIsAlreadyActive()
    {
        var actionClient = CreateActionClient("unused", _ => throw new InvalidOperationException("Should not dispatch."));
        var home = CreateHome(actionClient);
        SeedRaid(home);

        SetField(home, "_extractHoldActive", true);
        SetField(home, "_holdAtExtractUntil", DateTimeOffset.UtcNow.AddMinutes(1));

        await InvokePrivateAsync(home, "StartExtractHoldAsync");

        Assert.Empty(actionClient.Requests);
    }

    [Fact]
    public void HomeAndRaidHudMarkup_BindTheExtractHoldContract()
    {
        var homeMarkup = File.ReadAllText(HomeMarkupPath);
        var raidHudMarkup = File.ReadAllText(RaidHudPath);

        Assert.Contains("OnStartExtractHold=\"StartExtractHoldAsync\"", homeMarkup);
        Assert.Contains("ExtractHoldActive=\"_extractHoldActive\"", homeMarkup);
        Assert.Contains("HoldAtExtractUntil=\"_holdAtExtractUntil\"", homeMarkup);
        Assert.DoesNotContain("OnStayAtExtract", homeMarkup);
        Assert.Contains("public bool ExtractHoldActive { get; set; }", raidHudMarkup);
        Assert.Contains("public DateTimeOffset? HoldAtExtractUntil { get; set; }", raidHudMarkup);
        Assert.Contains("Hold active for @GetExtractHoldCountdownText()", raidHudMarkup);
        Assert.Contains("disabled=\"@ExtractHoldActive\"", raidHudMarkup);
        Assert.Contains("Hold at Extract", raidHudMarkup);
        Assert.Contains("@GetExtractHoldCountdownText()", raidHudMarkup);
        Assert.DoesNotContain("Stay at Extract", raidHudMarkup);
    }

    [Fact]
    public async Task AttemptExtractAsync_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("attempt-extract", _ =>
            CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 8,
                    "weaponMalfunction": false,
                    "encounterType": "Extraction",
                    "encounterDescription": "Server extraction",
                    "contactState": "None",
                    "surpriseSide": "None",
                    "initiativeWinner": "None",
                    "openingActionsRemaining": 0,
                    "surprisePersistenceEligible": false,
                    "enemyName": "",
                    "enemyHealth": 0,
                    "lootContainer": "Dead Body",
                    "awaitingDecision": false,
                    "challenge": 5,
                    "distanceFromExtract": 0,
                    "discoveredLoot": [],
                    "carriedLoot": [],
                    "equippedItems": [
                      { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                      { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                    ],
                    "logEntries": ["Raid updated on server."]
                  }
                }
                """));
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "AttemptExtractAsync");

        Assert.Single(actionClient.Requests);
        Assert.Equal(EncounterType.Extraction, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal("Server extraction", Assert.IsType<string>(GetField(home, "_encounterDescription")));
        Assert.Equal(5, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        AssertOpeningPhaseFields(home, "None", "None", "None", 0, false);
    }

    [Fact]
    public void ApplyActiveRaidSnapshot_TransfersExtractHoldFieldsIntoClientState()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivate(
            home,
            "ApplyActiveRaidSnapshot",
            new RaidSnapshot(
                Health: 30,
                BackpackCapacity: 3,
                Ammo: 8,
                WeaponMalfunction: false,
                Medkits: 1,
                LootSlots: 0,
                Challenge: 0,
                DistanceFromExtract: 0,
                EncounterType: "Extraction",
                EncounterTitle: "Extraction",
                EncounterDescription: "Holding extract.",
                EnemyName: string.Empty,
                EnemyHealth: 0,
                EnemyDexterity: 0,
                EnemyConstitution: 0,
                EnemyStrength: 0,
                LootContainer: string.Empty,
                AwaitingDecision: false,
                ContactState: "None",
                SurpriseSide: "None",
                InitiativeWinner: "None",
                OpeningActionsRemaining: 0,
                SurprisePersistenceEligible: false,
                DiscoveredLoot: [],
                CarriedLoot: [],
                EquippedItems: [],
                LogEntries: [],
                ExtractHoldActive: true,
                HoldAtExtractUntil: DateTimeOffset.Parse("2026-03-28T12:34:56Z")));

        Assert.True(Assert.IsType<bool>(GetField(home, "_extractHoldActive")));
        Assert.Equal(DateTimeOffset.Parse("2026-03-28T12:34:56Z"), (DateTimeOffset?)GetField(home, "_holdAtExtractUntil"));
    }

    [Fact]
    public void RaidProjection_ClearsHoldTimestamp_WhenPartialUpdateDisablesExtractHold()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SeedRaid(home);

        InvokePrivate(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "RaidUpdated",
                null,
                JsonDocument.Parse("""
                    {
                      "raid": {
                        "extractHoldActive": true,
                        "holdAtExtractUntil": "2026-03-28T12:34:56Z"
                      }
                    }
                    """).RootElement.Clone(),
                null));

        InvokePrivate(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "RaidUpdated",
                null,
                JsonDocument.Parse("""
                    {
                      "raid": {
                        "extractHoldActive": false
                      }
                    }
                    """).RootElement.Clone(),
                null));

        Assert.False(Assert.IsType<bool>(GetField(home, "_extractHoldActive")));
        Assert.Null(GetField(home, "_holdAtExtractUntil"));
    }

    [Fact]
    public async Task RaidMovementActions_DoNotDispatch_WhenRaidIsMissing()
    {
        var actionClient = CreateActionClient("unused", _ => throw new InvalidOperationException("Should not dispatch."));
        var home = CreateHome(actionClient);

        await InvokePrivateAsync(home, "GoDeeper");
        await InvokePrivateAsync(home, "StartExtractHoldAsync");
        await InvokePrivateAsync(home, "MoveTowardExtract");
        await InvokePrivateAsync(home, "AttemptExtractAsync");

        Assert.Empty(actionClient.Requests);
    }

    [Fact]
    public void RaidLootAndEquipActions_RejectItemsThatWouldExceedWeightBudget()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var raid = new RaidState(
            30,
            RaidInventory.FromItems([ItemCatalog.Create("Makarov"), ItemCatalog.Create("Small Backpack")], [], 3));
        raid.MaxEncumbrance = 9;

        SetField(home, "_raid", raid);

        Assert.False(InvokePrivateBool(home, "CanLootItem", ItemCatalog.Create("6B13 assault armor")));
        Assert.False(InvokePrivateBool(home, "CanEquipRaidItem", ItemCatalog.Create("6B13 assault armor")));
    }

    [Fact]
    public void RaidLootAndEquipActions_UseAuthoritativeEncumbranceProjectionWhenAvailable()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SeedRaid(home);

        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        raid.MaxEncumbrance = 100;

        InvokePrivate(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "RaidUpdated",
                null,
                JsonDocument.Parse("""
                    {
                      "raid": {
                        "encumbrance": 98,
                        "maxEncumbrance": 100
                      }
                    }
                    """).RootElement.Clone(),
                null));

        Assert.Equal("98/100 lbs", InvokePrivate<string>(home, "GetRaidEncumbranceText"));
        Assert.False(InvokePrivateBool(home, "CanLootItem", ItemCatalog.Create("6B13 assault armor")));
        Assert.False(InvokePrivateBool(home, "CanEquipRaidItem", ItemCatalog.Create("6B13 assault armor")));
    }

    [Fact]
    public void RaidProjection_InvalidatesCachedEncumbrance_WhenInventoryChangesWithoutAProjection()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        SeedRaid(home);

        InvokePrivate(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "RaidUpdated",
                null,
                JsonDocument.Parse("""
                    {
                      "raid": {
                        "encumbrance": 98,
                        "maxEncumbrance": 100
                      }
                    }
                    """).RootElement.Clone(),
                null));

        InvokePrivate(
            home,
            "ApplyActionResult",
            new GameActionResult(
                "RaidUpdated",
                null,
                JsonDocument.Parse("""
                    {
                      "raid": {
                        "carriedLoot": [
                          { "name": "Bandage", "type": 3, "value": 15, "slots": 1, "rarity": 0, "displayRarity": 0, "weight": 1 }
                        ],
                        "equippedItems": [
                          { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3, "weight": 9 },
                          { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3, "weight": 4 }
                        ]
                      }
                    }
                    """).RootElement.Clone(),
                null));

        Assert.Equal("9/100 lbs", InvokePrivate<string>(home, "GetRaidEncumbranceText"));
    }

    [Fact]
    public void CanEquipRaidItem_DoesNotConsumeEqualButDistinctCarriedItem_WhenEvaluatingDiscoveredLoot()
    {
        var home = CreateHome(new FakeGameActionApiClient());
        var carriedArmor = ItemCatalog.Create("6B13 assault armor");
        var discoveredArmor = ItemCatalog.Create("6B13 assault armor");
        var raid = new RaidState(
            30,
            RaidInventory.FromItems([ItemCatalog.Create("Makarov"), ItemCatalog.Create("Small Backpack")], [carriedArmor], 3));
        raid.MaxEncumbrance = 16;

        SetField(home, "_raid", raid);

        Assert.False(InvokePrivateBool(home, "CanEquipRaidItem", discoveredArmor));
    }

    private static Home CreateHome(FakeGameActionApiClient actionClient)
    {
        var home = new Home();
        SetProperty(home, "Profiles", new FakeProfileApiClient());
        SetProperty(home, "Actions", actionClient);
        return home;
    }

    private static void SeedRaid(Home home)
    {
        SetField(home, "_inRaid", true);
        SetField(home, "_maxHealth", 34);
        SetField(home, "_raid", new RaidState(
            30,
            RaidInventory.FromItems([ItemCatalog.Create("AK74"), ItemCatalog.Create("Small Backpack")], [], 3)));
        SetField(home, "_encounterType", EncounterType.Combat);
        SetField(home, "_contactState", "PlayerAmbush");
        SetField(home, "_surpriseSide", "Player");
        SetField(home, "_initiativeWinner", "None");
        SetField(home, "_openingActionsRemaining", 1);
        SetField(home, "_surprisePersistenceEligible", true);
        SetField(home, "_enemyName", "Old Scav");
        SetField(home, "_enemyHealth", 15);
        SetField(home, "_ammo", 8);
    }

    private static GameActionResult CreateRaidResult(string projectionJson)
    {
        return new GameActionResult(
            "RaidUpdated",
            null,
            JsonDocument.Parse(projectionJson).RootElement.Clone(),
            "Action resolved.");
    }

    private static FakeGameActionApiClient CreateActionClient(string expectedAction, Func<JsonElement, GameActionResult> responseFactory)
    {
        return new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal(expectedAction, request.Action);
                return responseFactory(request.Payload);
            }
        };
    }

    private static readonly string HomeMarkupPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Pages", "Home.razor"));
    private static readonly string RaidHudPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RaidLoop.Client", "Components", "RaidHUD.razor"));

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

    private static void AssertOpeningPhaseFields(
        Home home,
        string contactState,
        string surpriseSide,
        string initiativeWinner,
        int openingActionsRemaining,
        bool surprisePersistenceEligible)
    {
        Assert.Equal(contactState, Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal(surpriseSide, Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal(initiativeWinner, Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(openingActionsRemaining, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.Equal(surprisePersistenceEligible, Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var task = method!.Invoke(instance, args) as Task;
        Assert.NotNull(task);
        await task!;
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(instance, args);
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<T>(method!.Invoke(instance, args));
    }

    private static bool InvokePrivateBool(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method!.Invoke(instance, args)!;
    }

    private sealed class FakeProfileApiClient : IProfileApiClient
    {
        public Task<AuthBootstrapResponse> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthBootstrapResponse(
                true,
                "player@example.com",
                new PlayerSnapshot(500, [], [], 12, 34, DateTimeOffset.MinValue, null, null)));
        }
    }

    private sealed class FakeGameActionApiClient : IGameActionApiClient
    {
        public List<GameActionRequest> Requests { get; } = [];

        public Func<GameActionRequest, GameActionResult> ResponseFactory { get; set; } =
            _ => throw new InvalidOperationException("No response configured.");

        public Task<GameActionResult> SendAsync(string action, object payload, CancellationToken cancellationToken = default)
        {
            var request = new GameActionRequest(action, JsonSerializer.SerializeToElement(payload));
            Requests.Add(request);
            return Task.FromResult(ResponseFactory(request));
        }
    }
}
