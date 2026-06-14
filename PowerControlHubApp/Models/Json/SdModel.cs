using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class SdModel
    {
        [JsonPropertyName("present")]
        public int Present { get; set; }

        [JsonPropertyName("log")]
        public int Log { get; set; }
    }
}
