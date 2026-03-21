using System.Text.Json;

namespace RaidLoop.Core.Contracts;

public record GameActionResult(
    string EventType,
    JsonElement? Event,
    JsonElement? Projections,
    string? Message);
