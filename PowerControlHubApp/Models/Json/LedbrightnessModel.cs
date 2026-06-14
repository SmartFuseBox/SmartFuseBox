using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class LedbrightnessModel
    {
        [JsonPropertyName("day")]
        public int Day { get; set; }

        [JsonPropertyName("night")]
        public int Night { get; set; }
    }
}
