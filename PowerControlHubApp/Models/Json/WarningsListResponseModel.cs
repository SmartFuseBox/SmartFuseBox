using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class WarningsListResponseModel
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = [];
    }
}
