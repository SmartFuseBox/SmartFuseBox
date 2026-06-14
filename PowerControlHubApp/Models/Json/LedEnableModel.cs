using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class LedEnableModel
    {
        [JsonPropertyName("gps")]
        public bool Gps { get; set; }

        [JsonPropertyName("warning")]
        public bool Warning { get; set; }

        [JsonPropertyName("system")]
        public bool System { get; set; }
    }
}
