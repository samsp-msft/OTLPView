using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using OpenTelemetry.Proto.Common.V1;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using System.Diagnostics;
using OTLPView.DataModel;

namespace OTLPView
{
    public class LogsServiceImpl : LogsService.LogsServiceBase
    {
        ILogger<LogsServiceImpl> _logger;
        TelemetryResults _telemetryResults;
        LogsPageState _pageState;
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
                ProcessGrpcResourceLog(rl, logApp);
            }
        }

        private void ProcessGrpcResourceLog(ResourceLogs rl, OtlpApplication logApp)
        {
            foreach (var log in rl.ScopeLogs)
            {
                if (log.Scope is not null || !string.IsNullOrEmpty(log.SchemaUrl))
                {
                    Debugger.Break();
                }
                foreach (var record in log.LogRecords)
                {
                    var logentry = new OtlpLogEntry(record, logApp);
                    _telemetryResults.Logs.Add(logentry);
                    foreach (var key in logentry.Properties.Keys)
                    {
                        _telemetryResults.LogPropertyKeys.GetOrAdd(key.GetHashCode(), key);
                    }
                }
            }
        }
    }

    public class OtlpLogEntry
    {
        public Dictionary<string, string> Properties { get; init; }
        public DateTime TimeStamp { get; init; }
        public uint flags { get; init; }
        public Microsoft.Extensions.Logging.LogLevel Severity { get; init; }
        public string Message { get; init; }
        public string SpanId { get; init; }
        public string TraceId { get; init; }
        public string ParentId { get; init; }
        public string OriginalFormat { get; init; }
        public OtlpApplication Application { get; init; }

        public OtlpLogEntry(LogRecord record, OtlpApplication logApp)
        {
            var properties = new Dictionary<string, string>();
            foreach (var kv in record.Attributes)
            {
                switch (kv.Key)
                {
                    case "{OriginalFormat}": OriginalFormat = kv.Value.ValueString(); break;
                    case "ParentId": ParentId = kv.Value.ValueString(); break;
                    case "SpanId":
                    case "TraceId":
                        // Explicitly ignore these
                        break;
                    default:
                        properties.TryAdd(kv.Key, kv.Value.ValueString());
                        break;
                }
            }
            Properties = properties;

            TimeStamp = Helpers.UnixNanoSecondsToDateTime(record.TimeUnixNano);
            flags = record.Flags;
            //Severity = switch (record.SeverityNumber)
            //{
            //    SeverityNumber.Trace => LogLevel.Trace,
            //    SeverityNumber.Debug => LogLevel.Debug,
            //    SeverityNumber.Info => LogLevel.Information,
            //    SeverityNumber.Warn => LogLevel.Warning,
            //    SeverityNumber.Error => LogLevel.Error,
            //    SeverityNumber.Critical => LogLevel.Critical,
            //    _ => LogLevel.None
            //};

            Message = record.Body.ValueString();
            SpanId = record.SpanId.ToHexString();
            TraceId = record.TraceId.ToHexString();
            Application = logApp;
        }
    }

}
