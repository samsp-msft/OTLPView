using Fluent = Microsoft.Fast.Components.FluentUI;

namespace OTLPView.Components;

public sealed partial class FilterDialog : Fluent.IDialogContentComponent<LogFilter>
{
    [CascadingParameter]
    public Fluent.FluentDialog? Dialog { get; set; }

    [Parameter]
    public LogFilter Content { get; set; }

    [Inject]
    public required TelemetryResults TelemetryResults { get; set; }

    private string Parameter { get; set; }
    private FilterCondition Condition { get; set; }
    private string Value { get; set; }

    protected override void OnInitialized()
    {
        if (Content != null)
        {
            Parameter = Content.Field;
            Condition = Content.Condition;
            Value = Content.Value;
        }
        else
        {
            Parameter = "Message";
            Condition = FilterCondition.Contains;
            Value = "";
        }

    }


    public List<string> Parameters
    {
        get
        {
            var result = new string[] { "Message", "Application", "TraceId", "SpanId", "ParentId", "OriginalFormat" }.ToList();
            result.AddRange(TelemetryResults.LogPropertyKeys.Values);
            return result;
        }
    }

    public Dictionary<FilterCondition, string> Conditions
    {
        get
        {
            var result = new Dictionary<FilterCondition, string>();
            foreach (var c in Enum.GetValues<FilterCondition>())
            {
                result.Add(c, LogFilter.ConditionToString(c));
            }
            return result;
        }
    }

    private void Cancel()
    {
        Dialog!.CancelAsync();
    }

    private void Delete()
    {
        Dialog!.CloseAsync(Fluent.DialogResult.Ok(new FilterDialogResult() { Filter = Content, Delete = true }));
    }

    private void Apply()
    {
        if (Content == null)
        {
            Content = new LogFilter()
            {
                Field = Parameter,
                Condition = Condition,
                Value = Value
            };
            Dialog!.CloseAsync(Fluent.DialogResult.Ok(new FilterDialogResult() { Filter = Content, Add = true }));
        }
        else
        {
            Content.Field = Parameter;
            Content.Condition = Condition;
            Content.Value = Value;

            Dialog!.CloseAsync(Fluent.DialogResult.Ok(new FilterDialogResult() { Filter = Content, Delete = false }));
        }
    }
}


