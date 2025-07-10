using System.Text.Json.Serialization;

namespace Nss.ApiService.Models
{
    public sealed record DbSchemaField
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        public override string? ToString() => Name;
    }
}
