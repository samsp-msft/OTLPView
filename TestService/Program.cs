// <WholeProgram>
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.Core;
using Google.Protobuf.WellKnownTypes;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

var greeterMeter = new Meter("MyTestMeter.Example", "1.0.0");
var countGreetings = greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");

// Custom ActivitySource for the application
var greeterActivitySource = new ActivitySource("OtPrGrJa.Example");

builder.Services.AddHttpClient();

var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"];
var isTestCmd = builder.Configuration["TEST_CMD"] != null;

builder.Logging.AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.AddOtlpExporter((otlp, EaP) =>
        {
            otlp.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
            otlp.Endpoint = new Uri(tracingOtlpEndpoint);
            otlp.Protocol = OtlpExportProtocol.Grpc;
            EaP.BatchExportProcessorOptions = new BatchExportLogRecordProcessorOptions()
            {
                ExporterTimeoutMilliseconds = 10000,
                MaxQueueSize = 1000,
                MaxExportBatchSize = 256,
            };
        });
    });

var otel = builder.Services.AddOpenTelemetry();
var instanceId = Guid.NewGuid().ToString();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName, serviceInstanceId: instanceId));

// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
otel.WithMetrics(metrics =>
{
    metrics
        // Metrics provider from OpenTelemetry
        .AddMeter(greeterMeter.Name)

       // Metrics provides by ASP.NET Core in .NET 8
       .AddAspNetCore8Instrumentation()

        .AddOtlpExporter((otlpOptions, MetricReaderOptions) =>
        {
            otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
            otlpOptions.Protocol = OtlpExportProtocol.Grpc;

            MetricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
            MetricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative;

        });
});


// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSource(greeterActivitySource.Name);
    if (tracingOtlpEndpoint != null)
    {
        tracing.AddOtlpExporter(otlpOptions =>
         {
             otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
         });
    }
    else
    {
        tracing.AddConsoleExporter();
    }
});

var app = builder.Build();

app.MapGet("/", SendGreeting);
app.MapGet("/NestedGreeting", SendNestedGreeting);



app.Run();


async Task<string> SendGreeting(ILogger<Program> logger)
{
    // Create a new Activity scoped to the method
    using var activity = greeterActivitySource.StartActivity("GreeterActivity");

    // Log a message
    logger.LogInformation("Sending greeting");

    // Increment the custom counter
    countGreetings.Add(1);

    // Add a tag to the Activity
    activity?.SetTag("greeting", "Hello World!");

    return "Hello World!";
}

async Task SendNestedGreeting(int nestlevel, ILogger<Program> logger, HttpContext context, IHttpClientFactory clientFactory)
{
    // Create a new Activity scoped to the method
    using var activity = greeterActivitySource.StartActivity("GreeterActivity");
    var now = DateTimeOffset.UtcNow;

    if (nestlevel <= 5)
    {
        // Log a message
        logger.LogInformation("Sending greeting, level {nestlevel}", nestlevel);

        // Increment the custom counter
        countGreetings.Add(1);

        // Add a tag to the Activity
        activity?.SetTag("nest-level", nestlevel);

        var thread = new Thread(() =>
        {
            var random = Random.Shared;
            for (var i = 0; i < random.Next(10); i++)
            {
                var delay = random.Next(1, 20);
                Thread.Sleep(delay);
                activity.AddEvent(new ActivityEvent("Event_" + i, tags: new ActivityTagsCollection { { "i", i.ToString() }, { "delay", delay.ToString() } }));
            }
        });
        thread.Start();

        await context.Response.WriteAsync($"Nested Greeting, level: {nestlevel}\r\n");

        if (nestlevel > 0)
        {
            var request = context.Request;
            var host = request.Host.ToString();
            if (isTestCmd)
            {
                var random = Random.Shared;
                host = $"localhost:{random.Next(5000, 5005)}";
            }


            var url = new Uri($"{request.Scheme}://{host}{request.Path}?nestlevel={nestlevel - 1}");
            logger.LogInformation("Calling {url}", url);
            // Makes an http call passing the activity information as http headers
            var nestedResult = await clientFactory.CreateClient().GetStringAsync(url);
            await context.Response.WriteAsync(nestedResult);
        };
        thread.Join();
    }
    else
    {
        // Log a message
        logger.LogError("Greeting nest level {nestlevel} too high", nestlevel);
        await context.Response.WriteAsync("Nest level too high, max is 5");
    }
    var duration = (DateTime.UtcNow - now).TotalMilliseconds;
    logger.LogInformation("Greeting finished for {nestlevel} with duration {duration} ", nestlevel, duration);
}

public static class helpers
{
    public static MeterProviderBuilder AddAspNetCore8Instrumentation(this MeterProviderBuilder meterProviderBuilder)
    {
        var boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 };
        meterProviderBuilder.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Net.Http");

        foreach (var v in new string[] {
            "http-server-request-duration",
            "http-server-current-requests",
            "kestrel-connection-duration",
            "http-client-connection-duration",
            "http-client-requests-queue-duration",
            "http-client-request-duration"
        })
        {
            meterProviderBuilder.AddView(v, new ExplicitBucketHistogramConfiguration() { Boundaries = boundaries });
        }
        return meterProviderBuilder;
    }

}
