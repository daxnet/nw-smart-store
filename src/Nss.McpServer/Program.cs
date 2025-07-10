using Nss.McpServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<SqlExecutionTools>();

var app = builder.Build();

app.MapMcp();

app.Run();