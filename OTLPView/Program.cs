var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<TelemetryResults>();
builder.Services.AddSingleton<TracesPageState>();
builder.Services.AddSingleton<MetricsPageState>();
builder.Services.AddSingleton<LogsPageState>();

builder.Services.AddGrpc();
builder.Services.AddMudServices();

var otlpPort = builder.Configuration.GetValue("OTLP_PORT", 4317);
var webPorts = builder.Configuration.GetValue("ASPNETCORE_HTTP_PORTS", "8080")!.Split(',').Select(p => int.Parse(p)).ToArray();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(otlpPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
        listenOptions.UseHttps();
    });
    foreach (var port in webPorts)
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

app.MapGrpcService<DefaultMetricsService>();
app.MapGrpcService<DefaultTraceService>();
app.MapGrpcService<DefaultLogsService>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
