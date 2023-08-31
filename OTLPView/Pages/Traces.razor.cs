using MudBlazor;
using static MudBlazor.CategoryTypes;
using OTLPView.DataModel;

namespace OTLPView.Pages;

public sealed partial class Traces
{
    [Inject]
    public required TelemetryResults TelemetryResults { get; set; }

    [Inject]
    public required TracesPageState State { get; set; }

    private MudTable<TraceOperation> opsTable;

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

    private void OperationClick(TableRowClickEventArgs<TraceOperation> args)
    {
        State.SelectedOperation = args.Item;
        State.SelectedSpan = State.SelectedOperation.RootSpans.Values.First();
    }

    private int selectedRowNumber = -1;
    private string SelectedRowClassFunc(TraceOperation o, int rowNumber)
    {
        if (selectedRowNumber == rowNumber)
        {
            selectedRowNumber = -1;
            return string.Empty;
        }
        else if (opsTable.SelectedItem != null && opsTable.SelectedItem.Equals(o))
        {
            selectedRowNumber = rowNumber;
            return "selected";
        }
        else
        {
            return string.Empty;
        }
    }

    private string ShortOpName(string name)
    {
        if (name.Length > 8)
        {
            return $"{name.Substring(0, 1)}…{name.Substring(name.Length-6)}".HtmlEncode();
        }
        else
        {
            return name;
        }
    }
}
