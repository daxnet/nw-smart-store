using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nss.ApiService.Models
{
    public sealed class DbSchema
    {
        [JsonPropertyName("tables")] public List<DbSchemaTable> Tables { get; set; } = [];

        [JsonPropertyName("relationships")] public List<DbSchemaRelationship> Relationships { get; set; } = [];

        public static async Task<DbSchema> ReadDbSchemaAsync(string fileName)
        {
            var fileStream = File.OpenRead(fileName);
            return await JsonSerializer.DeserializeAsync<DbSchema>(fileStream) ?? new DbSchema();
        }
    }
}
