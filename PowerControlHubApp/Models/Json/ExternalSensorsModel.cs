using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class ExternalSensorsModel
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("sensors")]
        public List<object> Sensors { get; set; }
    }
}
