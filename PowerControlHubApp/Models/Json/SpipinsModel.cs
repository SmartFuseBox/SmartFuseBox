using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class SpipinsModel
    {
        [JsonPropertyName("sck")]
        public int Sck { get; set; }

        [JsonPropertyName("mosi")]
        public int Mosi { get; set; }

        [JsonPropertyName("miso")]
        public int Miso { get; set; }
    }
}
