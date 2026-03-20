using System.Reflection;
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
            return CreateRaidResponse("Combat", "Server combat", "Scav", 11, ammo: 7);
        });
        var home = CreateHome(actionClient);
        SeedRaid(home);

        await InvokePrivateAsync(home, "AttackAsync");

        Assert.Single(actionClient.Requests);
        Assert.Equal(7, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal(11, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal("Scav", Assert.IsType<string>(GetField(home, "_enemyName")));
    }

    [Fact]
    public async Task TakeLootAsync_CallsBackend_And_AppliesReturnedRaidSnapshot()
    {
        var actionClient = CreateActionClient("take-loot", payload =>
        {
            Assert.Equal("Bandage", payload.GetProperty("itemName").GetString());
            return CreateRaidResponse(
                "Loot",
                "Server loot",
                string.Empty,
                0,
                ammo: 8,
                carriedLoot: [ItemCatalog.Create("Bandage")],
                discoveredLoot: []);
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
            CreateRaidResponse("Loot", "Server loot", string.Empty, 0, ammo: 8, discoveredLoot: [ItemCatalog.Create("Scrap Metal")]));
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
            CreateRaidResponse("Extraction", "Server extraction", string.Empty, 0, ammo: 8));
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
        SetField(home, "_raid", new RaidState(
            30,
            RaidInventory.FromItems([ItemCatalog.Create("AK74"), ItemCatalog.Create("Small Backpack")], [], 3)));
        SetField(home, "_encounterType", EncounterType.Combat);
        SetField(home, "_enemyName", "Old Scav");
        SetField(home, "_enemyHealth", 15);
        SetField(home, "_ammo", 8);
    }

    private static GameActionResponse CreateRaidResponse(
        string encounterType,
        string encounterDescription,
        string enemyName,
        int enemyHealth,
        int ammo,
        IReadOnlyList<Item>? carriedLoot = null,
        IReadOnlyList<Item>? discoveredLoot = null)
    {
        return new GameActionResponse(
            new PlayerSnapshot(
                Money: 500,
                MainStash: [],
                OnPersonItems:
                [
                    new OnPersonSnapshot(ItemCatalog.Create("AK74"), true),
                    new OnPersonSnapshot(ItemCatalog.Create("Small Backpack"), true)
                ],
                RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                RandomCharacter: null,
                ActiveRaid: new RaidSnapshot(
                    Health: 28,
                    BackpackCapacity: 3,
                    Ammo: ammo,
                    WeaponMalfunction: false,
                    Medkits: 1,
                    LootSlots: 0,
                    ExtractProgress: 1,
                    ExtractRequired: 3,
                    EncounterType: encounterType,
                    EncounterTitle: "Server Encounter",
                    EncounterDescription: encounterDescription,
                    EnemyName: enemyName,
                    EnemyHealth: enemyHealth,
                    LootContainer: "Dead Body",
                    AwaitingDecision: false,
                    DiscoveredLoot: discoveredLoot ?? [ItemCatalog.Create("Bandage")],
                    CarriedLoot: carriedLoot ?? [],
                    EquippedItems: [ItemCatalog.Create("AK74"), ItemCatalog.Create("Small Backpack")],
                    LogEntries: ["Raid updated on server."])),
            null);
    }

    private static FakeGameActionApiClient CreateActionClient(string expectedAction, Func<System.Text.Json.JsonElement, GameActionResponse> responseFactory)
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
                new PlayerSnapshot(500, [], [], DateTimeOffset.MinValue, null, null)));
        }
    }

    private sealed class FakeGameActionApiClient : IGameActionApiClient
    {
        public List<GameActionRequest> Requests { get; } = [];

        public Func<GameActionRequest, GameActionResponse> ResponseFactory { get; set; } =
            _ => throw new InvalidOperationException("No response configured.");

        public Task<GameActionResponse> SendAsync(string action, object payload, CancellationToken cancellationToken = default)
        {
            var request = new GameActionRequest(action, System.Text.Json.JsonSerializer.SerializeToElement(payload));
            Requests.Add(request);
            return Task.FromResult(ResponseFactory(request));
        }
    }
}
