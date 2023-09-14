using Microsoft.AspNetCore.Hosting.Server;
using OTLPView.DataModel;
using Fluent = Microsoft.Fast.Components.FluentUI;

namespace OTLPView.Pages;

public sealed partial class LogViewer
{
    private IQueryable<OtlpLogEntry> _logEntries { get; set; }
    private readonly List<LogFilter> _logFilters = new();
    private string _textFilter = "";

    private bool HasLogData => TelemetryResults is { Applications.Count: > 0 };

    [Inject]
    public required TelemetryResults TelemetryResults { get; set; }

    [Inject]
    public required LogsPageState State { get; set; }

    [Inject]
    public Fluent.IDialogService DialogService { get; set; }

    private Fluent.PaginationState pagination = new Fluent.PaginationState { ItemsPerPage = 30, };

    //[Inject]
    //public required IDialogService DialogService { get; set; }

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
        //if (TelemetryResults is { Logs.Count: 0 })
        //{
        //    _logEntries = null;
        //    return;
        //}

        var results = TelemetryResults.Logs.AsQueryable<OtlpLogEntry>();
        foreach (var filter in _logFilters) { results = filter.Apply(results); }

        _logEntries = results;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnShowProperties(OtlpLogEntry entry)
    {
        //await DialogService.ShowAsync<TableDialog>(
        //    entry.Application.ApplicationName,
        //    new DialogParameters()
        //    {
        //        [nameof(entry.Properties)] = entry.AllProperties()
        //    },
        //    new DialogOptions()
        //    {
        //        FullWidth = true,
        //        CloseButton = true,
        //        CloseOnEscapeKey = true,
        //        Position = DialogPosition.Center
        //    });

        Fluent.DialogParameters<Dictionary<string,string>> parameters = new()
        {
            Title = "Log Entry Details",
            Alignment = Fluent.HorizontalAlignment.Right,

            //Width = "600px",
            //Height = "100px",
            Content = entry.AllProperties(),
            //TrapFocus = true,
            //Modal = true,
            PrimaryAction = null,
            SecondaryAction = null,
        };
        DialogService.ShowPanel<LogDetailsDialog, Dictionary<string,string>>(parameters);
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

    private void OpenFilter(LogFilter entry)
    {
        var title = (entry is { }) ? "Edit Filter" : "Add Filter";
        Fluent.DialogParameters<LogFilter> parameters = new()
        {
            OnDialogResult = DialogService.CreateDialogCallback(this, HandleFilterDialog),
            Title=title,
            Alignment = Fluent.HorizontalAlignment.Right,

            //Width = "600px",
            //Height = "100px",
            Content = entry,
            //TrapFocus = true,
            //Modal = true,
            PrimaryAction = null,
            SecondaryAction = null,
        };
        DialogService.ShowPanel<FilterDialog, LogFilter>(parameters);
    }

    private async Task HandleFilterDialog(Fluent.DialogResult result)
    {
        if (result.Data is not null)
        {
            var fdr = result.Data as FilterDialogResult;
            if (fdr.Delete)
            {
                _logFilters.Remove(fdr.Filter as LogFilter);
            }
            else if (fdr.Add)
            {                 _logFilters.Add(fdr.Filter as LogFilter);
                       }
        }
        UpdateQuery();

    }
}


    public class FilterDialogResult
{
    public LogFilter Filter { get; set; }
    public bool Delete { get; set; }
    public bool Add { get; set; }
}


