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
                RandomCharacterAvailableAt: DateTimeOffset.Parse("2026-03-18T00:00:00Z"),
                RandomCharacter: new RandomCharacterSnapshot("Ghost-101", [ItemCatalog.Create("Bandage")]),
                ActiveRaid: new RaidSnapshot(
                    Health: 30,
                    BackpackCapacity: 3,
                    Ammo: 8,
                    WeaponMalfunction: false,
                    Medkits: 1,
                    LootSlots: 0,
                    ExtractProgress: 0,
                    ExtractRequired: 3,
                    EncounterType: "Neutral",
                    EncounterTitle: "Area Clear",
                    EncounterDescription: "Nothing found.",
                    EnemyName: string.Empty,
                    EnemyHealth: 0,
                    LootContainer: string.Empty,
                    AwaitingDecision: false,
                    DiscoveredLoot: [],
                    CarriedLoot: [],
                    EquippedItems: [ItemCatalog.Create("Makarov")],
                    LogEntries: ["Raid started."])));

        var json = JsonSerializer.Serialize(response);
        var roundTrip = JsonSerializer.Deserialize<AuthBootstrapResponse>(json);

        Assert.NotNull(roundTrip);
        Assert.True(roundTrip!.IsAuthenticated);
        Assert.Equal("player@example.com", roundTrip.UserEmail);
        Assert.Equal(500, roundTrip.Snapshot.Money);
        Assert.Equal("Makarov", Assert.Single(roundTrip.Snapshot.MainStash).Name);
        Assert.Equal("Neutral", roundTrip.Snapshot.ActiveRaid!.EncounterType);
    }

    [Fact]
    public void GameActionRequest_And_Response_HaveExplicitActionEnvelope()
    {
        var request = new GameActionRequest(
            Action: "attack",
            Payload: JsonDocument.Parse("{\"target\":\"enemy\"}").RootElement.Clone());
        var response = new GameActionResponse(
            Snapshot: new PlayerSnapshot(
                Money: 640,
                MainStash: [],
                OnPersonItems: [],
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: null),
            Message: "Action resolved.");

        Assert.Equal("attack", request.Action);
        Assert.Equal("enemy", request.Payload.GetProperty("target").GetString());
        Assert.Equal("Action resolved.", response.Message);
        Assert.Equal(640, response.Snapshot.Money);
    }

    [Fact]
    public void GameActionResponse_DeserializesLegacySupabaseUtcTimestampFormat()
    {
        const string json = """
            {
              "snapshot": {
                "money": 54,
                "mainStash": [],
                "onPersonItems": [],
                "randomCharacterAvailableAt": "2026-03-20 02:39:44.905934",
                "randomCharacter": null,
                "activeRaid": null
              },
              "message": null
            }
            """;

        var response = JsonSerializer.Deserialize<GameActionResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(response);
        Assert.Equal(
            DateTimeOffset.Parse("2026-03-20T02:39:44.905934+00:00"),
            response!.Snapshot.RandomCharacterAvailableAt);
    }
}
