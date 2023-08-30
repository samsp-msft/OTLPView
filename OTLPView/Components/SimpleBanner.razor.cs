namespace OTLPView.Components;

public sealed partial class SimpleBanner
{
    [Parameter, EditorRequired]
    public required string TitleText { get; set; }

    [Parameter, EditorRequired]
    public required string BodyText { get; set; }
}
