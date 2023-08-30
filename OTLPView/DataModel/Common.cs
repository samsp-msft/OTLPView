using System.Collections.Concurrent;
using OpenTelemetry.Proto.Resource.V1;

namespace OTLPView.DataModel;

public class OtlpApplication
{
    public const string SERVICE_NAME = "service.name";
    public const string SERVICE_INSTANCE_ID = "service.instance.id";


    public string ApplicationName { get; init; }
    public string InstanceId { get; init; }
    public int Suffix { get; init; }

    private readonly Dictionary<string, string> _properties = new();

    public IReadOnlyDictionary<string, string> Properties => _properties;

    public int[] ColorSequence { get; init; }

    public OtlpApplication(Resource resource, IReadOnlyDictionary<string, OtlpApplication> applications)
    {
        foreach (var attribute in resource.Attributes)
        {
            switch (attribute.Key)
            {
                case SERVICE_NAME:
                    ApplicationName = attribute.Value.ValueString();
                    break;
                case SERVICE_INSTANCE_ID:
                    InstanceId = attribute.Value.ValueString();
                    break;
                default:
                    _properties.TryAdd(attribute.Key, attribute.Value.ValueString());
                    break;

            }
        }
        if (string.IsNullOrEmpty(ApplicationName)) { ApplicationName = "Unknown"; }
        if (string.IsNullOrEmpty(InstanceId)) { throw new ArgumentException("Resource needs to include a 'service.instance.id'"); }
        Suffix = applications.Where(a => a.Value.ApplicationName == ApplicationName).Count();
        ColorSequence = Helpers.ColorSequence[applications.Count];
    }

    public string UniqueApplicationName => $"{ApplicationName}-{Suffix}";

    public string ShortApplicationName
    {
        get
        {
            var n = ApplicationName + Suffix.ToString();
            return (n.Length <= 10) ? n : $"{ApplicationName.Left(3)}…{ApplicationName.Right(5)}{Suffix}";
        }
    }

    #region Metrics
    private readonly ConcurrentDictionary<string, MeterResult> _meters = new();
    public IReadOnlyDictionary<string, MeterResult> Meters => _meters;

    public MeterResult GetOrAddMeter(string meterName, Func<string, MeterResult> itemFactory)
    {
        return _meters.GetOrAdd(meterName, _ => itemFactory.Invoke(meterName));
    }
    #endregion


    #region Traces

    private readonly ConcurrentDictionary<string, TraceScope> _scopes = new();
    public IReadOnlyDictionary<string, TraceScope> Scopes => _scopes;

    public TraceScope GetOrAddTrace(string scopeName, Func<string, TraceScope> itemFactory)
    {
        return _scopes.GetOrAdd(scopeName, _ => itemFactory.Invoke(scopeName));
    }
    #endregion




}

public static class CommonHelpers
{
    public static string GetServiceId(this Resource resource)
    {
        foreach (var attribute in resource.Attributes)
        {
            if (attribute.Key == OtlpApplication.SERVICE_INSTANCE_ID)
            {
                return attribute.Value.ValueString();
            }
        }
        return null;
    }
}
