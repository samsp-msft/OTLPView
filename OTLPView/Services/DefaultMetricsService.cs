namespace OTLPView.Services;

public class DefaultMetricsService : MetricsService.MetricsServiceBase
{
    private readonly ILogger<DefaultMetricsService> _logger;
    private readonly TelemetryResults _telemetryResults;
    private readonly MetricsPageState _pageState;
    public DefaultMetricsService(ILogger<DefaultMetricsService> logger, TelemetryResults telemetryResults, MetricsPageState state)
    {
        _logger = logger;
        _telemetryResults = telemetryResults;
        _pageState = state;
    }

    public override Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
    {
        ProcessGrpcResourceMetrics(request.ResourceMetrics);
        _pageState.DataChanged();

        var resp = new ExportMetricsServiceResponse
        {
            PartialSuccess = null
        };

        return Task.FromResult(resp);
    }

    private void ProcessGrpcResourceMetrics(RepeatedField<ResourceMetrics> resourceMetrics)
    {
        foreach (var rm in resourceMetrics)
        {
            var serviceMetrics = _telemetryResults.GetOrAddApplication(rm.Resource);

            foreach (var m in rm.ScopeMetrics)
            {
                var meterResults = serviceMetrics.GetOrAddMeter(m.Scope.Name, _ => new MeterResult(m.Scope));

                foreach (var mData in m.Metrics)
                {
                    meterResults.ProcessGrpcMetricData(mData);
                }
            }
        }
    }
}
