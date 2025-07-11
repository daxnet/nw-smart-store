using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Qdrant.Client;

namespace Nss.ApiService.Services
{
    public sealed class SearchService(
        Kernel kernel,
        QdrantClient qdrantClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<SearchService> logger)
    {
        #region Private Fields

        private readonly PromptExecutionSettings _promptExecutionSettings =
            new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new()
                {
#pragma warning disable SKEXP0001
                    RetainArgumentTypes = true
#pragma warning restore SKEXP0001
                })
            };

        #endregion Private Fields

        #region Public Methods

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

            string instruction;

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

                instruction =
                    "Please generate the PostgreSQL SQL statement and use the registered tools to execute the SQL, then give back the results.";
            }
            else
            {
                logger.LogDebug("Didn't find table schema information hits in the vector DB");
                instruction = "Please answer the user's question.";
            }

            var agent = new ChatCompletionAgent
            {
                Instructions = instruction,
                Name = "NssChatAgent",
                Kernel = kernel,
                Arguments = new KernelArguments(_promptExecutionSettings)
            };

            var response = agent.InvokeStreamingAsync(prompt.ToString());
            await foreach (var content in response)
            {
                yield return content.Message;
            }
        }

        #endregion Public Methods
    }
}
