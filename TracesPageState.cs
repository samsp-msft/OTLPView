using OTLPView;
using OTLPView.Pages;

public class TracesPageState
{
    private Traces _page;
    private Span _selectedSpan;
    private Operation _selectedOperation;
    public Span SelectedSpan
    {
        get { return _selectedSpan; }
        set
        {
            _selectedSpan = value;
            _page.Update();
        }
    }

    public Operation SelectedOperation
    {
        get { return _selectedOperation; }
        set
        {
            _selectedOperation = value;
            _page.Update();
        }
    }
    public void SetPage(Traces page)
    {
        _page = page;
    }

    public void DataChanged()
    {
        if (_page is not null)
        _page.Update();
    }

}