using RaidLoop.Core.Contracts;

namespace RaidLoop.Client.Services;

public interface IGameActionApiClient
{
    Task<GameActionResponse> SendAsync(string action, object payload, CancellationToken cancellationToken = default);
}
