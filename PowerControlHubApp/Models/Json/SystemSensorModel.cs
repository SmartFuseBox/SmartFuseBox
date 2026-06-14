using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class SystemSensorModel
    {
        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("idType")]
        public int IdType { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("freeMemory")]
        public int FreeMemory { get; set; }

        [JsonPropertyName("cpuUsage")]
        public int CpuUsage { get; set; }
    }
}
