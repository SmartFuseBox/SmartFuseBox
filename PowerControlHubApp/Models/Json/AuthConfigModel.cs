using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class AuthConfigModel
    {
        [JsonPropertyName("e")]
        public bool Enabled { get; set; }

        [JsonPropertyName("k")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("h")]
        public string HmacKey { get; set; } = string.Empty;
    }
}
