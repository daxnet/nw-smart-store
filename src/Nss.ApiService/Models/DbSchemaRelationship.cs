using System.Text.Json.Serialization;

namespace Nss.ApiService.Models
{
    public sealed class DbSchemaRelationship
    {
        [JsonPropertyName("from")]
        public DbSchemaRelationshipRef? From { get; set; }

        [JsonPropertyName("to")]
        public DbSchemaRelationshipRef? To { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        public override string ToString() => $"{From?.TableName}.{From?.FieldName} -> {To?.TableName}.{To?.FieldName}";
    }
}
