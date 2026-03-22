namespace RaidLoop.Client.Services;

public interface IClientTelemetryService
{
    ValueTask ReportErrorAsync(string message, object? details = null, CancellationToken cancellationToken = default);
}
