using RaidLoop.Core.Contracts;

namespace RaidLoop.Client.Services;

public interface IProfileApiClient
{
    Task<AuthBootstrapResponse> BootstrapAsync(CancellationToken cancellationToken = default);
}
