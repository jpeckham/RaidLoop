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
    }

    [Fact]
    public async Task ContinueSearching_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("continue-searching", _ =>
            CreateRaidResult("""
                {
                  "raid": {
                    "health": 28,
                    "ammo": 8,
                    "weaponMalfunction": false,
                    "encounterType": "Loot",
                    "encounterDescription": "Server loot",
                    "enemyName": "",
                    "enemyHealth": 0,
                    "lootContainer": "Dead Body",
                    "awaitingDecision": false,
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

        InvokePrivate(home, "ContinueSearching");

        Assert.Single(actionClient.Requests);
        Assert.Equal(EncounterType.Loot, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal("Scrap Metal", Assert.Single(raid.Inventory.DiscoveredLoot).Name);
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
                    "enemyName": "",
                    "enemyHealth": 0,
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
                """));
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "AttemptExtractAsync");

        Assert.Single(actionClient.Requests);
        Assert.Equal(EncounterType.Extraction, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal("Server extraction", Assert.IsType<string>(GetField(home, "_encounterDescription")));
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
