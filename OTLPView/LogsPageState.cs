using OTLPView.Pages;

namespace OTLPView
{
    public class LogsPageState
    {

        private LogViewer _page;

        public void SetPage(LogViewer page)
        {
            _page = page;
        }

        public void DataChanged()
        {
            if (_page is not null)
                _page.Update();
        }
    }
}
