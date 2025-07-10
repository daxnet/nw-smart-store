using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Nss.ApiService.Models
{
    public sealed class QueryModel
    {
        [JsonPropertyName("query")]
        [Required]
        public string? Query { get; set; }
    }
}
