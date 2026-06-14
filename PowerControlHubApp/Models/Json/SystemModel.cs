using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class SystemModel
    {
        [JsonPropertyName("mem")]
        public int Mem { get; set; }

        [JsonPropertyName("cpu")]
        public int Cpu { get; set; }

        [JsonPropertyName("bluetooth")]
        public int Bluetooth { get; set; }

        [JsonPropertyName("wifi")]
        public int Wifi { get; set; }

        [JsonPropertyName("rssi")]
        public int Rssi { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("sd")]
        public SdModel Sd { get; set; }

        [JsonPropertyName("Uptime")]
        public string Uptime { get; set; }

        [JsonPropertyName("fw")]
        public string Fw { get; set; }
    }
}
