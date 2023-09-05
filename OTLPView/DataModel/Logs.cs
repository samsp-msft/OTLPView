using Microsoft.AspNetCore.Authentication;

namespace OTLPView.DataModel;

public class OtlpLogEntry
{
    public Dictionary<string, string> Properties { get; init; }
    public DateTime TimeStamp { get; init; }
    public uint Flags { get; init; }
    public LogLevel Severity { get; init; }
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
        Flags = record.Flags;
        Severity = MapSeverity(record.SeverityNumber);

        Message = record.Body.ValueString();
        SpanId = record.SpanId.ToHexString();
        TraceId = record.TraceId.ToHexString();
        Application = logApp;
    }

    private LogLevel MapSeverity(SeverityNumber severityNumber) => severityNumber switch
    {
        SeverityNumber.Trace => LogLevel.Trace,
        SeverityNumber.Trace2 => LogLevel.Trace,
        SeverityNumber.Trace3 => LogLevel.Trace,
        SeverityNumber.Trace4 => LogLevel.Trace,
        SeverityNumber.Debug => LogLevel.Debug,
        SeverityNumber.Debug2 => LogLevel.Debug,
        SeverityNumber.Debug3 => LogLevel.Debug,
        SeverityNumber.Debug4 => LogLevel.Debug,
        SeverityNumber.Info => LogLevel.Information,
        SeverityNumber.Info2 => LogLevel.Information,
        SeverityNumber.Info3 => LogLevel.Information,
        SeverityNumber.Info4 => LogLevel.Information,
        SeverityNumber.Warn => LogLevel.Warning,
        SeverityNumber.Warn2 => LogLevel.Warning,
        SeverityNumber.Warn3 => LogLevel.Warning,
        SeverityNumber.Warn4 => LogLevel.Warning,
        SeverityNumber.Error => LogLevel.Error,
        SeverityNumber.Error2 => LogLevel.Error,
        SeverityNumber.Error3 => LogLevel.Error,
        SeverityNumber.Error4 => LogLevel.Error,
        SeverityNumber.Fatal => LogLevel.Critical,
        SeverityNumber.Fatal2 => LogLevel.Critical,
        SeverityNumber.Fatal3 => LogLevel.Critical,
        SeverityNumber.Fatal4 => LogLevel.Critical,
        _ => LogLevel.None
    };

    public Dictionary<string, string> AllProperties()
    {
        var props = new Dictionary<string, string>();
        props.Add("Application", Application.UniqueApplicationName);
        props.Add("Flags", Message);
        props.Add("Severity", Severity.ToString());
        props.Add("TraceId", TraceId);
        props.Add("SpanId", SpanId);
        props.Add("ParentId", ParentId);
        props.Add("OriginalFormat", OriginalFormat);

        foreach (var kv in Properties) { props.Add(kv.Key, kv.Value); }

        return props;
    }
}

