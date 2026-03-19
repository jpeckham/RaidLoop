namespace RaidLoop.Core.Contracts;

public sealed record GameActionResponse(
    PlayerSnapshot Snapshot,
    string? Message);
