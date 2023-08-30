namespace OTLPView.Pages;

public sealed partial class Metrics
{
    [Inject]
    public required TelemetryResults TelemetryResults { get; set; }

    [Inject]
    public required MetricsPageState State { get; set; }

    protected override void OnInitialized()
    {
        State.SetPage(this);
        UpdateSelectedApp();
        UpdateSelectedMeter();
        UpdateSelectedMetric();
    }

    public void Update()
    {
        UpdateSelectedApp();
        UpdateSelectedMeter();
        UpdateSelectedMetric();
        InvokeAsync(() => StateHasChanged());
    }

    public void UpdateSelectedApp()
    {
        if (State.SelectedApp is null)
        {
            if (TelemetryResults.Applications.Count > 0)
            {
                State.SelectedApp = TelemetryResults.Applications.Values.First();
            }
        }
    }

    public void UpdateSelectedMeter()
    {
        if (State.SelectedMeter is null)
        {
            if (State.SelectedApp?.Meters.Count > 0)
            {
                State.SelectedMeter = State.SelectedApp.Meters.Values.First();
            }
        }
    }

    public void UpdateSelectedMetric()
    {
        if (State.SelectedMetric is null)
        {
            if (State.SelectedMeter?.Counters.Count > 0)
            {
                State.SelectedMetric = State.SelectedMeter.Counters.Values.First();
            }
        }
    }

    public void SelectApp(OtlpApplication s)
    {
        State.SelectedApp = s;
        State.SelectedMeter = State.SelectedApp.Meters.Values.First();
    }

    public string IsAppSelected(OtlpApplication o, string cssClass) =>
        $"{cssClass}{((State.SelectedApp == o) ? " Selected" : "")}";

    public void SelectMeter(MeterResult m)
    {
        State.SelectedMeter = m;
        State.SelectedMetric = m.Counters.Values.First();
    }

    public string IsMeterSelected(MeterResult o, string cssClass) =>
        cssClass + ((State.SelectedMeter == o) ? " Selected" : "");

    public void SelectMetric(Counter c) => State.SelectedMetric = c;

    public string IsMetricSelected(Counter o, string cssClass) =>
        cssClass + ((State.SelectedMetric == o) ? " Selected" : "");
}
