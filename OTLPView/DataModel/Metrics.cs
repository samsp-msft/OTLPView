using System.Text.Json;
using System.Text.Json.Serialization;

namespace OTLPView;

//public class MetricsApplication
//{
//    public OtlpApplication OtlpApplication { get; init; }

//    public string ApplicationName => OtlpApplication.ApplicationName;
//    public IReadOnlyDictionary<string, string> Properties => OtlpApplication.Properties;
//    public ConcurrentDictionary<string, MeterResult> Meters { get; } = new();

//    public MetricsApplication(OtlpApplication app)
//    {
//        OtlpApplication = app;
//    }

//}

public class MeterResult
{
    public string? MeterName { get; init; }
    public string Version { get; init; }
    private readonly Dictionary<string, string> _properties = new();
    private readonly ConcurrentDictionary<string, Counter> _counters = new();

    public IReadOnlyDictionary<string, string> Properties => _properties;
    public IReadOnlyDictionary<string, Counter> Counters => _counters;

    public MeterResult(InstrumentationScope scope)
    {
        MeterName = scope.Name;
        Version = scope.Version;
        _properties = scope.Attributes.ToDictionary();
    }

    public void ProcessGrpcMetricData(Metric mData)
    {
        var counter = _counters.GetOrAdd(mData.Name, _ => new Counter(mData, this));
        counter.AddCounterValuesFromGrpc(mData);
    }
}

public class Counter
{
    private readonly ConcurrentDictionary<int, DimensionScope> _dimensions = new();
    public string CounterName { get; init; }
    public string CounterDescription { get; init; }
    public string CounterUnit { get; init; }
    public Metric.DataOneofCase CounterType { get; init; }
    public MeterResult Parent { get; init; }

    public IReadOnlyDictionary<int, DimensionScope> Dimensions => _dimensions;

    public Counter(Metric mData, MeterResult parent)
    {
        CounterName = mData.Name;
        CounterDescription = mData.Description;
        CounterUnit = mData.Unit;
        CounterType = mData.DataCase;
        Parent = parent;
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
        var key = CalculateDimensionHashcode(attributes);
        return _dimensions.GetOrAdd(key, _ => new DimensionScope(key, attributes));
    }

    /// <summary>
    /// Creates a hashcode for a dimension based on the dimension key/value pairs.
    /// </summary>
    /// <param name="keyvalues">The keyvalue pairs of the dimension</param>
    /// <returns>A hashcode</returns>
    private static int CalculateDimensionHashcode(RepeatedField<KeyValue> keyvalues)
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

    private Dictionary<string, string> _dimensions { get; init; }
    //public readonly ConcurrentCappedCache<MetricValueBase> _values = new(256);
    public readonly ConcurrentBag<MetricValueBase> _values = new();
    // Used to aid in merging values that are the same in a concurrent environment
    private MetricValueBase _lastValue;

    public IEnumerable<MetricValueBase> Values => _values;
    public IReadOnlyDictionary<string, string> Dimensions => _dimensions;

    public bool IsHistogram => (_values.FirstOrDefault() is HistogramValue);

    public DimensionScope(int key, RepeatedField<KeyValue> keyvalues)
    {
        _dimensions = keyvalues.ToDictionary();
        var name = _dimensions.ConcatString();
        Name = (name != null && name.Length > 0) ? name : "no-dimensions";
        Key = key;
    }

    /// <summary>
    /// Compares and updates the timespan for metrics if they are unchanged.
    /// </summary>
    /// <param name="d">Metric value to merge</param>
    public void AddPointValue(NumberDataPoint d)
    {
        var start = Helpers.UnixNanoSecondsToDateTime(d.StartTimeUnixNano);
        var end = Helpers.UnixNanoSecondsToDateTime(d.TimeUnixNano);
        Console.WriteLine($"{start.ToLocalTime().ToLongTimeString()} - {end.ToLocalTime().ToLongTimeString()} - {d.ValueCase} - {d.AsInt} - {d.AsDouble}");
        if (d.ValueCase == NumberDataPoint.ValueOneofCase.AsInt)
        {
            var value = d.AsInt;
            lock (this)
            {
                var lastLongValue = _lastValue as MetricValue<long>;
                if (lastLongValue is not null && lastLongValue.Value == value)
                {
                    lastLongValue.End = end;
                    Interlocked.Increment(ref lastLongValue.Count);
                }
                else
                {
                    if (lastLongValue is not null)
                    {
                        start = lastLongValue.End;
                    }
                    _lastValue = new MetricValue<long>(d.AsInt, start, end);
                    _values.Add(_lastValue);
                }
            }
        }
        else if (d.ValueCase == NumberDataPoint.ValueOneofCase.AsDouble)
        {
            var value = d.AsDouble;
            lock (this)
            {
                var lastDoubleValue = _lastValue as MetricValue<double>;
                if (lastDoubleValue is not null && lastDoubleValue.Value == d.AsDouble)
                {
                    lastDoubleValue.End = end;
                    Interlocked.Increment(ref lastDoubleValue.Count);
                }
                else
                {
                    if (lastDoubleValue is not null)
                    {
                        start = lastDoubleValue.End;
                    }
                    _lastValue = new MetricValue<double>(d.AsDouble, start, end);
                    _values.Add(_lastValue);
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
            var lastHistogramValue = _lastValue as HistogramValue;
            if (lastHistogramValue is not null && lastHistogramValue.Count == h.Count)
            {
                lastHistogramValue.End = end;
            }
            else
            {
                _lastValue = new HistogramValue(h.BucketCounts, h.Sum, h.Count, start, end);
                _values.Add(_lastValue);
            }
        }
    }
}

public abstract class MetricValueBase
{
    public readonly DateTime Start;
    public DateTime End { get; set; }
    public ulong Count = 1;

    protected MetricValueBase(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }
}

public class MetricValue<T> : MetricValueBase
{
    public readonly T? Value;
    public MetricValue(T value, DateTime start, DateTime end) : base(start, end)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString();
}

public class HistogramValue : MetricValueBase
{
    public ulong[] Values { get; init; }
    public double Sum { get; init; }

    public HistogramValue(IList<ulong> values, double sum, ulong count, DateTime start, DateTime end) : base(start, end)
    {
        Values = values?.ToArray();
        Sum = sum;
        Count = count;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var first = true;
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
        JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(
        Utf8JsonWriter writer,
        MetricValueBase metric,
        JsonSerializerOptions options) =>
            writer.WriteStringValue(metric.ToString());
}
