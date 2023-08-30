using System.Diagnostics;
using Google.Protobuf.Collections;
using Grpc.Core;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using OTLPView.DataModel;

namespace OTLPView
{
    public class LogsServiceImpl : LogsService.LogsServiceBase
    {
        private readonly ILogger<LogsServiceImpl> _logger;
        private readonly TelemetryResults _telemetryResults;
        private readonly LogsPageState _pageState;
        public LogsServiceImpl(ILogger<LogsServiceImpl> logger, TelemetryResults telemetryResults, LogsPageState state)
        {
            _logger = logger;
            _telemetryResults = telemetryResults;
            _pageState = state;
        }

        public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
        {
            ProcessGrpcResourceLogs(request.ResourceLogs);
            _pageState.DataChanged();

            var resp = new ExportLogsServiceResponse();
            resp.PartialSuccess = null;

            return Task.FromResult(resp);
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



}
