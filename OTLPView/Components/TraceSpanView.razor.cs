using OTLPView.DataModel;

namespace OTLPView.Components;

public sealed partial class TraceSpanView
{
    [Inject]
    public required TracesPageState state { get; set; }

    private TraceSpan _span;

    [Parameter]
    public TraceSpan Span
    {
        get { return _span; }
        set
        {
            _span = value;
            CalculateOffsets();
        }
    }

    private double _leftOffset;
    private double _width;
    private double _rightOffset;
    private double _left_ms;

    private void CalculateOffsets()
    {
        if (Span.ParentSpan != null)
        {
            var rootDuration = Span.RootSpan.Duration.TotalMilliseconds;
            _leftOffset = (Span.StartTime - Span.RootSpan.StartTime).TotalMilliseconds / rootDuration * 100;
            _width = (Span.EndTime - Span.StartTime).TotalMilliseconds / rootDuration * 100;
            _rightOffset = 100 - _leftOffset - _width;
            _left_ms = (Span.StartTime - Span.RootSpan.StartTime).TotalMilliseconds;
        }
        else
        {
            _leftOffset = _rightOffset = 0;
            _width = 100;
        }
    }

    private void UpdateSelection() => state.SelectedSpan = Span;

   public string BarColor => (state.SelectedSpan == Span) ? "#4D5788" : Span.TraceScope.BarColor;
}


