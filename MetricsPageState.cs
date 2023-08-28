using OTLPView;
using OTLPView.Pages;

public class MetricsPageState
{
    private Metrics _page;

    public void SetPage(Metrics page)
    {
        _page = page;
    }

    public void DataChanged()
    {
        if (_page is not null)
            _page.Update();
    }

}