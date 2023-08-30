using System.Collections.Concurrent;
using System.Text.Json;
using OpenTelemetry.Proto.Resource.V1;
using OTLPView.DataModel;

namespace OTLPView;

public class TelemetryResults
{
    private readonly int MAX_OPERATION_COUNT;

    // Common
    private readonly ConcurrentDictionary<string, OtlpApplication> _applications = new();

    //Traces
    private readonly ConcurrentDictionary<string, TraceOperation> _operations = new();
    //Using a list to keep the order of operations, but need to be careful about concurrency
    private readonly List<TraceOperation> _operationStack = new();

    public ConcurrentBag<OtlpLogEntry> Logs { get; init; } = new();
    public ConcurrentDictionary<int, string> LogPropertyKeys { get; } = new();


    public IReadOnlyList<TraceOperation> Operations => (IReadOnlyList<TraceOperation>)_operationStack;
    public IReadOnlyDictionary<string, OtlpApplication> Applications => _applications;


    public TelemetryResults(IConfiguration config)
    {
        MAX_OPERATION_COUNT = config.GetValue<int>("MaxOperationCount", 1000);
    }

    internal TraceOperation GetOrAddOperation(string operationId)
    {
        if (!_operations.TryGetValue(operationId, out var operation))
        {
            lock (_operationStack)
            {
                if (_operations.Count >= MAX_OPERATION_COUNT)
                {
                    var dead_operation = _operationStack[MAX_OPERATION_COUNT - 1];
                    _operationStack.RemoveAt(MAX_OPERATION_COUNT - 1);
                    _operations.TryRemove(dead_operation.OperationId, out _);
                }
                operation = new TraceOperation() { OperationId = operationId };
                operation = _operations.GetOrAdd(operationId, operation);
                _operationStack.Insert(0, operation);
            }
        }
        return operation;
    }

    public OtlpApplication GetOrAddApplication(Resource resource)
    {
        if (resource == null)
        {
            return null;
        }
        var serviceId = resource.GetServiceId();
        return _applications.GetOrAdd(serviceId, _ => new OtlpApplication(resource, Applications));
    }

    #region JSON Serialization
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters =
        {
            new MetricValueJsonConverter()
        }
    };

    public string GetMetricJSON() => JsonSerializer.Serialize(Applications, _jsonOptions);

    public string GetTraceJSON() => JsonSerializer.Serialize(_operationStack, _jsonOptions);
    #endregion
}
