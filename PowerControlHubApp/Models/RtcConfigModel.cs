using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class RtcConfigModel
    {
        [JsonPropertyName("dat")]
        public int Dat { get; set; }

        [JsonPropertyName("clk")]
        public int Clk { get; set; }

        [JsonPropertyName("rst")]
        public int Rst { get; set; }
    }
}
