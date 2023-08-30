using OTLPView;
using OTLPView.Pages;
using OTLPView.DataModel;

public class MetricsPageState
{
    private Metrics _page;

    public void SetPage(Metrics page)
    {
        _page = page;
    }

    public void DataChanged()
    {
        if (_page is not null) { _page.Update(); }
    }

    private OtlpApplication _selectedService;
    public OtlpApplication SelectedApp {
        get        {return _selectedService;}
        
        set
        {
            _selectedService = value;
            _page.Update();
        }
    }


    private MeterResult _selectedMeter;
    public MeterResult SelectedMeter
    {
        get { return _selectedMeter; }

        set
        {
            _selectedMeter = value;
            _page.Update();
        }
    }


    private Counter _selectedMetric;
    public Counter SelectedMetric
    {
        get { return _selectedMetric; }

        set
        {
            _selectedMetric = value;
            _page.Update();
        }
    }
}
