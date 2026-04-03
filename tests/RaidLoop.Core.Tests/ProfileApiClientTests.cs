using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaidLoop.Client.Configuration;
using RaidLoop.Client.Services;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

public sealed class ProfileApiClientTests
{
    [Fact]
    public async Task BootstrapAsync_SendsBearerToken_And_ParsesSnapshot()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "isAuthenticated": true,
                  "userEmail": "player@example.com",
                  "snapshot": {
                    "money": 640,
                    "mainStash": [{ "itemDefId": 2 }],
                    "onPersonItems": [{ "item": { "itemDefId": 14 }, "isEquipped": true }],
                    "shopStock": [
                      { "itemDefId": 2, "price": 60, "stock": 1 },
                      { "itemDefId": 3, "price": 160, "stock": 1 }
                    ],
                    "itemRules": [
                      { "itemDefId": 2, "type": 0, "weight": 2, "slots": 1, "rarity": 0 },
                      { "itemDefId": 3, "type": 0, "weight": 12, "slots": 1, "rarity": 1 },
                      { "itemDefId": 14, "type": 2, "weight": 1, "slots": 1, "rarity": 0 }
                    ],
                    "acceptedStats": { "strength": 8, "dexterity": 12, "constitution": 10, "intelligence": 9, "wisdom": 11, "charisma": 14 },
                    "draftStats": { "strength": 8, "dexterity": 13, "constitution": 10, "intelligence": 9, "wisdom": 11, "charisma": 14 },
                    "availableStatPoints": 6,
                    "statsAccepted": false,
                    "playerConstitution": 10,
                    "playerMaxHealth": 30,
                    "randomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
                    "randomCharacter": null,
                    "activeRaid": null
                  }
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dblgbpzlrglcdwqyagnx.supabase.co/functions/v1/")
        };
        var client = new ProfileApiClient(
            httpClient,
            new StubSessionProvider("token-123"),
            new SupabaseOptions
            {
                Url = "https://dblgbpzlrglcdwqyagnx.supabase.co",
                PublishableKey = "publishable-key"
            });

        var response = await client.BootstrapAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://dblgbpzlrglcdwqyagnx.supabase.co/functions/v1/profile-bootstrap", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("token-123", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Equal("publishable-key", Assert.Single(handler.LastRequest.Headers.GetValues("apikey")));
        Assert.True(response.IsAuthenticated);
        Assert.Equal(640, response.Snapshot.Money);
        Assert.Equal([2, 3, 14], response.Snapshot.ItemRules.Select(rule => rule.ItemDefId).ToArray());
        Assert.Equal("makarov", Assert.Single(response.Snapshot.MainStash).Key);
        Assert.Equal("small_backpack", Assert.Single(response.Snapshot.OnPersonItems).Item.Key);
        Assert.Equal([2, 3], response.Snapshot.ShopStock.Select(item => item.ItemDefId).ToArray());
        Assert.Equal([60, 160], response.Snapshot.ShopStock.Select(item => item.Price).ToArray());
        Assert.Equal(12, response.Snapshot.AcceptedStats.Dexterity);
        Assert.Equal(13, response.Snapshot.DraftStats.Dexterity);
        Assert.Equal(6, response.Snapshot.AvailableStatPoints);
        Assert.False(response.Snapshot.StatsAccepted);
    }

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode statusCode, T body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubSessionProvider(string accessToken) : ISupabaseSessionProvider
    {
        public string? UserEmail => "player@example.com";

        public Task<string> GetAccessTokenAsync()
        {
            return Task.FromResult(accessToken);
        }
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult() ?? string.Empty;
            return Task.FromResult(_responder(request));
        }
    }
}

