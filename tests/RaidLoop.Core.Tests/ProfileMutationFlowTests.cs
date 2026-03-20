using System.Reflection;
using RaidLoop.Client;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

public sealed class ProfileMutationFlowTests
{
    [Fact]
    public async Task SellStashItemAsync_DelegatesToActionApi_And_AppliesReturnedSnapshot()
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
    public async Task MoveStashToOnPersonAsync_DelegatesToActionApi_And_AppliesReturnedSnapshot()
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
    public async Task BuyFromShopAsync_DelegatesToActionApi_And_AppliesReturnedSnapshot()
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
    public async Task SellLuckRunItemAsync_DelegatesToActionApi_And_AppliesReturnedSnapshot()
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

    private static Home CreateHome(FakeGameActionApiClient actionClient)
    {
        var home = new Home();
        SetProperty(home, "Profiles", new FakeProfileApiClient());
        SetProperty(home, "Actions", actionClient);
        return home;
    }

    private static GameActionResponse Response(
        int money,
        IReadOnlyList<Item> mainStash,
        IReadOnlyList<OnPersonSnapshot> onPersonItems,
        DateTimeOffset? randomCharacterAvailableAt = null,
        RandomCharacterSnapshot? randomCharacter = null)
    {
        return new GameActionResponse(
            new PlayerSnapshot(
                Money: money,
                MainStash: mainStash,
                OnPersonItems: onPersonItems,
                RandomCharacterAvailableAt: randomCharacterAvailableAt ?? DateTimeOffset.MinValue,
                RandomCharacter: randomCharacter,
                ActiveRaid: null),
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
            var jsonPayload = System.Text.Json.JsonSerializer.SerializeToElement(payload);
            var request = new GameActionRequest(action, jsonPayload);
            Requests.Add(request);
            return Task.FromResult(ResponseFactory(request));
        }
    }
}
