using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace OTLPView
{
    public class MetricsServiceImpl : OpenTelemetry.Proto.Collector.Metrics.V1.MetricsService.MetricsServiceBase
    {
        ILogger<MetricsServiceImpl> _logger;
        TelemetryResults _telemetryResults;
        MetricsPageState _pageState;
        public MetricsServiceImpl(ILogger<MetricsServiceImpl> logger, TelemetryResults telemetryResults, MetricsPageState state)
        {
            _logger = logger;
            _telemetryResults = telemetryResults;
            _pageState = state;
        }

        public override Task<OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsServiceResponse> Export(OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsServiceRequest request, ServerCallContext context)
        {
            ProcessGrpcResourceMetrics(request.ResourceMetrics);
            _pageState.DataChanged();

            var resp = new ExportMetricsServiceResponse();
            resp.PartialSuccess = null;

            return Task.FromResult(resp);
        }

        private void ProcessGrpcResourceMetrics(RepeatedField<ResourceMetrics> resourceMetrics)
        {
            foreach (var rm in resourceMetrics)
            {
                var serviceName = rm.Resource.Attributes.FindStringValueOrDefault("service.name", "Unknown");
                var serviceMetrics = _telemetryResults.ServiceMetrics.GetOrAdd(serviceName, _ => new ServiceMetrics()
                {
                    ServiceName = serviceName,
                    Properties = rm.Resource.Attributes.ToDictionary()
                });

                foreach (var m in rm.ScopeMetrics)
                {
                    var meterResults = serviceMetrics.Meters.GetOrAdd(m.Scope.Name, _ => new MeterResult(m.Scope));

                    foreach (var mData in m.Metrics)
                    {
                        meterResults.ProcessGrpcMetricData(mData);
                    }
                }
            }
        }
    }

    public class ServiceMetrics
    {
        public string ServiceName { get; init; }
        public Dictionary<String, String> Properties { get; init; }
        public ConcurrentDictionary<String, MeterResult> Meters { get; } = new();

        public string ServiceDescription => Properties.ConcatString();
    }

    public class MeterResult
    {
        public string? MeterName { get; init; }
        public string Version { get; init; }
        public Dictionary<String, String> Properties { get; init; }
        public ConcurrentDictionary<String, Counter> Counters { get; } = new();

        public MeterResult(InstrumentationScope scope)
        {
            MeterName = scope.Name;
            Version = scope.Version;
            Properties = scope.Attributes.ToDictionary();
        }

        public void ProcessGrpcMetricData(Metric mData)
        {
            var counter = Counters.GetOrAdd(mData.Name, _ => new Counter(mData));
            counter.AddCounterValuesFromGrpc(mData);
        }
    }

    public class Counter
    {
        public string CounterName { get; init; }
        public string CounterDescription { get; init; }
        public string CounterUnit { get; init; }
        public Metric.DataOneofCase CounterType { get; init; }
        public ConcurrentDictionary<int, DimensionScope> Dimensions { get; } = new();

        public Counter(Metric mData)
        {
            CounterName = mData.Name;
            CounterDescription = mData.Description;
            CounterUnit = mData.Unit;
            CounterType = mData.DataCase;
        }

        public void AddCounterValuesFromGrpc(Metric mData)
        {
            switch (mData.DataCase)
            {
                case Metric.DataOneofCase.Gauge:
                    foreach (var d in mData.Gauge.DataPoints)
                    {
                        FindScope(d.Attributes).AddPointValue(d);
                    }
                    break;
                case Metric.DataOneofCase.Sum:
                    foreach (var d in mData.Sum.DataPoints)
                    {
                        FindScope(d.Attributes).AddPointValue(d);
                    }
                    break;
                case Metric.DataOneofCase.Histogram:
                    foreach (var d in mData.Histogram.DataPoints)
                    {
                        FindScope(d.Attributes).AddHistogramValue(d);
                    }
                    break;
            }
        }

        private DimensionScope FindScope(RepeatedField<KeyValue> attributes)
        {
            var key = CreateDimensionHashcode(attributes);
            return Dimensions.GetOrAdd(key, _ => new DimensionScope(key, attributes));
        }

        /// <summary>
        /// Creates a hascode for a dimension based on the dimension key/value pairs.
        /// </summary>
        /// <param name="keyvalues">The keyvalue pairs of the dimension</param>
        /// <returns>A hashcode</returns>
        public static int CreateDimensionHashcode(RepeatedField<KeyValue> keyvalues)
        {
            if (keyvalues is null || keyvalues.Count == 0)
            {
                return 0;
            }
            var dict = keyvalues.ToDictionary();

            var hash = new HashCode();
            foreach (var kv in dict.OrderBy(x => x.Key))
            {
                hash.Add(kv.Key);
                hash.Add(kv.Value);
            }
            return hash.ToHashCode();
        }
    }

    public class DimensionScope
    {
        public string Name { get; init; }
        public int Key { get; init; }
        public Dictionary<string, string> Dimensions { get; init; }
        public ConcurrentCappedCache<MetricValueBase> values { get; } = new(20);
        private MetricValueBase _lastValue;

        public DimensionScope(int key, RepeatedField<KeyValue> keyvalues)
        {
            Dimensions = keyvalues.ToDictionary();
            var name = Dimensions.ConcatString();
            Name = (name != null && name.Length > 0) ? name : "no-dimensions";
        }

        public void AddPointValue(NumberDataPoint d)
        {
            var start = Helpers.UnixNanoSecondsToDateTime(d.StartTimeUnixNano);
            var end = Helpers.UnixNanoSecondsToDateTime(d.TimeUnixNano);
            if (d.ValueCase == NumberDataPoint.ValueOneofCase.AsInt)
            {
               lock (this)
                {
                    if (_lastValue is not null && (_lastValue as MetricValue<long>).Value == d.AsInt)
                    {
                        (_lastValue as MetricValue<long>).End = end;
                    }
                    else
                    {
                        _lastValue = new MetricValue<long>(d.AsInt, start, end);
                        values.Add(_lastValue);
                    }
                }
            }
            else if (d.ValueCase == NumberDataPoint.ValueOneofCase.AsDouble)
            {
                lock (this)
                {
                    if (_lastValue is not null && (_lastValue as MetricValue<double>).Value == d.AsDouble)
                    {
                        (_lastValue as MetricValue<double>).End = end;
                    }
                    else
                    {
                        _lastValue = new MetricValue<double>(d.AsDouble, start, end);
                        values.Add(_lastValue);
                    }
                }
            }
        }

        public void AddHistogramValue(HistogramDataPoint h)
        {
            var start = Helpers.UnixNanoSecondsToDateTime(h.StartTimeUnixNano);
            var end = Helpers.UnixNanoSecondsToDateTime(h.TimeUnixNano);

            lock (this)
            {
                if (_lastValue is not null && (_lastValue as HistogramValue).Count == h.Count)
                {
                    (_lastValue as HistogramValue).End = end;
                }
                else
                {
                    _lastValue = new HistogramValue(h.BucketCounts, h.Sum, h.Count, start, end);
                    values.Add(_lastValue);
                }
            }
        }
    }

    public abstract class MetricValueBase
    {
        public readonly DateTime Start;
        public DateTime End;

        protected MetricValueBase(DateTime start, DateTime end)
        {
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
        }
    }

    public class MetricValue<T> : MetricValueBase
    {
        public readonly T? Value;
        public MetricValue(T value, DateTime start, DateTime end) : base(start, end)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class HistogramValue : MetricValueBase
    {
        public ulong[] Values { get; init; }
        public double Sum { get; init; }
        public ulong Count { get; init; }
        public HistogramValue(IList<ulong> values, double sum, ulong count, DateTime start, DateTime end) : base(start, end)
        {
            Values = values?.ToArray();
            Sum = sum;
            Count = count;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            bool first = true;
            sb.Append($"Count:{Count} Sum:{Sum} Values:");
            foreach (var v in Values)
            {
                if (!first)
                {
                    sb.Append(" ");
                }
                first = false;
                sb.Append($"{v}");
            }
            return sb.ToString();
        }
    }

    public class MetricValueJsonConverter : JsonConverter<MetricValueBase>
    {
        public override MetricValueBase Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            MetricValueBase metric,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(metric.ToString());
    }


}
