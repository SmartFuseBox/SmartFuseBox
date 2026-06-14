using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class WarningModel
    {
        [JsonPropertyName("active")]
        public string Active { get; set; }
    }
}
