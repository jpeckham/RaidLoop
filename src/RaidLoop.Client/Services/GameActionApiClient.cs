using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaidLoop.Client.Configuration;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Client.Services;

public sealed class GameActionApiClient : IGameActionApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ISupabaseSessionProvider _sessionProvider;
    private readonly string _publishableKey;

    public GameActionApiClient(
        HttpClient httpClient,
        ISupabaseSessionProvider sessionProvider,
        SupabaseOptions options)
    {
        _httpClient = httpClient;
        _sessionProvider = sessionProvider;
        _publishableKey = options.PublishableKey;
    }

    public async Task<GameActionResponse> SendAsync(string action, object payload, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "game-action")
        {
            Content = JsonContent.Create(new GameActionRequest(action, JsonSerializer.SerializeToElement(payload)), options: JsonOptions)
        };

        await AuthorizeAsync(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<GameActionResponse>(JsonOptions, cancellationToken);
        return body ?? throw new InvalidOperationException("Game action returned no payload.");
    }

    private async Task AuthorizeAsync(HttpRequestMessage request)
    {
        var accessToken = await _sessionProvider.GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", _publishableKey);
    }
}
