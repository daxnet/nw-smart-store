using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Qdrant.Client;

namespace Nss.ApiService.Services
{
    public sealed class SearchService(
        Kernel kernel,
        QdrantClient qdrantClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<SearchService> logger)
    {
        private readonly ChatHistory _chatHistory = new();
        private readonly IChatCompletionService _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(string query)
        {
            var prompt = new StringBuilder(query);
            prompt.AppendLine();

            // Generate the embeddings based on the query
            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            // Search the vector database for related tables, fields, and relationships based
            // on the query embedding
            var tableResults = await qdrantClient.SearchAsync(QdrantIndexService.TableCollectionName,
                queryEmbedding.Vector, limit: 5);
            var fieldResults = await qdrantClient.SearchAsync(QdrantIndexService.FieldCollectionName,
                queryEmbedding.Vector, limit: 5);
            var relationshipResults = await qdrantClient.SearchAsync(QdrantIndexService.RelationshipCollectionName,
                queryEmbedding.Vector, limit: 5);

            // Adding more contextual information to the prompt
            if (tableResults.Any())
            {
                logger.LogDebug("Found table schema information hits in the vector DB");
                prompt.AppendLine("Followings are the tables that are related to the query:");
                prompt.AppendLine(string.Join(Environment.NewLine,
                    tableResults.Select(point => point.Payload["name"].ToString())));
                if (fieldResults.Any())
                {
                    prompt.AppendLine("Followings are the fields that might be related:");
                    prompt.AppendLine(string.Join(Environment.NewLine,
                        fieldResults.Select(point => $"table: {point.Payload["table"]}, field: {point.Payload["name"]}")));
                }

                if (relationshipResults.Any())
                {
                    prompt.AppendLine("You might also need to consider the following table relationships:");
                    prompt.AppendLine(string.Join(Environment.NewLine,
                        relationshipResults.Select(point => $"from field: {point.Payload["fromTable"]} references: {point.Payload["toTable"]}")));
                }

                prompt.AppendLine("Please generate the PostgreSQL SQL statement and give me the query results.");
            }
            else
            {
                logger.LogDebug("Didn't find table schema information hits in the vector DB");
                prompt.AppendLine("Please answer the user's question.");
            }

            _chatHistory.Clear();
            _chatHistory.AddUserMessage(prompt.ToString()); 
            var chatResponse = _chatCompletionService.GetStreamingChatMessageContentsAsync(_chatHistory, kernel: kernel);
            await foreach (var content in chatResponse)
            {
                yield return content;
            }
        }
    }
}
