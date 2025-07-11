using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Nss.McpServer;

[McpServerToolType]
public sealed class SqlExecutionTools(IConfiguration configuration)
{
    [McpServerTool]
    [Description("Executes a SQL statement and returns the result as a string.")]
    public async Task<string> ExecuteSqlStatementAsync(
        [Description("The SQL statement to be executed.")]
        string sqlStatement)
    {
        var connectionString = configuration["db:connectionString"];
        Console.WriteLine("tool called.");
        await Task.CompletedTask;
        return string.Empty;
    }
}