namespace OTLPView.Shared;

public sealed partial class DimensionedCounterView
{
    private DimensionScope _dimension;
    private string[] chartLabels;
    private List<ChartSeries> chartValues;

    [Parameter]
    public DimensionScope Dimension
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

    [Parameter]
    public Counter Counter { get; set; }

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
                for (int i = start.GetValueOrDefault(0); i <= end.GetValueOrDefault(17); i++)
                {
                    if (point as MetricValue<long> != null)
                    { values[i] = Math.Max((point as MetricValue<long>).Value, values[i]); }
                    else
                    { values[i] = Math.Max((point as MetricValue<double>).Value, values[i]); }
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
