using System.Text.Json.Serialization;

namespace Nss.ApiService.Models
{
    public sealed class DbSchemaTable
    {
        [JsonPropertyName("table_name")] public string? Name { get; set; }

        [JsonPropertyName("description")] public string? Description { get; set; }

        [JsonPropertyName("fields")] public List<DbSchemaField> Fields { get; set; } = [];

        public override string? ToString() => Name;
    }
}
