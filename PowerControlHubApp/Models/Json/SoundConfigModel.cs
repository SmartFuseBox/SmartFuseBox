using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class SoundConfigModel
    {
        [JsonPropertyName("goodPreset")]
        public int GoodPreset { get; set; }

        [JsonPropertyName("goodToneHz")]
        public int GoodToneHz { get; set; }

        [JsonPropertyName("goodDurationMs")]
        public int GoodDurationMs { get; set; }

        [JsonPropertyName("badPreset")]
        public int BadPreset { get; set; }

        [JsonPropertyName("badToneHz")]
        public int BadToneHz { get; set; }

        [JsonPropertyName("badDurationMs")]
        public int BadDurationMs { get; set; }

        [JsonPropertyName("badRepeatMs")]
        public int BadRepeatMs { get; set; }
    }
}
