using Microsoft.JSInterop;

namespace RaidLoop.Client.Services;

public sealed class ClientTelemetryService : IClientTelemetryService
{
    private readonly IJSRuntime _jsRuntime;

    public ClientTelemetryService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask ReportErrorAsync(string message, object? details = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "RaidLoopTelemetry.reportError",
                cancellationToken,
                message,
                details);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"RaidLoop telemetry reporting failed: {ex}");
        }
    }
}
