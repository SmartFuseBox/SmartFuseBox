using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class SoundModel
    {
        [JsonPropertyName("active")]
        public int Active { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }
    }
}
