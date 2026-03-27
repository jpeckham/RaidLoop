using System.Reflection;
using System.Text.Json;
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
                ShopStock: [ItemCatalog.Create("Makarov"), ItemCatalog.Create("PPSH")],
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
        Assert.Equal(["Makarov", "PPSH"], roundTrip.Snapshot.ShopStock.Select(item => item.Name).ToArray());
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
}
