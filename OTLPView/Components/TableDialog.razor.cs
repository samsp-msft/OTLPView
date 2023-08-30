namespace OTLPView.Components;

public sealed partial class TableDialog
{
    [CascadingParameter]
    public required MudDialogInstance Dialog { get; set; }

    [Parameter]
    public required Dictionary<string, string> Properties { get; set; }

    private void Ok() => Dialog.Close(DialogResult.Ok(true));
}
