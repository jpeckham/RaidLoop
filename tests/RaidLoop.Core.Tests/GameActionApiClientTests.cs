using System.Net;
using System.Text;
using System.Text.Json;
using RaidLoop.Client.Configuration;
using RaidLoop.Client.Services;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Core.Tests;

public sealed class GameActionApiClientTests
{
    [Fact]
    public async Task SendAsync_RejectsLegacySnapshotResponse()
    {
        var handler = new FakeHandler(_ =>
        {
            const string payload = """
                {
                  "snapshot": {
                    "money": 640,
                    "mainStash": [],
                    "onPersonItems": [],
                    "randomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
                    "randomCharacter": null,
                    "activeRaid": null
                  },
                  "message": "Profile saved."
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });
        var client = CreateClient(handler);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SendAsync("sell-stash-item", new { stashIndex = 0 }));

        Assert.Equal("Game action returned legacy snapshot payload.", error.Message);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://dblgbpzlrglcdwqyagnx.supabase.co/functions/v1/game-action", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("token-123", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Equal("publishable-key", Assert.Single(handler.LastRequest.Headers.GetValues("apikey")));
    }

    [Fact]
    public async Task SendAsync_MapsEnvelopeResponseToGameActionResult()
    {
        var handler = new FakeHandler(_ =>
        {
            var payload = """
                {
                  "eventType": "CombatResolved",
                  "event": {
                    "enemyDamage": 2,
                    "playerDamage": 3,
                    "ammoSpent": 1
                  },
                  "projections": {
                    "economy": {
                      "money": 640
                    },
                    "raid": {
                      "ammo": 4
                    }
                  },
                  "message": "Action resolved."
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });
        var client = CreateClient(handler);

        var result = await client.SendAsync("attack", new { target = "enemy" });

        Assert.Equal("CombatResolved", result.EventType);
        Assert.NotNull(result.Event);
        Assert.Equal(2, result.Event!.Value.GetProperty("enemyDamage").GetInt32());
        Assert.Equal(3, result.Event.Value.GetProperty("playerDamage").GetInt32());
        Assert.NotNull(result.Projections);
        Assert.Equal(640, result.Projections!.Value.GetProperty("economy").GetProperty("money").GetInt32());
        Assert.Equal(4, result.Projections.Value.GetProperty("raid").GetProperty("ammo").GetInt32());
        Assert.Equal("Action resolved.", result.Message);
    }

    private static GameActionApiClient CreateClient(FakeHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dblgbpzlrglcdwqyagnx.supabase.co/functions/v1/")
        };

        return new GameActionApiClient(
            httpClient,
            new StubSessionProvider("token-123"),
            new SupabaseOptions
            {
                Url = "https://dblgbpzlrglcdwqyagnx.supabase.co",
                PublishableKey = "publishable-key"
            });
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}
