using System.Text.Json;

namespace RaidLoop.Core.Contracts;

public sealed record GameActionRequest(
    string Action,
    JsonElement Payload);
