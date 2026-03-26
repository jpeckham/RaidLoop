using System.Reflection;
using System.Text.Json;
using RaidLoop.Client;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

public sealed class RaidStartApiTests
{
    [Fact]
    public async Task StartMainRaidAsync_CallsBackend_And_HydratesAuthoritativeRaidSnapshot()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("start-main-raid", request.Action);
                return new GameActionResult(
                    "RaidStarted",
                    null,
                    JsonDocument.Parse("""
                        {
                          "raid": {
                            "health": 27,
                            "backpackCapacity": 3,
                            "ammo": 9,
                            "weaponMalfunction": false,
                            "medkits": 1,
                            "lootSlots": 0,
                            "challenge": 0,
                            "distanceFromExtract": 3,
                            "encounterType": "Combat",
                            "encounterTitle": "Extract Route Ambush",
                            "encounterDescription": "You are ambushed while moving between positions.",
                            "contactState": "EnemyAmbush",
                            "surpriseSide": "Enemy",
                            "initiativeWinner": "None",
                            "openingActionsRemaining": 1,
                            "surprisePersistenceEligible": false,
                            "enemyName": "Server Scav",
                            "enemyHealth": 17,
                            "lootContainer": "Dead Body",
                            "awaitingDecision": false,
                            "discoveredLoot": [],
                            "carriedLoot": [],
                            "equippedItems": [
                              { "name": "AK74", "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3 },
                              { "name": "Small Backpack", "type": 2, "value": 75, "slots": 2, "rarity": 2, "displayRarity": 3 }
                            ],
                            "logEntries": ["Raid started on server."]
                          }
                        }
                        """).RootElement.Clone(),
                    null);
            }
        };
        var home = CreateHome(actionClient);
        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(500, [], [], 12, 34, DateTimeOffset.MinValue, null, null));

        SetField(home, "_onPersonItems", new List<OnPersonEntry>
        {
            new(ItemCatalog.Create("AK74"), true),
            new(ItemCatalog.Create("Small Backpack"), true)
        });

        await InvokePrivateAsync(home, "StartMainRaidAsync");

        Assert.Single(actionClient.Requests);
        Assert.True(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.Equal(9, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        Assert.Equal("Server Scav", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(17, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal(34, Assert.IsType<int>(GetField(home, "_maxHealth")));
        Assert.Equal(EncounterType.Combat, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal("EnemyAmbush", Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal("Enemy", Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal("None", Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(1, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal("AK74", raid.Inventory.EquippedWeapon?.Name);
        Assert.Equal("Small Backpack", raid.Inventory.EquippedBackpack?.Name);
    }

    [Fact]
    public async Task StartRandomRaidAsync_CallsBackend_And_HydratesMutualContactCombatSnapshot()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("start-random-raid", request.Action);
                return new GameActionResult(
                    "RaidStarted",
                    null,
                    JsonDocument.Parse("""
                        {
                          "luckRun": {
                            "randomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
                            "randomCharacter": {
                              "name": "Ghost-101",
                              "inventory": [
                                { "name": "Makarov", "type": 0, "value": 60, "slots": 1, "rarity": 0, "displayRarity": 1 }
                              ]
                            }
                          },
                          "raid": {
                            "health": 27,
                            "backpackCapacity": 3,
                            "ammo": 8,
                            "weaponMalfunction": false,
                            "medkits": 1,
                            "lootSlots": 0,
                            "challenge": 0,
                            "distanceFromExtract": 3,
                            "encounterType": "Combat",
                            "encounterTitle": "Roadside Contact",
                            "encounterDescription": "You and a scav patrol spot each other at the same moment.",
                            "contactState": "MutualContact",
                            "surpriseSide": "None",
                            "initiativeWinner": "Player",
                            "openingActionsRemaining": 0,
                            "surprisePersistenceEligible": false,
                            "enemyName": "Road Scav",
                            "enemyHealth": 14,
                            "lootContainer": "Dead Body",
                            "awaitingDecision": false,
                            "discoveredLoot": [],
                            "carriedLoot": [],
                            "equippedItems": [
                              { "name": "Makarov", "type": 0, "value": 60, "slots": 1, "rarity": 0, "displayRarity": 1 }
                            ],
                            "logEntries": ["Raid started on server."]
                          }
                        }
                        """).RootElement.Clone(),
                    null);
            }
        };
        var home = CreateHome(actionClient);
        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(500, [], [], 12, 34, DateTimeOffset.MinValue, null, null));

        SetField(home, "_randomCharacterAvailableAt", DateTimeOffset.MinValue);
        SetField(home, "_randomCharacter", null);

        await InvokePrivateAsync(home, "StartRandomRaidAsync");

        Assert.Single(actionClient.Requests);
        Assert.True(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.Equal(EncounterType.Combat, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        Assert.Equal("MutualContact", Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal("None", Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal("Player", Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
        Assert.Equal("Road Scav", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(14, Assert.IsType<int>(GetField(home, "_enemyHealth")));
    }

    [Fact]
    public async Task StartRandomRaidAsync_CallsBackend_And_HydratesAuthoritativeRaidSnapshot()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("start-random-raid", request.Action);
                return new GameActionResult(
                    "RaidStarted",
                    null,
                    JsonDocument.Parse("""
                        {
                          "luckRun": {
                            "randomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
                            "randomCharacter": {
                              "name": "Ghost-101",
                              "inventory": [
                                { "name": "Makarov", "type": 0, "value": 60, "slots": 1, "rarity": 0, "displayRarity": 1 }
                              ]
                            }
                          },
                          "raid": {
                            "health": 27,
                            "backpackCapacity": 3,
                            "ammo": 8,
                            "weaponMalfunction": false,
                            "medkits": 1,
                            "lootSlots": 0,
                            "challenge": 0,
                            "distanceFromExtract": 3,
                            "encounterType": "Loot",
                            "encounterTitle": "Server Encounter",
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
                            "carriedLoot": [],
                            "equippedItems": [
                              { "name": "Makarov", "type": 0, "value": 60, "slots": 1, "rarity": 0, "displayRarity": 1 }
                            ],
                            "logEntries": ["Raid started on server."]
                          }
                        }
                        """).RootElement.Clone(),
                    null);
            }
        };
        var home = CreateHome(actionClient);
        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(500, [], [], 12, 34, DateTimeOffset.MinValue, null, null));

        SetField(home, "_randomCharacterAvailableAt", DateTimeOffset.MinValue);
        SetField(home, "_randomCharacter", null);

        await InvokePrivateAsync(home, "StartRandomRaidAsync");

        Assert.Single(actionClient.Requests);
        Assert.True(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.Equal(EncounterType.Loot, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_challenge")));
        Assert.Equal(3, Assert.IsType<int>(GetField(home, "_distanceFromExtract")));
        Assert.Equal("Server loot", Assert.IsType<string>(GetField(home, "_encounterDescription")));
        Assert.Equal(8, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(34, Assert.IsType<int>(GetField(home, "_maxHealth")));
        Assert.Equal("None", Assert.IsType<string>(GetField(home, "_contactState")));
        Assert.Equal("None", Assert.IsType<string>(GetField(home, "_surpriseSide")));
        Assert.Equal("None", Assert.IsType<string>(GetField(home, "_initiativeWinner")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_surprisePersistenceEligible")));
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal("Makarov", raid.Inventory.EquippedWeapon?.Name);
    }

    private static Home CreateHome(FakeGameActionApiClient actionClient)
    {
        var home = new Home();
        SetProperty(home, "Profiles", new FakeProfileApiClient());
        SetProperty(home, "Actions", actionClient);
        return home;
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

    private static void InvokePrivateVoid(object instance, string methodName, params object[] args)
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
