namespace OTLPView.Services;

public class LogsPageState
{
    private LogViewer? _page;

    public void SetPage(LogViewer page) => _page = page;

    public Task DataChanged() => _page?.Update() ?? Task.CompletedTask;
}
