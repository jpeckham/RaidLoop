namespace RaidLoop.Core.Contracts;

public sealed record AuthBootstrapResponse(
    bool IsAuthenticated,
    string? UserEmail,
    PlayerSnapshot Snapshot);
