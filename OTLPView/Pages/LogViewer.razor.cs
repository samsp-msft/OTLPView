namespace OTLPView.Pages;

public sealed partial class LogViewer
{
    private List<OtlpLogEntry>? _logEntries;
    private string _filter = "";

    private bool HasLogData => TelemetryResults is { Applications.Count: > 0 };

    [Inject]
    public required TelemetryResults TelemetryResults { get; set; }

    [Inject]
    public required LogsPageState State { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        State.SetPage(this);

        await UpdateQuery();
    }

    public Task Update() => UpdateQuery();

    public async Task OnFilterByTraceId(string traceId)
    {
        _filter = traceId;
        await UpdateQuery();
    }

    private bool OnApplyFilter(OtlpLogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_filter))
        {
            return true;
        }

        if (entry.TraceId == _filter)
        {
            return true;
        }

        if (entry.Application.ApplicationName.Contains(_filter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (entry.Message.Contains(_filter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private async Task UpdateQuery()
    {
        if (TelemetryResults is { Logs.Count: 0 })
        {
            _logEntries = null;
            return;
        }

        _logEntries = string.IsNullOrWhiteSpace(_filter)
            ? TelemetryResults.Logs.ToList()
            : TelemetryResults.Logs.Where(l => l.TraceId == _filter).ToList();

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnShowProperties(OtlpLogEntry entry)
    {
        await DialogService.ShowAsync<TableDialog>(
            entry.Application.ApplicationName,
            new DialogParameters()
            {
                [nameof(entry.Properties)] = entry.Properties
            },
            new DialogOptions()
            {
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true,
                Position = DialogPosition.Center
            });
    }
}
