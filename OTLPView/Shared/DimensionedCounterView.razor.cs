namespace OTLPView.Shared;

public sealed partial class DimensionedCounterView
{
    private DimensionScope _dimension;
    private string[] chartLabels;
    private List<ChartSeries> chartValues;

    [Parameter, EditorRequired]
    public required DimensionScope Dimension
    {
        get => _dimension;
        set
        {
            _dimension = value;
            chartValues = new List<ChartSeries>()
            {
                new ChartSeries()
                {
                    Name = Counter?.CounterName ?? "unknown",
                    Data = CalcChartValues(_dimension)
                }
            };
        }
    }

    [Parameter, EditorRequired]
    public required Counter Counter { get; set; }

    protected override void OnInitialized()
    {
        chartLabels = CalcLabels();
    }

    private string[] CalcLabels()
    {
        var labels = new string[18];
        for (var i = 0; i < 18; i++)
        {
            labels[i] = (i < 17) ? $"{(-170 + (10 * i))}s" : "now";
        }
        return labels;
    }

    private double[] CalcChartValues(DimensionScope dimension)
    {
        var values = new double[18];
        foreach (var point in dimension.Values)
        {
            var start = CalcOffset(point.Start);
            var end = CalcOffset(point.End);
            if (start is not null && end is not null)
            {
                for (var i = start.GetValueOrDefault(0); i <= end.GetValueOrDefault(17); i++)
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

    private double[] CalcFakeValues()
    {
        var values = new double[18];
        var rnd = Random.Shared;
        for (var i = 0; i < 18; i++)
        {
            values[i] = rnd.NextDouble() * 100;
        }
        return values;
    }

    private int? CalcOffset(DateTime time)
    {
        var now = DateTime.UtcNow;
        var diff = now - time;
        var offset = 17 - (int)Math.Floor(diff.TotalSeconds / 10);
        return (offset >= 0 && offset < 18) ? offset : null;
    }
}
