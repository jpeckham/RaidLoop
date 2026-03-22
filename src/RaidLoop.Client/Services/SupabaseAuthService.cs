using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using RaidLoop.Client.Configuration;
using Supabase.Gotrue;

namespace RaidLoop.Client.Services;

public sealed class SupabaseAuthService : ISupabaseSessionProvider
{
    private const string SessionStorageKey = "raidloop.auth.session.v1";
    private const string PkceVerifierStorageKey = "raidloop.auth.pkce-verifier.v1";

    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly IClientTelemetryService Telemetry;
    private readonly SupabaseOptions _options;

    private Supabase.Client? _client;
    private bool _isInitialized;
    private bool _isSignedOutLocally;

    public SupabaseAuthService(
        IJSRuntime jsRuntime,
        NavigationManager navigationManager,
        IClientTelemetryService telemetry,
        IOptions<SupabaseOptions> options)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        Telemetry = telemetry;
        _options = options.Value;
    }

    public event Action? AuthStateChanged;

    public bool IsLoading { get; private set; }

    public bool IsAuthenticated => !_isSignedOutLocally && _client?.Auth.CurrentSession is not null;

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
        _client.Auth.AddStateChangedListener((_, _) => _ = HandleAuthSessionChangedAsync());
        _isSignedOutLocally = false;

        var currentUri = new Uri(_navigationManager.Uri);
        if (TryGetQueryParameter(currentUri, "code", out var code))
        {
            Session? session = null;
            var pkceVerifier = await LoadPkceVerifierAsync();
            if (!string.IsNullOrWhiteSpace(pkceVerifier))
            {
                try
                {
                    session = await _client.Auth.ExchangeCodeForSession(pkceVerifier, code);
                }
                catch (Exception ex)
                {
                    await ReportHandledErrorAsync("Supabase PKCE session exchange failed.", "auth-session", ex);
                    throw;
                }
            }

            if (session is not null)
            {
                await PersistSessionAsync(session);
            }

            await ClearPkceVerifierAsync();
            _navigationManager.NavigateTo(GetCurrentPathWithoutQueryOrFragment(), replace: true);
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
                catch (Exception ex)
                {
                    await ReportHandledErrorAsync("Supabase session restore failed.", "auth-session", ex);
                    await ClearPersistedSessionAsync();
                }
            }
        }

        if (_client.Auth.CurrentSession is not null)
        {
            _isSignedOutLocally = false;
            await PersistSessionAsync(_client.Auth.CurrentSession);
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

        _isSignedOutLocally = false;

        var providerState = await _client!.Auth.SignIn(
            Supabase.Gotrue.Constants.Provider.Google,
            new SignInOptions
            {
                FlowType = Constants.OAuthFlowType.PKCE,
                RedirectTo = GetCurrentUriWithoutQueryOrFragment()
            });

        if (!string.IsNullOrWhiteSpace(providerState.PKCEVerifier))
        {
            await PersistPkceVerifierAsync(providerState.PKCEVerifier);
        }

        _navigationManager.NavigateTo(providerState.Uri.ToString(), forceLoad: true);
    }

    public async Task SignOutAsync()
    {
        if (_client?.Auth is not null)
        {
            try
            {
                await _client.Auth.SignOut();
            }
            catch (Exception ex)
            {
                await ReportHandledErrorAsync("Supabase remote sign-out failed.", "auth-session", ex);
                // Force a local sign-out path when the remote session is already invalid.
            }
        }

        _isSignedOutLocally = true;
        await ClearPersistedSessionAsync();
        await ClearPkceVerifierAsync();
        NotifyAuthStateChanged();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (_client is null)
        {
            await InitializeAsync();
        }

        if (_client is null)
        {
            throw new InvalidOperationException("Supabase client is not available.");
        }

        var session = _client.Auth.CurrentSession;
        if (session?.ExpiresAt().Subtract(TimeSpan.FromMinutes(1)) <= DateTime.UtcNow)
        {
            try
            {
                await _client.Auth.RefreshSession();
                session = _client.Auth.CurrentSession;
            }
            catch (Exception ex)
            {
                await ReportHandledErrorAsync("Supabase session refresh failed.", "auth-session", ex);
                _isSignedOutLocally = true;
                await ClearPersistedSessionAsync();
                NotifyAuthStateChanged();
                throw new InvalidOperationException("Supabase session refresh failed.");
            }
        }

        var accessToken = session?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Supabase session is not available.");
        }

        return accessToken;
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

    private async Task PersistPkceVerifierAsync(string verifier)
    {
        await _jsRuntime.InvokeVoidAsync("raidLoopStorage.save", PkceVerifierStorageKey, verifier);
    }

    private Task<string?> LoadPkceVerifierAsync()
    {
        return _jsRuntime.InvokeAsync<string?>("raidLoopStorage.load", PkceVerifierStorageKey).AsTask();
    }

    private async Task ClearPkceVerifierAsync()
    {
        await _jsRuntime.InvokeVoidAsync("raidLoopStorage.remove", PkceVerifierStorageKey);
    }

    private async Task HandleAuthSessionChangedAsync()
    {
        if (_client?.Auth.CurrentSession is not null)
        {
            _isSignedOutLocally = false;
            await PersistSessionAsync(_client.Auth.CurrentSession);
        }
        else
        {
            _isSignedOutLocally = true;
            await ClearPersistedSessionAsync();
        }

        NotifyAuthStateChanged();
    }

    private void NotifyAuthStateChanged()
    {
        AuthStateChanged?.Invoke();
    }

    private ValueTask ReportHandledErrorAsync(string message, string source, Exception? exception = null)
    {
        return Telemetry.ReportErrorAsync(
            message,
            new
            {
                source,
                exception = exception?.GetType().FullName,
                exceptionMessage = exception?.Message,
                stack = exception?.ToString()
            });
    }

    private string GetCurrentUriWithoutQueryOrFragment()
    {
        var currentUri = new Uri(_navigationManager.Uri);
        return currentUri.GetLeftPart(UriPartial.Path);
    }

    private string GetCurrentPathWithoutQueryOrFragment()
    {
        var currentUri = new Uri(_navigationManager.Uri);
        var path = currentUri.GetLeftPart(UriPartial.Path);
        return _navigationManager.ToBaseRelativePath(path) switch
        {
            "" => ".",
            var relativePath => relativePath
        };
    }

    private static bool TryGetQueryParameter(Uri uri, string key, out string value)
    {
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (!string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private sealed record PersistedSession(string AccessToken, string RefreshToken);

}
