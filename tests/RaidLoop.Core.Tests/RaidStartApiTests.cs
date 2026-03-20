using System.Reflection;
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
                        ActiveRaid: BuildRaidSnapshot("Combat", "Server combat", "Server Scav", 17, 9)),
                    null);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_onPersonItems", new List<OnPersonEntry>
        {
            new(ItemCatalog.Create("AK74"), true),
            new(ItemCatalog.Create("Small Backpack"), true)
        });

        await InvokePrivateAsync(home, "StartMainRaidAsync");

        Assert.Single(actionClient.Requests);
        Assert.True(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.Equal(9, Assert.IsType<int>(GetField(home, "_ammo")));
        Assert.Equal("Server Scav", Assert.IsType<string>(GetField(home, "_enemyName")));
        Assert.Equal(17, Assert.IsType<int>(GetField(home, "_enemyHealth")));
        Assert.Equal(EncounterType.Combat, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal("AK74", raid.Inventory.EquippedWeapon?.Name);
        Assert.Equal("Small Backpack", raid.Inventory.EquippedBackpack?.Name);
    }

    [Fact]
    public async Task StartRandomRaidAsync_CallsBackend_And_HydratesAuthoritativeRaidSnapshot()
    {
        var actionClient = new FakeGameActionApiClient
        {
            ResponseFactory = request =>
            {
                Assert.Equal("start-random-raid", request.Action);
                return new GameActionResponse(
                    new PlayerSnapshot(
                        Money: 500,
                        MainStash: [],
                        OnPersonItems: [],
                        RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                        RandomCharacter: new RandomCharacterSnapshot("Ghost-101", [ItemCatalog.Create("Makarov")]),
                        ActiveRaid: BuildRaidSnapshot("Loot", "Server loot", string.Empty, 0, 8, [ItemCatalog.Create("Makarov")])),
                    null);
            }
        };
        var home = CreateHome(actionClient);

        SetField(home, "_randomCharacterAvailableAt", DateTimeOffset.MinValue);
        SetField(home, "_randomCharacter", null);

        await InvokePrivateAsync(home, "StartRandomRaidAsync");

        Assert.Single(actionClient.Requests);
        Assert.True(Assert.IsType<bool>(GetField(home, "_inRaid")));
        Assert.Equal(EncounterType.Loot, Assert.IsType<EncounterType>(GetField(home, "_encounterType")));
        Assert.Equal("Server loot", Assert.IsType<string>(GetField(home, "_encounterDescription")));
        Assert.Equal(8, Assert.IsType<int>(GetField(home, "_ammo")));
        var raid = Assert.IsType<RaidState>(GetField(home, "_raid"));
        Assert.Equal("Makarov", raid.Inventory.EquippedWeapon?.Name);
    }

    private static RaidSnapshot BuildRaidSnapshot(string encounterType, string encounterDescription, string enemyName, int enemyHealth, int ammo, IReadOnlyList<Item>? equippedItems = null)
    {
        return new RaidSnapshot(
            Health: 27,
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
            DiscoveredLoot: [],
            CarriedLoot: [],
            EquippedItems: equippedItems ?? [ItemCatalog.Create("AK74"), ItemCatalog.Create("Small Backpack")],
            LogEntries: ["Raid started on server."]);
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
