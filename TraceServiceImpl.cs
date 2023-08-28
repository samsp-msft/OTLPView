using Google.Protobuf.Collections;
using Grpc.Core;
using OpenTelemetry.Proto.Collector.Trace.V1;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using static OTLPView.TraceServiceImpl;
using Otel = OpenTelemetry.Proto.Trace.V1;

namespace OTLPView
{
    public class TraceServiceImpl : OpenTelemetry.Proto.Collector.Trace.V1.TraceService.TraceServiceBase
    {
        ILogger<TraceServiceImpl> _logger;
        TelemetryResults _telemetryResults;
        TracesPageState _pageState;

        public TraceServiceImpl(ILogger<TraceServiceImpl> logger, TelemetryResults telemetryResults, TracesPageState pageState)
        {
            _logger = logger;
            _telemetryResults = telemetryResults;
            _pageState = pageState;
        }

        public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
        {
            ProcessTraceGrpcResourceSpans(request.ResourceSpans);
            _pageState.DataChanged();

            var resp = new ExportTraceServiceResponse();
            resp.PartialSuccess = null;
            return Task.FromResult(resp);
        }


        private void ProcessTraceGrpcResourceSpans(RepeatedField<Otel.ResourceSpans> resourceSpans)
        {
            foreach (var r in resourceSpans)
            {
                // Store the trace source if we haven't seen it before
                var serviceName = r.Resource.Attributes.FindStringValueOrDefault("service.name", "Unknown");
                TraceSourceApplication traceSource = _telemetryResults.TraceSources.GetOrAdd(serviceName, _ =>
                {
                    var c = _telemetryResults.TraceSources.Count;

                    return new TraceSourceApplication()
                    {
                        ApplicationName = serviceName,
                        Properties = r.Resource.Attributes.ToDictionary(),
                        BarColors = Helpers.BarColors[c]
                    };
                });

                foreach (var ss in r.ScopeSpans)
                {
                    string scopeName = ss.Scope.Name;
                    TraceScope traceScope = traceSource.Scopes.GetOrAdd(scopeName, _ =>
                    {
                        var color = traceSource.BarColors[traceSource.Scopes.Count];
                        return new TraceScope()
                        {
                            ScopeName = scopeName,
                            Properties = ss.Scope.Attributes.ToDictionary(),
                            Version = ss.Scope.Version,
                            BarColor = color
                        };
                    });

                    foreach (var sp in ss.Spans)
                    {
                        var operationId = sp.TraceId.ToHexString();
                        var operation = _telemetryResults.GetOrAddOperation(operationId);
                        var span = new Span(sp, operation, traceSource, traceScope);
                    }
                }
            }
        }

    }

    /// <summary>
    /// Represents an Operation (Trace) that is composed of one or more Spans
    /// </summary>
    public class Operation
    {
        public string OperationId { get; init; }
        public ConcurrentDictionary<String, Span> RootSpans { get; } = new();
        [JsonIgnore]
        public ConcurrentDictionary<String, Span> AllSpans { get; } = new();

        public List<Span> UnParentedSpans => AllSpans.Values.Where(s => s.ParentSpanId is not null && s.ParentSpan == null).ToList();

        public DateTime StartTime => AllSpans.Values.Min(s => s.StartTime);
        public DateTime EndTime => AllSpans.Values.Max(s => s.EndTime);

        public double Duration => (EndTime - StartTime).TotalMilliseconds;

    }

    /// <summary>
    /// Represents a Span within an Operation (Trace)
    /// </summary>
    public class Span
    {
        public string OperationId { get; init; }
        [JsonIgnore]
        public Operation Operation { get; init; }
        [JsonIgnore]
        public TraceScope TraceScope { get; init; }
        [JsonIgnore]
        public TraceSourceApplication Source { get; init; }
        public string SpanId { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ParentSpanId { get; init; }
        [JsonIgnore]
        public Span ParentSpan { get; set; }
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
        public ConcurrentBag<Span> ChildSpans { get; } = new();

        public string ScopeName => TraceScope.ScopeName;
        public string ScopeSource => Source.ApplicationName;
        public TimeSpan Duration => EndTime - StartTime;
        public Span RootSpan => ParentSpan is null ? this : ParentSpan.RootSpan;

        public bool NotParented => (ParentSpanId is not null && ParentSpan is null);

        public Span(Otel.Span s, Operation operation, TraceSourceApplication traceSource, TraceScope scope)
        {
            this.OperationId = operation.OperationId;
            this.SpanId = s.SpanId?.ToHexString();
            if (s.SpanId is null)
            {
                throw new ArgumentException("Span has no SpanId");
            }
            this.ParentSpanId = s.ParentSpanId?.ToHexString();
            this.Operation = operation;
            this.Source = traceSource;
            this.TraceScope = scope;
            this.Name = s.Name;
            this.Kind = s.Kind.ToString();
            this.StartTime = Helpers.UnixNanoSecondsToDateTime(s.StartTimeUnixNano);
            this.EndTime = Helpers.UnixNanoSecondsToDateTime(s.EndTimeUnixNano);
            this.Status = s.Status?.ToString();
            this.Attributes = s.Attributes.ToDictionary();
            this.State = s.TraceState;

            operation.AllSpans.TryAdd(SpanId, this);

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
            if (string.IsNullOrEmpty(ParentSpanId))
            {
                operation.RootSpans.TryAdd(SpanId, this);
            }
            else
            {
                Span parentSpan;
                if (operation.AllSpans.TryGetValue(ParentSpanId, out parentSpan))
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
        public double TimeOffset(Span span) => (Time - span.StartTime).TotalMilliseconds;
    }

    public class TraceSourceApplication
    {
        public string ApplicationName { get; init; }
        [JsonIgnore]
        public Dictionary<String, String> Properties { get; init; }
        [JsonIgnore]
        public ConcurrentDictionary<string, TraceScope> Scopes { get; } = new();
        public string[] BarColors { get; init; }

        public string ServiceProperties => Properties.ConcatString();
    

    }

    /// <summary>
    /// The Scope of a TraceSource, maps to the name of the ActivitySource in .NET
    /// </summary>
    public class TraceScope
    {
        public string ScopeName { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Version { get; init; }
        [JsonIgnore]
        public Dictionary<String, String> Properties { get; init; }
        public string ServiceProperties => Properties.ConcatString();
        public string BarColor { get; init; }
    }









}
