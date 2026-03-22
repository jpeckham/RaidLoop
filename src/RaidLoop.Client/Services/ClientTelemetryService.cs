using Microsoft.JSInterop;

namespace RaidLoop.Client.Services;

public sealed class ClientTelemetryService : IClientTelemetryService
{
    private readonly IJSRuntime _jsRuntime;

    public ClientTelemetryService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask ReportErrorAsync(string message, object? details = null, CancellationToken cancellationToken = default)
    {
        return _jsRuntime.InvokeVoidAsync(
            "RaidLoopTelemetry.reportError",
            cancellationToken,
            message,
            details);
    }
}
