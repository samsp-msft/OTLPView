using Fluent = Microsoft.Fast.Components.FluentUI;
namespace OTLPView.Components;

public sealed partial class LogDetailsDialog : Fluent.IDialogContentComponent<Dictionary<string,string>>
{
    [CascadingParameter]
    public Fluent.FluentDialog? Dialog { get; set; }

    private void Close() => Dialog.CloseAsync(Fluent.DialogResult.Ok(true));

    [Parameter]
    public Dictionary<string, string> Content { get; set; }
}
