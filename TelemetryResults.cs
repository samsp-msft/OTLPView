using System.Collections.Concurrent;
using System.Text.Json;

namespace OTLPView
{
    public class TelemetryResults
    {
        private readonly int MAX_OPERATION_COUNT;

        public ConcurrentDictionary<string, ServiceMetrics> ServiceMetrics = new();

        private ConcurrentDictionary<string, Operation> _operations = new();
        private List<Operation> _operationStack = new();
        public ConcurrentDictionary<string, TraceSourceApplication> TraceSources = new();

        public ConcurrentDictionary<string, LogApplication> LogApplications = new();
        public ConcurrentBag<OtlpLogEntry> Logs { get; init; } = new();
        public ConcurrentDictionary<int, string> LogPropertyKeys { get; } = new();


        public IReadOnlyList<Operation> Operations => (IReadOnlyList<Operation>)_operationStack;
        

        public TelemetryResults(IConfiguration config)
        {
           MAX_OPERATION_COUNT = config.GetValue<int>("MaxOperationCount",1000);
        }

        internal Operation GetOrAddOperation(string operationId)
        {
            Operation operation;
            if (!_operations.TryGetValue(operationId, out operation))
            {
                lock (_operationStack)
                {
                    if (_operations.Count >= MAX_OPERATION_COUNT)
                    {
                        var dead_operation = _operationStack[MAX_OPERATION_COUNT - 1];
                        _operationStack.RemoveAt(MAX_OPERATION_COUNT -1);
                        _operations.TryRemove(dead_operation.OperationId, out _);
                    }
                    operation = new Operation() { OperationId = operationId };
                    operation = _operations.GetOrAdd(operationId, operation);
                    _operationStack.Insert(0, operation);
                }
            }
            return operation;
        }

        #region JSON Serialization
        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new MetricValueJsonConverter()
            }
        };

        public string GetMetricJSON()
        {
            return JsonSerializer.Serialize(ServiceMetrics, _jsonOptions);
        }

        public string GetTraceJSON()
        {
            return JsonSerializer.Serialize(_operationStack, _jsonOptions);
        }
        #endregion
    }
}
