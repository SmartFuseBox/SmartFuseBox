using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json;

/// <summary>
/// Response model for GET /api/system/F16 — returns the compile-time pin restriction
/// table categorised as Hard (always blocked) and Advisory (strapping/UART/JTAG pins).
/// </summary>
public class SystemPinRestrictionsResponseModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; }

    [JsonPropertyName("hard")]
    public List<int> Hard { get; set; } = [];

    [JsonPropertyName("advisory")]
    public List<int> Advisory { get; set; } = [];
}
