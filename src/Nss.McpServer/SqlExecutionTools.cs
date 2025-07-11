using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using Npgsql;

namespace Nss.McpServer;

[McpServerToolType]
public sealed class SqlExecutionTools(IConfiguration configuration, ILogger<SqlExecutionTools> logger)
{
    [McpServerTool]
    [Description("Executes a SQL statement and returns the result as a string.")]
    public async Task<string> ExecuteSqlStatementAsync(
        [Description("The SQL statement to be executed.")]
        string sqlStatement)
    {
        try
        {
            logger.LogDebug("SQL statement to execute: {SqlStatement}", sqlStatement);
            var connectionString = configuration["db:connectionString"];
            await using var npgsqlConnection = new NpgsqlConnection(connectionString);
            await npgsqlConnection.OpenAsync();
            await using var command = new NpgsqlCommand(sqlStatement, npgsqlConnection);
            await using var reader = await command.ExecuteReaderAsync();
            var result = new StringBuilder();
            if (reader.HasRows)
            {
                // Append column names
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    result.Append(reader.GetName(i));
                    if (i < reader.FieldCount - 1)
                    {
                        result.Append(", ");
                    }
                }
                result.AppendLine();
            }
            while (await reader.ReadAsync())
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    result.Append(reader[i]?.ToString() ?? "NULL");
                    if (i < reader.FieldCount - 1)
                    {
                        result.Append(", ");
                    }
                }
                result.AppendLine();
            }
            await npgsqlConnection.CloseAsync();
            return result.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to execute SQL statement.");
            return string.Empty;
        }
    }
}