using Microsoft.AspNetCore.Hosting.Server;
using MudBlazor.Charts;
using OTLPView.DataModel;
using OTLPView.Shared;

namespace OTLPView.Pages;

public sealed partial class LogViewer
{
    private List<OtlpLogEntry>? _logEntries;
    private readonly List<LogFilter> _logFilters = new();
    private string _textFilter = "";

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
        _textFilter = traceId;
        await UpdateQuery();
    }

    private bool OnApplyTextFilter(OtlpLogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_textFilter))
        {
            return true;
        }

        if (entry.TraceId == _textFilter)
        {
            return true;
        }

        if (entry.Application.ApplicationName.Contains(_textFilter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (entry.Message.Contains(_textFilter, StringComparison.OrdinalIgnoreCase))
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

        var results = TelemetryResults.Logs.AsQueryable<OtlpLogEntry>();
        foreach (var filter in _logFilters) { results = filter.Apply(results); }
        results = (!string.IsNullOrWhiteSpace(_textFilter)) ? results.Where(l => l.TraceId == _textFilter) : results;

        _logEntries = results.ToList();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnShowProperties(OtlpLogEntry entry)
    {
        await DialogService.ShowAsync<TableDialog>(
            entry.Application.ApplicationName,
            new DialogParameters()
            {
                [nameof(entry.Properties)] = entry.AllProperties()
            },
            new DialogOptions()
            {
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true,
                Position = DialogPosition.Center
            });
    }

    private void AddFilter(string field, FilterCondition condition, string value)
    {
        _logFilters.Add(new LogFilter() { Field = field, Condition = condition, Value = value });
        UpdateQuery();
    }

    private void RemoveFilter(LogFilter filter)
    {
        _logFilters.Remove(filter);
        UpdateQuery();
    }

    private async Task OpenFilter(LogFilter entry)
    {
        var dialog = await DialogService.ShowAsync<FilterDialog>(
            "Filter",
            new DialogParameters()
            {
                [nameof(FilterDialog.Filter)] = entry
            },
            new DialogOptions()
            {
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true,
                Position = DialogPosition.Center,
                NoHeader = true, 
               
            });
        var result = await dialog.Result;
        if (result.DataType == typeof(string))
        {
            _logFilters.Remove(entry);
        }
        else if (result.DataType == typeof(LogFilter) && entry is not null)
        {
            var index = _logFilters.IndexOf(entry);
            _logFilters[index] = (LogFilter)result.Data;
        }
        else if (result.DataType == typeof(LogFilter))
        {
            _logFilters.Add((LogFilter)result.Data);
        }
        UpdateQuery();
    }
}


