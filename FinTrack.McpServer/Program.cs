using FinTrack.McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// MCP over stdio reserves stdout for protocol messages. Send diagnostic output
// to stderr so an MCP client never receives log lines as protocol data.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<FinTrackApiClient>();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<FinTrackTools>();

await builder.Build().RunAsync();
