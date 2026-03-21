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

    public async Task<GameActionResult> SendAsync(string action, object payload, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "game-action")
        {
            Content = JsonContent.Create(new GameActionRequest(action, JsonSerializer.SerializeToElement(payload)), options: JsonOptions)
        };

        await AuthorizeAsync(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Game action returned no payload.");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object
            && (root.TryGetProperty("eventType", out _)
                || root.TryGetProperty("event", out _)
                || root.TryGetProperty("projections", out _)))
        {
            var result = JsonSerializer.Deserialize<GameActionResult>(json, JsonOptions);
            return result ?? throw new InvalidOperationException("Game action returned no payload.");
        }

        var legacy = JsonSerializer.Deserialize<GameActionResponse>(json, JsonOptions);
        if (legacy is null)
        {
            throw new InvalidOperationException("Game action returned no payload.");
        }

        return new GameActionResult("LegacySnapshot", null, null, legacy.Snapshot, legacy.Message);
    }

    private async Task AuthorizeAsync(HttpRequestMessage request)
    {
        var accessToken = await _sessionProvider.GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", _publishableKey);
    }
}
