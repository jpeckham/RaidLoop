using System.Reflection;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using RaidLoop.Client.Configuration;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

[Collection("GameEventLog")]
public class GameEventValueScenarioTests : IDisposable
{
    public GameEventValueScenarioTests()
    {
        GameEventLog.Clear();
    }

    [Fact]
    public void ClientNoLongerImplementsLocalRaidSettlement()
    {
        var method = typeof(Home).GetMethod("EndRaidAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Null(method);
    }

    [Fact]
    public async Task TakeLootAsync_EmitsLootAcquiredWithItemValue()
    {
        var home = CreateHome();
        var loot = new Item("AK47", ItemType.Weapon, Value: 20, Slots: 1, Rarity: Rarity.Legendary);
        var raid = new RaidState(
            health: 30,
            inventory: RaidInventory.FromItems([], [], backpackCapacity: 4));
        RaidEngine.StartDiscoveredLootEncounter(raid, [loot]);

        SetField(home, "_raid", raid);
        SetField(home, "_activeRaidId", "raid-loot");

        await InvokePrivateAsync(home, "TakeLootAsync", loot);

        var evt = Assert.Single(GameEventLog.Events);
        Assert.Equal("loot.acquired", evt.EventName);
        Assert.Equal(20, Assert.Single(evt.Items).Value);
    }

    [Fact]
    public async Task SellStashItemAsync_UsesItemValueForPayout()
    {
        var home = CreateHome();
        var soldItem = new Item("AK47", ItemType.Weapon, Value: 20, Slots: 1, Rarity: Rarity.Legendary);

        SetField(home, "_mainGame", new GameState([soldItem]));
        SetField(home, "_money", 0);
        SetField(home, "_onPersonItems", new List<OnPersonEntry>());

        await InvokePrivateAsync(home, "SellStashItemAsync", 0);

        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        var fallback = Assert.Single(mainGame.Stash);
        Assert.Equal("Rusty Knife", fallback.Name);
        Assert.Equal(20, Assert.IsType<int>(GetField(home, "_money")));
    }

    [Fact]
    public void ClientNoLongerTracksLocalRaidProfileSettlementState()
    {
        var profileField = typeof(Home).GetField("_activeProfile", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Null(profileField);
    }

    public void Dispose()
    {
        GameEventLog.Clear();
    }

    private static Home CreateHome()
    {
        var home = new Home();
        var profilesProperty = home.GetType().GetProperty("Profiles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(profilesProperty);
        profilesProperty!.SetValue(home, CreateProfileApiClient());
        var actionsProperty = home.GetType().GetProperty("Actions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(actionsProperty);
        actionsProperty!.SetValue(home, CreateGameActionApiClient());
        return home;
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

    private static ProfileApiClient CreateProfileApiClient()
    {
        var httpClient = new HttpClient(new FakeHandler())
        {
            BaseAddress = new Uri("https://dblgbpzlrglcdwqyagnx.supabase.co/functions/v1/")
        };
        return new ProfileApiClient(
            httpClient,
            new StubSessionProvider(),
            new SupabaseOptions
            {
                Url = "https://dblgbpzlrglcdwqyagnx.supabase.co",
                PublishableKey = "publishable-key"
            });
    }

    private static IGameActionApiClient CreateGameActionApiClient()
    {
        return new StubGameActionApiClient();
    }

    private sealed class StubSessionProvider : ISupabaseSessionProvider
    {
        public string? UserEmail => "player@example.com";

        public Task<string> GetAccessTokenAsync()
        {
            return Task.FromResult("token-123");
        }
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = request.RequestUri!.AbsolutePath.EndsWith("/profile-bootstrap", StringComparison.Ordinal)
                ? "{\"isAuthenticated\":true,\"userEmail\":\"player@example.com\",\"snapshot\":{\"money\":500,\"mainStash\":[],\"onPersonItems\":[],\"randomCharacterAvailableAt\":\"0001-01-01T00:00:00+00:00\",\"randomCharacter\":null,\"activeRaid\":null}}"
                : "{\"message\":\"Profile saved.\"}";

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(payload)
            });
        }
    }

    private sealed class StubGameActionApiClient : IGameActionApiClient
    {
        public Task<GameActionResult> SendAsync(string action, object payload, CancellationToken cancellationToken = default)
        {
            if (string.Equals(action, "sell-stash-item", StringComparison.Ordinal))
            {
                return Task.FromResult(new GameActionResult(
                    "ProfileMutated",
                    null,
                    null,
                    new PlayerSnapshot(
                        20,
                        [ItemCatalog.Create("Rusty Knife")],
                        [],
                        DateTimeOffset.MinValue,
                        null,
                        null),
                    null));
            }

            return Task.FromResult(new GameActionResult(
                "ProfileMutated",
                null,
                null,
                new PlayerSnapshot(0, [], [], DateTimeOffset.MinValue, null, null),
                null));
        }
    }
}
