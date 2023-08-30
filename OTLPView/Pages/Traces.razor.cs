namespace OTLPView.Pages;

public sealed partial class Traces
{
    [Inject]
    public required TelemetryResults TelemetryResults { get; set; }

    [Inject]
    public required TracesPageState State { get; set; }

    protected override void OnInitialized()
    {
        State.SetPage(this);
        UpdateSelectedOperation();
        UpdateSelectedSpan();
    }

    public void Update()
    {
        UpdateSelectedOperation();
        UpdateSelectedSpan();
        InvokeAsync(() => StateHasChanged());
    }

    public void UpdateSelectedOperation()
    {
        if (State.SelectedOperation is null)
        {
            if (TelemetryResults.Operations.Count > 0)
            {
                State.SelectedOperation = TelemetryResults.Operations.First();
            }
        }
    }

    public void UpdateSelectedSpan()
    {
        if (State.SelectedSpan is null)
        {
            if (State.SelectedOperation is not null && State.SelectedOperation.RootSpans.Count > 0)
            {
                State.SelectedSpan = State.SelectedOperation.RootSpans.Values.First();
            }
        }
    }

    public void SelectOperation(TraceOperation o)
    {
        State.SelectedOperation = o;
        State.SelectedSpan = State.SelectedOperation.RootSpans.Values.First();
    }

    public string IsSelected(TraceOperation o, string cssClass) =>
        cssClass + ((State.SelectedOperation == o) ? " Selected" : "");
}
