using Nss.McpServer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom
    .Configuration(builder.Configuration)
    .CreateLogger();


builder.Services
    .AddLogging(config =>
    {
        config.ClearProviders();
        config.AddSerilog(Log.Logger, true);
    })
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<SqlExecutionTools>();

var app = builder.Build();

app.MapMcp();

app.Run();