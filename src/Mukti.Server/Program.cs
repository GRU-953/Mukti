using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

const int PORT = 43017;
const int POLL_MS = 5000;

var binDir = Path.GetDirectoryName(Environment.ProcessPath)
    ?? AppContext.BaseDirectory;
var wwwRoot = Path.Combine(binDir, "wwwroot");

WebApplication? app = null;
bool running = false;
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

async Task Start()
{
    if (running) return;
    running = true;
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = binDir, WebRootPath = wwwRoot });
    builder.Logging.ClearProviders();
    builder.WebHost.UseKestrel(o => o.Listen(IPAddress.Loopback, PORT));
    app = builder.Build();
    app.Use(async (ctx, next) => { ctx.Response.Headers["Access-Control-Allow-Origin"] = "*"; await next(); });
    var ctp = new FileExtensionContentTypeProvider();
    ctp.Mappings[".wasm"] = "application/wasm";
    ctp.Mappings[".dll"] = "application/octet-stream";
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(wwwRoot), ContentTypeProvider = ctp });
    app.MapFallbackToFile("index.html");
    await app.StartAsync(cts.Token);
}

async Task Stop()
{
    if (!running || app == null) return;
    running = false;
    await app.StopAsync();
    await app.DisposeAsync();
    app = null;
}

bool IsOfficeRunning()
{
    foreach (var name in new[] { "Microsoft Word", "Microsoft Excel", "Microsoft PowerPoint", "WINWORD", "EXCEL", "POWERPNT" })
        try { if (Process.GetProcessesByName(name).Length > 0) return true; } catch { }
    return false;
}

while (!cts.Token.IsCancellationRequested)
{
    var officeOpen = IsOfficeRunning();
    if (officeOpen && !running) await Start();
    else if (!officeOpen && running) await Stop();
    try { await Task.Delay(POLL_MS, cts.Token); } catch (TaskCanceledException) { break; }
}
await Stop();
