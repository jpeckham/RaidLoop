using System.Net;
using System.Reflection;
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
                Assert.Equal("Medkit", request.Payload.GetProperty("itemName").GetString());
                return Response(
                    money: 490,
                    mainStash: [],
                    onPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("Medkit"), false)]);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_money", 500);
        SetField(home, "_onPersonItems", new List<OnPersonEntry>());

        await InvokePrivateAsync(home, "BuyFromShopAsync", new ShopStock(ItemCatalog.Create("Medkit")));

        Assert.Single(actionClient.Requests);
        Assert.Equal(490, Assert.IsType<int>(GetField(home, "_money")));
        var onPerson = Assert.IsType<List<OnPersonEntry>>(GetField(home, "_onPersonItems"));
        Assert.Equal("Medkit", Assert.Single(onPerson).Item.Name);
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
        SetField(home, "_randomCharacter", new RandomCharacterState("Ghost-101", [ItemCatalog.Create("Bandage")]));

        await InvokePrivateAsync(home, "SellLuckRunItemAsync", 0);

        Assert.Single(actionClient.Requests);
        Assert.Equal(520, Assert.IsType<int>(GetField(home, "_money")));
        Assert.Null(GetField(home, "_randomCharacter"));
        Assert.Equal(cooldown, Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
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
                  ]
                }
              },
              "raid": {
                "health": 21,
                "backpackCapacity": 6,
                "ammo": 4,
                "weaponMalfunction": true,
                "medkits": 1,
                "lootSlots": 0,
                "extractProgress": 2,
                "extractRequired": 3,
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
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal(21, raid.Health);
        Assert.Equal(6, raid.BackpackCapacity);
        Assert.Equal(4, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.True(Assert.IsType<bool>(GetField(home, "_weaponMalfunction")));
        Assert.Equal(2, Assert.IsType<int>(GetField(home, "_extractProgress")));
        Assert.Equal("Scav", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(6, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal("Combat", Assert.IsType<EncounterType>(GetField(home, "_encounterType")).ToString());
        Assert.Equal("Action resolved.", Assert.IsType<string>(GetField(home, "_resultMessage")));
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
                          ]
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
        Assert.Equal("Ghost-303", Assert.IsType<RandomCharacterState>(GetField(home, "_randomCharacter")).Name);
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
        SetField(home, "_extractProgress", 1);
        SetField(home, "_ammo", 5);
        SetField(home, "_weaponMalfunction", false);
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
        Assert.True(Assert.IsType<bool>(GetField(home, "_weaponMalfunction")));
        Assert.True(Assert.IsType<bool>(GetField(home, "_awaitingDecision")));
        Assert.Equal(1, Assert.IsType<int>(GetField(home, "_extractProgress")));
        Assert.Equal(EncounterType.Loot, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal("Weapons Crate", Assert.IsType<string>(GetField(home, "_lootContainer")));
        Assert.Equal("Patrol Guard", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(4, Assert.IsType<int>(GetField(home, "_enemyHealth")));
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
        SetField(home, "_extractProgress", 2);
        SetField(home, "_ammo", 7);
        SetField(home, "_weaponMalfunction", true);
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
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_extractProgress")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_weaponMalfunction")));
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
            ["Raid started as Main Character.", "You hit Scav for 4.", "Scav hits you for 3."],
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
        SetField(home, "_extractProgress", 2);
        SetField(home, "_ammo", 5);
        SetField(home, "_weaponMalfunction", true);
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
                null));

        Assert.Null(GetField(home, "_raid"));
        Assert.False(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_awaitingDecision")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_extractProgress")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.False(Assert.IsType<bool>(GetField(home, "_weaponMalfunction")));
        Assert.Equal(EncounterType.Neutral, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_encounterDescription")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(0, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal(string.Empty, Assert.IsType<string>(GetField(home, "_lootContainer")));
        Assert.Empty(Assert.IsType<List<string>>(GetField(home, "_log")));

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
    public void ApplySnapshot_ClearsEmptyRandomCharacter_And_LeavesReadyStateWhenCooldownMissing()
    {
        var home = CreateHome(new FakeGameActionApiClient());

        InvokePrivateVoid(
            home,
            "ApplySnapshot",
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems: [],
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: new RandomCharacterSnapshot("Ghost-101", []),
                ActiveRaid: null));

        Assert.Null(GetField(home, "_randomCharacter"));
        Assert.Equal(DateTimeOffset.MinValue, Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
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
                RandomCharacterAvailableAt: expectedCooldown,
                RandomCharacter: new RandomCharacterSnapshot("Ghost-101", []),
                ActiveRaid: null));

        Assert.Null(GetField(home, "_randomCharacter"));
        Assert.Equal(expectedCooldown, Assert.IsType<DateTimeOffset>(GetField(home, "_randomCharacterAvailableAt")));
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
        DateTimeOffset? randomCharacterAvailableAt = null,
        RandomCharacterSnapshot? randomCharacter = null)
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
                new PlayerSnapshot(500, [], [], DateTimeOffset.MinValue, null, null)));
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
