namespace RaidLoop.Client.Services;

public interface ISupabaseSessionProvider
{
    string? UserEmail { get; }

    Task<string> GetAccessTokenAsync();
}
