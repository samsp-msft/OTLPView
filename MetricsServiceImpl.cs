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
using OpenTelemetry.Proto.Resource.V1;
using OTLPView.DataModel;

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




}
