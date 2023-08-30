namespace OTLPView.Services;

public class DefaultLogsService : LogsService.LogsServiceBase
{
    private readonly ILogger<DefaultLogsService> _logger;
    private readonly TelemetryResults _telemetryResults;
    private readonly LogsPageState _pageState;

    public DefaultLogsService(ILogger<DefaultLogsService> logger, TelemetryResults telemetryResults, LogsPageState state)
    {
        _logger = logger;
        _telemetryResults = telemetryResults;
        _pageState = state;
    }

    public override async Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
    {
        ProcessGrpcResourceLogs(request.ResourceLogs);
        await _pageState.DataChanged();

        var resp = new ExportLogsServiceResponse
        {
            PartialSuccess = null
        };

        return resp;
    }

    private void ProcessGrpcResourceLogs(RepeatedField<ResourceLogs> resourceLogs)
    {
        foreach (var rl in resourceLogs)
        {
            var logApp = _telemetryResults.GetOrAddApplication(rl.Resource);
            foreach (var log in rl.ScopeLogs)
            {
                if (log.Scope is not null || !string.IsNullOrEmpty(log.SchemaUrl))
                {
                    // TODO: Handle this, but I don't know what the data looks like yet
                    Debugger.Break();
                }
                foreach (var record in log.LogRecords)
                {
                    var logEntry = new OtlpLogEntry(record, logApp);
                    _telemetryResults.Logs.Add(logEntry);
                    foreach (var key in logEntry.Properties.Keys)
                    {
                        _telemetryResults.LogPropertyKeys.GetOrAdd(key.GetHashCode(), key);
                    }
                }
            }
        }
    }
}
