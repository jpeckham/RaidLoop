using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaidLoop.Client.Configuration;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Client.Services;

public sealed class ProfileApiClient : IProfileApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ISupabaseSessionProvider _sessionProvider;
    private readonly string _publishableKey;

    public ProfileApiClient(
        HttpClient httpClient,
        ISupabaseSessionProvider sessionProvider,
        SupabaseOptions options)
    {
        _httpClient = httpClient;
        _sessionProvider = sessionProvider;
        _publishableKey = options.PublishableKey;
    }

    public async Task<AuthBootstrapResponse> BootstrapAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "profile-bootstrap")
        {
            Content = JsonContent.Create(new { }, options: JsonOptions)
        };

        await AuthorizeAsync(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthBootstrapResponse>(JsonOptions, cancellationToken);
        return payload ?? throw new InvalidOperationException("Profile bootstrap returned no payload.");
    }

    private async Task AuthorizeAsync(HttpRequestMessage request)
    {
        var accessToken = await _sessionProvider.GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", _publishableKey);
    }
}
