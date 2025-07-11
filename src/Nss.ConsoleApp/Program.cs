using System.Text;
using System.Text.Json;
using ModelContextProtocol.Client;

namespace Nss.ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ReSharper disable once InconsistentNaming
            const string ApiUrl = "http://localhost:5030/api/search";
            using var httpClient = new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(200)
            });

            Console.WriteLine("I am the AI Assistant of Northwind Sales Team. How can I help you today?");

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.Write("> ");
                var query = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;

                if (string.IsNullOrWhiteSpace(query))
                {
                    continue;
                }

                if (query.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    query.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (query.Equals("cls", StringComparison.OrdinalIgnoreCase) ||
                    query.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Clear();
                }

                if (query.Equals("list-tools", StringComparison.OrdinalIgnoreCase))
                {
                    await ListMcpToolsAsync();
                    continue;
                }

                var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { query }),
                        Encoding.UTF8,
                        "application/json")
                };

                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                
                var buffer = new char[1024];
                int read;
                while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    Console.Write(new string(buffer, 0, read));
                    await Console.Out.FlushAsync();
                }

                Console.WriteLine();
            }

            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Done.");
        }

        static async Task ListMcpToolsAsync()
        {
            await using var mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(
                new SseClientTransportOptions
                {
                    Endpoint = new Uri("http://localhost:5276")
                }));
            var tools = await mcpClient.ListToolsAsync();
            foreach (var tool in tools)
            {
                Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
            }
        }
    }
}
