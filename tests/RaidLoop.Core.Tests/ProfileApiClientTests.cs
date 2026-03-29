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
        var handler = new FakeHandler(_ =>
        {
            var response = new AuthBootstrapResponse(
                IsAuthenticated: true,
                UserEmail: "player@example.com",
                Snapshot: new PlayerSnapshot(
                    Money: 640,
                    MainStash: [ItemCatalog.Create("Makarov")],
                    OnPersonItems: [new OnPersonSnapshot(ItemCatalog.Create("Small Backpack"), true)],
                    ShopStock: [ItemCatalog.Create("Makarov"), ItemCatalog.Create("PPSH")],
                    AcceptedStats: new PlayerStats(8, 12, 10, 9, 11, 14),
                    DraftStats: new PlayerStats(8, 13, 10, 9, 11, 14),
                    AvailableStatPoints: 6,
                    StatsAccepted: false,
                    PlayerConstitution: 10,
                    PlayerMaxHealth: 30,
                    RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                    RandomCharacter: null,
                    ActiveRaid: null));

            return JsonResponse(HttpStatusCode.OK, response);
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
        Assert.Equal("Small Backpack", Assert.Single(response.Snapshot.OnPersonItems).Item.Name);
        Assert.Equal(["Makarov", "PPSH"], response.Snapshot.ShopStock.Select(item => item.Name).ToArray());
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
