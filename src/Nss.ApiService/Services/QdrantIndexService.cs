using Microsoft.Extensions.AI;
using Nss.ApiService.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Nss.ApiService.Services
{
    public sealed class QdrantIndexService(
        ILogger<QdrantIndexService> logger,
        QdrantClient client,
        IWebHostEnvironment webHostEnvironment,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {

        #region Internal Fields

        internal const string FieldCollectionName = "nss_fields";
        internal const string RelationshipCollectionName = "nss_relationships";
        internal const string TableCollectionName = "nss_tables";

        #endregion Internal Fields

        #region Private Fields

        private const string DbSchemaDefinitionFileName = "northwind_schema.{0}.json";

        #endregion Private Fields

        #region Public Methods

        public async Task CreateIndexAsync(string lang = "en", bool force = false)
        {
            var fileName = string.Format(DbSchemaDefinitionFileName, lang);
            var fileFullName = Path.Combine(webHostEnvironment.WebRootPath, fileName);
            if (!File.Exists(fileFullName))
            {
                throw new FileNotFoundException($"Schema definition file '{fileFullName}' does not exist.");
            }

            var schemaDefinition = await DbSchema.ReadDbSchemaAsync(fileFullName);
            try
            {
                logger.LogInformation("Creating index for schema definition file '{FileName}'...", fileFullName);
                var existingCollections = await client.ListCollectionsAsync();
                logger.LogDebug("Building table indexes...");
                await IndexTablesAsync(schemaDefinition, existingCollections, force);
                logger.LogDebug("Building field indexes...");
                await IndexFieldsAsync(schemaDefinition, existingCollections, force);
                logger.LogDebug("Building relationship indexes...");
                await IndexRelationshipsAsync(schemaDefinition, existingCollections, force);
                logger.LogInformation("Index built successfully.");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to create index.");
                throw;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private async Task IndexFieldsAsync(DbSchema schema, IReadOnlyList<string> existingCollections,
            bool forceRecreateCollection = false)
        {
            if (await PrepareCollectionAsync(FieldCollectionName, existingCollections, forceRecreateCollection))
            {
                var embeddings = await Task.WhenAll(schema.Tables
                    .SelectMany(table => table.Fields.Select(async (field, idx) => new PointStruct
                    {
                        Id = new PointId { Num = (ulong)idx },
                        Vectors = new Vectors
                        {
                            Vector = new Vector
                            {
                                Data =
                                {
                                    (await embeddingGenerator.GenerateAsync(field.Description ?? string.Empty))
                                    .Vector
                                    .ToArray()
                                }
                            }
                        },
                        Payload =
                        {
                            {
                                "table", new Value { StringValue = table.Name }
                            },
                            {
                                "name", new Value { StringValue = field.Name }
                            },
                            {
                                "description", new Value { StringValue = field.Description ?? string.Empty }
                            },
                            {
                                "type", new Value { StringValue = field.Type ?? string.Empty }
                            }
                        }
                    })));

                await client.UpsertAsync(FieldCollectionName, embeddings);
            }
        }

        private async Task IndexRelationshipsAsync(DbSchema schema, IReadOnlyList<string> existingCollections,
            bool forceRecreateCollection = false)
        {
            if (await PrepareCollectionAsync(RelationshipCollectionName, existingCollections, forceRecreateCollection))
            {
                var embeddings = await Task.WhenAll(schema.Relationships
                    .Select(async (relationship, idx) =>
                        new PointStruct
                        {
                            Id = new PointId { Num = (ulong)idx },
                            Vectors = new Vectors
                            {
                                Vector = new Vector
                                {
                                    Data =
                                    {
                                        (await embeddingGenerator.GenerateAsync(
                                            relationship.Description ?? string.Empty))
                                        .Vector
                                        .ToArray()
                                    }
                                }
                            },
                            Payload =
                            {
                                {
                                    "description", new Value { StringValue = relationship.Description ?? string.Empty }
                                },
                                {
                                    "fromTable", new Value { StringValue = relationship.From?.ToString() ?? string.Empty }
                                },
                                {
                                    "toTable", new Value { StringValue = relationship.To?.ToString() ?? string.Empty }
                                }
                            }
                        }));

                await client.UpsertAsync(RelationshipCollectionName, embeddings);
            }
        }

        private async Task IndexTablesAsync(DbSchema schema, IReadOnlyList<string> existingCollections,
                            bool forceRecreateCollection = false)
        {
            if (await PrepareCollectionAsync(TableCollectionName, existingCollections, forceRecreateCollection))
            {
                var embeddings = await Task.WhenAll(schema.Tables
                    .Select(async (table, idx) =>
                        new PointStruct
                        {
                            Id = new PointId { Num = (ulong)idx },
                            Vectors = new Vectors
                            {
                                Vector = new Vector
                                {
                                    Data =
                                    {
                                        (await embeddingGenerator.GenerateAsync(table.Description ?? string.Empty))
                                        .Vector
                                        .ToArray()
                                    }
                                }
                            },
                            Payload =
                            {
                                {
                                    "name", new Value { StringValue = table.Name }
                                },
                                {
                                    "description", new Value { StringValue = table.Description ?? string.Empty }
                                },
                                {
                                    "fieldCount", new Value { IntegerValue = table.Fields.Count }
                                }
                            }
                        }));

                await client.UpsertAsync(TableCollectionName, embeddings);
            }
        }

        private async Task<bool> PrepareCollectionAsync(string collectionName,
            IReadOnlyList<string> existingCollections,
            bool forceRecreateCollection = false)
        {
            var dropCollection = existingCollections.Contains(collectionName) && forceRecreateCollection;
            var createCollection = !existingCollections.Contains(collectionName) || forceRecreateCollection;

            if (dropCollection)
            {
                await client.DeleteCollectionAsync(collectionName);
            }

            if (createCollection)
            {
                await client.CreateCollectionAsync(collectionName, new VectorParams
                {
                    Size = 3072,
                    Distance = Distance.Cosine
                });
            }

            return dropCollection || createCollection;
        }

        #endregion Private Methods

    }
}