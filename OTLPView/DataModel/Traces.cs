using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using OTLPView.Extensions;

namespace OTLPView.DataModel;


/// <summary>
/// Represents an Operation (Trace) that is composed of one or more Spans
/// </summary>
public class TraceOperation
{
    public string OperationId { get; init; }

    private readonly ConcurrentDictionary<string, TraceSpan> _rootSpans = new();
    public IReadOnlyDictionary<string, TraceSpan> RootSpans => _rootSpans;

    [JsonIgnore]
    private readonly ConcurrentDictionary<string, TraceSpan> _allSpans = new();
    public IReadOnlyDictionary<string, TraceSpan> AllSpans => _allSpans;

    public IReadOnlyList<TraceSpan> UnParentedSpans => AllSpans.Values.Where(s => s.ParentSpanId is not null && s.ParentSpan == null).ToList();

    public DateTime StartTime => AllSpans.Values.Min(s => s.StartTime);
    public DateTime EndTime => AllSpans.Values.Max(s => s.EndTime);

    public double Duration => (EndTime - StartTime).TotalMilliseconds;

    public void AddSpan(TraceSpan span)
    {
        _allSpans.TryAdd(span.SpanId, span);
        if (string.IsNullOrEmpty(span.ParentSpanId))
        {
            _rootSpans.TryAdd(span.SpanId, span);
        }
    }
}

/// <summary>
/// Represents a Span within an Operation (Trace)
/// </summary>
public class TraceSpan
{
    public string OperationId { get; init; }
    [JsonIgnore]
    public TraceOperation Operation { get; init; }
    [JsonIgnore]
    public TraceScope TraceScope { get; init; }
    [JsonIgnore]
    public OtlpApplication Source { get; init; }
    public string SpanId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ParentSpanId { get; init; }
    [JsonIgnore]
    public TraceSpan ParentSpan { get; set; }
    public string Name { get; init; }
    public string Kind { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Status { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string State { get; init; }
    [JsonIgnore]
    public Dictionary<string, string> Attributes { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SpanEvent> Events { get; } = new();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string> Links { get; } = new();
    public ConcurrentBag<TraceSpan> ChildSpans { get; } = new();

    public string ScopeName => TraceScope.ScopeName;
    public string ScopeSource => Source.ApplicationName;
    public TimeSpan Duration => EndTime - StartTime;
    public TraceSpan RootSpan => ParentSpan is null ? this : ParentSpan.RootSpan;

    public bool NotParented => ParentSpanId is not null && ParentSpan is null;

    public TraceSpan(Otel.Span s, TraceOperation operation, OtlpApplication traceSource, TraceScope scope)
    {
        OperationId = operation.OperationId;
        SpanId = s.SpanId?.ToHexString();
        if (s.SpanId is null)
        {
            throw new ArgumentException("Span has no SpanId");
        }
        ParentSpanId = s.ParentSpanId?.ToHexString();
        Operation = operation;
        Source = traceSource;
        TraceScope = scope;
        Name = s.Name;
        Kind = s.Kind.ToString();
        StartTime = Helpers.UnixNanoSecondsToDateTime(s.StartTimeUnixNano);
        EndTime = Helpers.UnixNanoSecondsToDateTime(s.EndTimeUnixNano);
        Status = s.Status?.ToString();
        Attributes = s.Attributes.ToDictionary();
        State = s.TraceState;

        operation.AddSpan(this);

        // Find any events and add them
        foreach (var e in s.Events)
        {
            Events.Add(new SpanEvent()
            {
                Name = e.Name,
                Time = Helpers.UnixNanoSecondsToDateTime(e.TimeUnixNano),
                Attributes = e.Attributes.ToDictionary()
            });
        }

        //Find the parent span and add this as a child
        if (!string.IsNullOrEmpty(ParentSpanId))
        {
            if (operation.AllSpans.TryGetValue(ParentSpanId, out var parentSpan))
            {
                ParentSpan = parentSpan;
                parentSpan.ChildSpans.Add(this);
            }
        }

        // Find child spans and add them as children
        foreach (var childSpan in operation.AllSpans.Values.Where(x => x.ParentSpanId == SpanId))
        {
            childSpan.ParentSpan = this;
            ChildSpans.Add(childSpan);
        }
    }
}

public class SpanEvent
{
    public string Name { get; init; }
    public DateTime Time { get; init; }
    public Dictionary<string, string> Attributes { get; init; }
    public double TimeOffset(TraceSpan span) => (Time - span.StartTime).TotalMilliseconds;
}

/// <summary>
/// The Scope of a TraceSource, maps to the name of the ActivitySource in .NET
/// </summary>
public class TraceScope
{
    public string ScopeName { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Version { get; init; }

    private Dictionary<string, string> _properties { get; init; }


    [JsonIgnore]
    public IReadOnlyDictionary<string, string> Properties => _properties;
    public string ServiceProperties => Properties.ConcatString();
    public string BarColor { get; init; }

    public TraceScope(InstrumentationScope scope, string color)
    {
        ScopeName = scope.Name;

        _properties = scope.Attributes.ToDictionary();
        Version = scope.Version;
        BarColor = color;
    }
}
