using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OTLPView;
using OTLPView.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<TelemetryResults>();
builder.Services.AddSingleton<TracesPageState>();
builder.Services.AddSingleton<MetricsPageState>();

builder.Services.AddGrpc();

var OTLPPort = builder.Configuration.GetValue<int>("OTLP_PORT", 4317);
var WebPorts = builder.Configuration.GetValue<string>("ASPNETCORE_HTTP_PORTS", "8080").Split(',').Select(p => int.Parse(p)).ToArray();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(OTLPPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
        listenOptions.UseHttps();
    });
    foreach (var port in WebPorts)
    {
        options.ListenAnyIP(port, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps();
        });
    }
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapGrpcService<MetricsServiceImpl>();
app.MapGrpcService<TraceServiceImpl>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
