using System.Reflection;
using System.Text.Json;
using RaidLoop.Client;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

public sealed class ContractsTests
{
    [Fact]
    public void AuthBootstrapResponse_RoundTripsThroughJson()
    {
        var response = new AuthBootstrapResponse(
            IsAuthenticated: true,
            UserEmail: "player@example.com",
                Snapshot: new PlayerSnapshot(
                    Money: 500,
                    MainStash: [ItemCatalog.Create("Makarov")],
                    OnPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("Small Backpack"), true)],
                    ShopStock: [new ShopOfferSnapshot(2, 60, 1), new ShopOfferSnapshot(3, 160, 1)],
                    AcceptedStats: new PlayerStats(8, 14, 12, 10, 13, 16),
                DraftStats: new PlayerStats(8, 15, 12, 10, 13, 16),
                AvailableStatPoints: 5,
                StatsAccepted: true,
                PlayerConstitution: 10,
                PlayerMaxHealth: 30,
                RandomCharacterAvailableAt: DateTimeOffset.Parse("2026-03-18T00:00:00Z"),
                RandomCharacter: new RandomCharacterSnapshot("Ghost-101", [ItemCatalog.Create("Bandage")], PlayerStats.Default),
                ActiveRaid: new RaidSnapshot(
                    Health: 30,
                    BackpackCapacity: 3,
                    Encumbrance: 19,
                    MaxEncumbrance: 40,
                    Ammo: 8,
                    WeaponMalfunction: false,
                    Medkits: 1,
                    LootSlots: 0,
                    Challenge: 0,
                    DistanceFromExtract: 0,
                    EncounterType: "Neutral",
                    EncounterTitle: "Area Clear",
                    EncounterDescription: "Nothing found.",
                    EnemyName: string.Empty,
                    EnemyHealth: 0,
                    EnemyDexterity: 0,
                    EnemyConstitution: 0,
                    EnemyStrength: 0,
                    LootContainer: string.Empty,
                    AwaitingDecision: false,
                    ContactState: "PlayerAmbush",
                    SurpriseSide: "Player",
                    InitiativeWinner: "None",
                    OpeningActionsRemaining: 1,
                    SurprisePersistenceEligible: false,
                    DiscoveredLoot: [],
                    CarriedLoot: [],
                    EquippedItems: [ItemCatalog.Create("Makarov")],
                    LogEntries: ["Raid started."])));

        var json = JsonSerializer.Serialize(response);
        var roundTrip = JsonSerializer.Deserialize<AuthBootstrapResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(roundTrip);
        Assert.True(roundTrip!.IsAuthenticated);
        Assert.Equal("player@example.com", roundTrip.UserEmail);
        Assert.Equal(500, roundTrip.Snapshot.Money);
        Assert.Equal("Makarov", Assert.Single(roundTrip.Snapshot.MainStash).Name);
        Assert.Equal([2, 3], roundTrip.Snapshot.ShopStock.Select(item => item.ItemDefId).ToArray());
        Assert.Equal([60, 160], roundTrip.Snapshot.ShopStock.Select(item => item.Price).ToArray());
        Assert.Equal(14, roundTrip.Snapshot.AcceptedStats.Dexterity);
        Assert.Equal(15, roundTrip.Snapshot.DraftStats.Dexterity);
        Assert.Equal(5, roundTrip.Snapshot.AvailableStatPoints);
        Assert.True(roundTrip.Snapshot.StatsAccepted);
        Assert.Equal(10, roundTrip.Snapshot.PlayerConstitution);
        Assert.Equal(30, roundTrip.Snapshot.PlayerMaxHealth);
        Assert.Equal(19, roundTrip.Snapshot.ActiveRaid!.Encumbrance);
        Assert.Equal(40, roundTrip.Snapshot.ActiveRaid.MaxEncumbrance);
        Assert.Equal("Neutral", roundTrip.Snapshot.ActiveRaid!.EncounterType);
        Assert.Equal("PlayerAmbush", roundTrip.Snapshot.ActiveRaid.ContactState);
        Assert.Equal("Player", roundTrip.Snapshot.ActiveRaid.SurpriseSide);
        Assert.Equal("None", roundTrip.Snapshot.ActiveRaid.InitiativeWinner);
        Assert.Equal(1, roundTrip.Snapshot.ActiveRaid.OpeningActionsRemaining);
        Assert.False(roundTrip.Snapshot.ActiveRaid.SurprisePersistenceEligible);
    }

    [Fact]
    public void RandomCharacterSnapshot_RoundTripsStatsThroughJson()
    {
        const string json = """
            {
              "IsAuthenticated": true,
              "UserEmail": "player@example.com",
              "Snapshot": {
                "Money": 500,
                "MainStash": [],
                "OnPersonItems": [],
                "ShopStock": [],
                "PlayerConstitution": 10,
                "PlayerMaxHealth": 30,
                "RandomCharacterAvailableAt": "2026-03-18T00:00:00Z",
                "RandomCharacter": {
                  "Name": "Ghost-101",
                  "Inventory": [
                    { "Name": "Bandage", "Type": 4, "Value": 15, "Slots": 1, "Rarity": 0, "DisplayRarity": 0, "Weight": 1 }
                  ],
                  "Stats": {
                    "Strength": 12,
                    "Dexterity": 11,
                    "Constitution": 10,
                    "Intelligence": 9,
                    "Wisdom": 8,
                    "Charisma": 13
                  }
                },
                "ActiveRaid": null
              }
            }
            """;

        var roundTrip = JsonSerializer.Deserialize<AuthBootstrapResponse>(json);

        Assert.NotNull(roundTrip);
        var randomCharacter = roundTrip!.Snapshot.RandomCharacter;
        Assert.NotNull(randomCharacter);
        var statsProperty = randomCharacter.GetType().GetProperty("Stats", BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(statsProperty);
        Assert.Equal(new PlayerStats(12, 11, 10, 9, 8, 13), Assert.IsType<PlayerStats>(statsProperty!.GetValue(randomCharacter)));
    }

    [Fact]
    public void AuthBootstrapResponse_DeserializesLegacyExtractHoldTimestamp()
    {
        const string json = """
            {
              "isAuthenticated": true,
              "userEmail": "player@example.com",
              "snapshot": {
                "money": 500,
                "mainStash": [],
                "onPersonItems": [],
                "shopStock": [],
                "playerConstitution": 10,
                "playerMaxHealth": 30,
                "randomCharacterAvailableAt": "2026-03-18T00:00:00Z",
                "randomCharacter": null,
                "activeRaid": {
                  "health": 30,
                  "backpackCapacity": 3,
                  "ammo": 8,
                  "weaponMalfunction": false,
                  "medkits": 1,
                  "lootSlots": 0,
                  "challenge": 4,
                  "distanceFromExtract": 0,
                  "encounterType": "Extraction",
                  "encounterTitle": "Extraction Opportunity",
                  "encounterDescription": "Holding at extract.",
                  "enemyName": "",
                  "enemyHealth": 0,
                  "enemyDexterity": 0,
                  "enemyConstitution": 0,
                  "enemyStrength": 0,
                  "lootContainer": "",
                  "awaitingDecision": false,
                  "contactState": "None",
                  "surpriseSide": "None",
                  "initiativeWinner": "None",
                  "openingActionsRemaining": 0,
                  "surprisePersistenceEligible": false,
                  "discoveredLoot": [],
                  "carriedLoot": [],
                  "equippedItems": [],
                  "logEntries": [],
                  "encumbrance": 0,
                  "maxEncumbrance": 0,
                  "extractHoldActive": true,
                  "holdAtExtractUntil": "2026-03-28 12:34:56"
                }
              }
            }
            """;

        var roundTrip = JsonSerializer.Deserialize<AuthBootstrapResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(roundTrip);
        Assert.Equal(DateTimeOffset.Parse("2026-03-28T12:34:56Z"), roundTrip!.Snapshot.ActiveRaid!.HoldAtExtractUntil);
    }

    [Fact]
    public void AuthBootstrapResponse_RoundTripsItemDefIdInMainStash()
    {
        const string json = """
            {
              "isAuthenticated": true,
              "userEmail": "player@example.com",
              "snapshot": {
                "money": 500,
                "mainStash": [
                  { "itemDefId": 2, "type": 0, "value": 60, "slots": 1, "rarity": 0, "displayRarity": 1, "weight": 2 }
                ],
                "onPersonItems": [],
                "shopStock": [],
                "playerConstitution": 10,
                "playerMaxHealth": 30,
                "randomCharacterAvailableAt": "2026-03-18T00:00:00Z",
                "randomCharacter": null,
                "activeRaid": null
              }
            }
            """;

        var serialized = RoundTripBootstrapJson(json);

        Assert.Contains("\"itemDefId\":2", serialized);
        Assert.DoesNotContain("\"itemKey\":", serialized);
        Assert.DoesNotContain("\"name\":", serialized);
    }

    [Fact]
    public void AuthBootstrapResponse_RoundTripsLeanShopOffersWithoutEmbeddedItems()
    {
        const string json = """
            {
              "isAuthenticated": true,
              "userEmail": "player@example.com",
              "snapshot": {
                "money": 500,
                "mainStash": [],
                "onPersonItems": [],
                "shopStock": [
                  { "itemDefId": 2, "price": 60, "stock": 1 },
                  { "itemDefId": 3, "price": 160, "stock": 1 }
                ],
                "itemRules": [
                  { "itemDefId": 2, "type": 0, "weight": 2, "slots": 1, "rarity": 0 },
                  { "itemDefId": 3, "type": 0, "weight": 12, "slots": 1, "rarity": 1 }
                ],
                "playerConstitution": 10,
                "playerMaxHealth": 30,
                "randomCharacterAvailableAt": "2026-03-18T00:00:00Z",
                "randomCharacter": null,
                "activeRaid": null
              }
            }
            """;

        var serialized = RoundTripBootstrapJson(json);

        Assert.Contains("\"shopStock\":[{\"itemDefId\":2,\"price\":60,\"stock\":1},{\"itemDefId\":3,\"price\":160,\"stock\":1}]", serialized);
        Assert.DoesNotContain("\"name\":", serialized);
        Assert.DoesNotContain("\"itemKey\":", serialized);
        Assert.Contains("\"itemRules\":[{\"itemDefId\":2,\"type\":0,\"weight\":2,\"slots\":1,\"rarity\":0},{\"itemDefId\":3,\"type\":0,\"weight\":12,\"slots\":1,\"rarity\":1}]", serialized);
    }

    [Fact]
    public void AuthBootstrapResponse_RoundTripsItemDefIdInOnPersonItems()
    {
        const string json = """
            {
              "isAuthenticated": true,
              "userEmail": "player@example.com",
              "snapshot": {
                "money": 500,
                "mainStash": [],
                "onPersonItems": [
                  {
                    "item": { "itemDefId": 18, "type": 2, "value": 600, "slots": 4, "rarity": 4, "displayRarity": 4, "weight": 8 },
                    "isEquipped": true
                  }
                ],
                "shopStock": [],
                "playerConstitution": 10,
                "playerMaxHealth": 30,
                "randomCharacterAvailableAt": "2026-03-18T00:00:00Z",
                "randomCharacter": null,
                "activeRaid": null
              }
            }
            """;

        var serialized = RoundTripBootstrapJson(json);

        Assert.Contains("\"itemDefId\":18", serialized);
        Assert.DoesNotContain("\"itemKey\":", serialized);
        Assert.DoesNotContain("\"name\":", serialized);
    }

    [Fact]
    public void AuthBootstrapResponse_RoundTripsItemDefIdInActiveRaid()
    {
        const string json = """
            {
              "isAuthenticated": true,
              "userEmail": "player@example.com",
              "snapshot": {
                "money": 500,
                "mainStash": [],
                "onPersonItems": [],
                "shopStock": [],
                "playerConstitution": 10,
                "playerMaxHealth": 30,
                "randomCharacterAvailableAt": "2026-03-18T00:00:00Z",
                "randomCharacter": null,
                "activeRaid": {
                  "health": 30,
                  "backpackCapacity": 3,
                  "ammo": 8,
                  "weaponMalfunction": false,
                  "medkits": 1,
                  "lootSlots": 0,
                  "challenge": 0,
                  "distanceFromExtract": 0,
                  "encounterType": "Neutral",
                  "encounterTitle": "Area Clear",
                  "encounterDescription": "Nothing found.",
                  "enemyName": "",
                  "enemyHealth": 0,
                  "enemyDexterity": 0,
                  "enemyConstitution": 0,
                  "enemyStrength": 0,
                  "lootContainer": "",
                  "awaitingDecision": false,
                  "contactState": "PlayerAmbush",
                  "surpriseSide": "Player",
                  "initiativeWinner": "None",
                  "openingActionsRemaining": 1,
                  "surprisePersistenceEligible": false,
                  "discoveredLoot": [
                    { "itemDefId": 8, "type": 1, "value": 95, "slots": 1, "rarity": 0, "displayRarity": 1, "weight": 9 }
                  ],
                  "carriedLoot": [],
                  "equippedItems": [
                    { "itemDefId": 4, "type": 0, "value": 320, "slots": 1, "rarity": 2, "displayRarity": 3, "weight": 7 }
                  ],
                  "logEntries": ["Raid started."],
                  "encumbrance": 19,
                  "maxEncumbrance": 40,
                  "extractHoldActive": false,
                  "holdAtExtractUntil": null
                }
              }
            }
            """;

        var serialized = RoundTripBootstrapJson(json);

        Assert.Contains("\"itemDefId\":8", serialized);
        Assert.Contains("\"itemDefId\":4", serialized);
        Assert.DoesNotContain("\"itemKey\":", serialized);
        Assert.DoesNotContain("\"name\":", serialized);
    }

    [Fact]
    public void AuthBootstrapResponse_DeserializesLegacyItemPayloadsWithoutItemKeys()
    {
        const string json = """
            {
              "isAuthenticated": true,
              "userEmail": "player@example.com",
              "snapshot": {
                "money": 500,
                "mainStash": [
                  { "name": "Makarov", "type": 0, "value": 60, "slots": 1, "rarity": 0, "displayRarity": 1 }
                ],
                "onPersonItems": [],
                "shopStock": [],
                "playerConstitution": 10,
                "playerMaxHealth": 30,
                "randomCharacterAvailableAt": "2026-03-18T00:00:00Z",
                "randomCharacter": null,
                "activeRaid": null
              }
            }
            """;

        var roundTrip = JsonSerializer.Deserialize<AuthBootstrapResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(roundTrip);
        Assert.Equal("Makarov", Assert.Single(roundTrip!.Snapshot.MainStash).Name);
    }

    [Fact]
    public void Item_DoesNotInferIdentityFromLegacyName()
    {
        var item = new Item("Makarov", ItemType.Weapon, Weight: 2);

        Assert.Equal(0, item.ItemDefId);
        Assert.Equal(string.Empty, item.Key);
    }

    [Fact]
    public void ItemPresentationCatalog_FallsBackToNameWhenItemDefinitionIdIsUnknown()
    {
        var item = new Item("Legacy label", ItemType.Weapon, Weight: 2)
        {
            ItemDefId = 99999
        };

        Assert.Equal("Legacy label", ItemPresentationCatalog.GetLabel(item));
    }

    [Fact]
    public void GameActionRequest_HasExplicitActionEnvelope()
    {
        var request = new GameActionRequest(
            Action: "attack",
            Payload: JsonDocument.Parse("{\"target\":\"enemy\"}").RootElement.Clone());

        Assert.Equal("attack", request.Action);
        Assert.Equal("enemy", request.Payload.GetProperty("target").GetString());
    }

    [Fact]
    public void GameActionResult_Serializes_WithoutSnapshotShape()
    {
        var response = new GameActionResult(
            EventType: "CombatResolved",
            Event: JsonDocument.Parse("""{ "enemyDamage": 2 }""").RootElement.Clone(),
            Projections: JsonDocument.Parse("""{ "raid": { "health": 17 } }""").RootElement.Clone(),
            Message: "Action resolved.");

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("\"snapshot\"", json);
        Assert.Contains("\"message\":\"Action resolved.\"", json);
        Assert.Contains("\"eventType\":\"CombatResolved\"", json);
        Assert.Contains("\"event\":", json);
        Assert.Contains("\"projections\":", json);
    }

    [Fact]
    public void GameActionResult_DeserializesEventEnvelopeAndProjectionPayload()
    {
        const string json = """
            {
              "eventType": "CombatResolved",
              "event": {
                "enemyDamage": 2,
                "playerDamage": 3,
                "ammoSpent": 1,
                "weaponMalfunctioned": true
              },
              "projections": {
                "raid": {
                  "health": 17,
                  "enemyHealth": 6,
                  "ammo": 4,
                  "weaponMalfunction": true
                }
              },
              "message": "Action resolved."
            }
            """;

        var response = JsonSerializer.Deserialize<GameActionResult>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(response);
        Assert.Equal("CombatResolved", response!.EventType);
        Assert.True(response.Event.HasValue);
        Assert.Equal(2, response.Event.Value.GetProperty("enemyDamage").GetInt32());
        Assert.True(response.Projections.HasValue);
        Assert.Equal(17, response.Projections.Value.GetProperty("raid").GetProperty("health").GetInt32());
        Assert.Equal("Action resolved.", response.Message);
    }

    private static string RoundTripBootstrapJson(string json)
    {
        var roundTrip = JsonSerializer.Deserialize<AuthBootstrapResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(roundTrip);
        return JsonSerializer.Serialize(roundTrip, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}


