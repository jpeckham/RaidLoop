using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using RaidLoop.Client.Configuration;
using Supabase.Gotrue;

namespace RaidLoop.Client.Services;

public sealed class SupabaseAuthService
{
    private const string SessionStorageKey = "raidloop.auth.session.v1";

    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly SupabaseOptions _options;

    private Supabase.Client? _client;
    private bool _isInitialized;

    public SupabaseAuthService(
        IJSRuntime jsRuntime,
        NavigationManager navigationManager,
        IOptions<SupabaseOptions> options)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        _options = options.Value;
    }

    public event Action? AuthStateChanged;

    public bool IsLoading { get; private set; }

    public bool IsAuthenticated => _client?.Auth.CurrentSession is not null;

    public string? UserEmail => _client?.Auth.CurrentUser?.Email;

    public Supabase.Client? Client => _client;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        IsLoading = true;
        NotifyAuthStateChanged();

        _client = new Supabase.Client(
            _options.Url,
            _options.PublishableKey,
            new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false
            });

        await _client.InitializeAsync();
        _client.Auth.AddStateChangedListener((_, _) => NotifyAuthStateChanged());

        var currentUri = new Uri(_navigationManager.Uri);
        if (currentUri.Query.Contains("code=", StringComparison.OrdinalIgnoreCase))
        {
            var session = await _client.Auth.GetSessionFromUrl(currentUri);
            if (session is not null)
            {
                await PersistSessionAsync(session);
            }

            var cleanPath = _navigationManager.ToBaseRelativePath(_navigationManager.Uri).Split('?', '#')[0];
            _navigationManager.NavigateTo(string.IsNullOrWhiteSpace(cleanPath) ? "/" : cleanPath, replace: true);
        }
        else
        {
            var persisted = await LoadPersistedSessionAsync();
            if (persisted is not null)
            {
                try
                {
                    await _client.Auth.SetSession(persisted.AccessToken, persisted.RefreshToken, false);
                }
                catch
                {
                    await ClearPersistedSessionAsync();
                }
            }
        }

        _isInitialized = true;
        IsLoading = false;
        NotifyAuthStateChanged();
    }

    public async Task SignInWithGoogleAsync()
    {
        if (_client is null)
        {
            await InitializeAsync();
        }

        var providerState = await _client!.Auth.SignIn(
            Supabase.Gotrue.Constants.Provider.Google,
            new SignInOptions
            {
                RedirectTo = _navigationManager.BaseUri
            });

        _navigationManager.NavigateTo(providerState.Uri.ToString(), forceLoad: true);
    }

    public async Task SignOutAsync()
    {
        if (_client?.Auth is not null)
        {
            await _client.Auth.SignOut();
        }

        await ClearPersistedSessionAsync();
        NotifyAuthStateChanged();
    }

    private async Task PersistSessionAsync(Session session)
    {
        if (string.IsNullOrWhiteSpace(session.AccessToken) || string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            return;
        }

        var persisted = new PersistedSession(session.AccessToken, session.RefreshToken);
        var payload = JsonSerializer.Serialize(persisted);
        await _jsRuntime.InvokeVoidAsync("raidLoopStorage.save", SessionStorageKey, payload);
    }

    private async Task<PersistedSession?> LoadPersistedSessionAsync()
    {
        var payload = await _jsRuntime.InvokeAsync<string?>("raidLoopStorage.load", SessionStorageKey);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PersistedSession>(payload);
        }
        catch
        {
            return null;
        }
    }

    private async Task ClearPersistedSessionAsync()
    {
        await _jsRuntime.InvokeVoidAsync("raidLoopStorage.remove", SessionStorageKey);
    }

    private void NotifyAuthStateChanged()
    {
        AuthStateChanged?.Invoke();
    }

    private sealed record PersistedSession(string AccessToken, string RefreshToken);

}
