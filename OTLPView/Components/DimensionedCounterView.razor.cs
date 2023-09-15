using Microsoft.JSInterop;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OTLPView.Components;

public sealed partial class DimensionedCounterView
{
    static int lastId = 0;

    // Define the size of the graph based on the number of points and the duration of each point
    private const int GRAPH_POINT_COUNT = 18; // 3 minutes
    private const int GRAPH_POINT_SIZE = 10; // 10s

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private DimensionScope _dimension;
    private readonly int _instanceID = ++lastId;

    private double[] _chartLabels;
    private double[] _chartValues;

    [Parameter, EditorRequired]
    public required DimensionScope Dimension
    {
        get => _dimension;
        set
        {
            _dimension = value;
            _chartValues = CalcChartValues(_dimension, GRAPH_POINT_COUNT, GRAPH_POINT_SIZE);
        }
    }

    [Parameter, EditorRequired]
    public required Counter Counter { get; set; }

    private string chartDivId => $"lineChart{_instanceID}";

    protected override void OnInitialized()
    {
        _chartLabels = _CalcLabels(GRAPH_POINT_COUNT, GRAPH_POINT_SIZE);
    }

    private double[] _CalcLabels(int pointCount, int pointSize)
    {
        var duration = pointSize * pointCount;
        var labels = new double[pointCount];
        for (var i = 0; i < pointCount; i++)
        {
            labels[i] = (pointSize * (i + 1)) - duration;
        }
        return labels;
    }

    // Graph is not based on x,y coordinates, but rather a series of data points with a value
    // Each point in the graph is the max value of all the values in that point's time range
    private double[] CalcChartValues(DimensionScope dimension, int pointCount, int pointSize)
    {
        var duration = pointSize * pointCount;
        var values = new double[pointCount];
        var now = DateTime.UtcNow;
        foreach (var point in dimension.Values)
        {
            var start = CalcOffset(now - point.Start, pointCount, pointSize);
            var end = CalcOffset(now - point.End, pointCount, pointSize);
            if (start is not null && end is not null)
            {
                for (var i = start.GetValueOrDefault(0); i <= end.GetValueOrDefault(pointCount - 1); i++)
                {
                    values[i] = point switch
                    {
                        MetricValue<long> @longMetric => Math.Max(@longMetric.Value, values[i]),
                        MetricValue<double> doubleMetric => Math.Max(doubleMetric.Value, values[i]),
                        _ => values[i]
                    };
                }
            }
        }
        //Console.WriteLine($"Values: {string.Join(",", values)}");
        return values;
    }

    // Calculate the offset of each metric value in the graph based on the current time
    private int? CalcOffset(TimeSpan difference, int pointCount, int pointSize)
    {

        var offset = (pointCount - 1) - (int)Math.Floor(difference.TotalSeconds / pointSize);
        return (offset >= 0 && offset < pointCount) ? offset : null;
    }

    private double[] CalcFakeValues(int pointCount)
    {
        var values = new double[pointCount];
        var rnd = Random.Shared;
        for (var i = 0; i < pointCount; i++)
        {
            values[i] = rnd.NextDouble() * 100;
        }
        return values;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var data = new[]{ new {
              x= _chartLabels,
              y= _chartValues,
              type= "scatter"
            } };

        var layout = new {
            title = Counter?.CounterName ?? "unknown",
            showlegend = false,
            xaxis = new {
                title = "Time (s)"
            }
        };
        var options = new { staticPlot = true };

        await JSRuntime.InvokeVoidAsync("Plotly.newPlot", chartDivId, data, layout, options);
    }
}
