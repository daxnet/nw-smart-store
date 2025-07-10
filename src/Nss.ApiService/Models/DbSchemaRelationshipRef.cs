using System.Text.Json.Serialization;

namespace Nss.ApiService.Models
{
    public record DbSchemaRelationshipRef
    {
        [JsonPropertyName("table")]
        public string? TableName { get; set; }

        [JsonPropertyName("field")]
        public string? FieldName { get; set; }

        public override string ToString() => $"{TableName}.{FieldName}";
    }
}
