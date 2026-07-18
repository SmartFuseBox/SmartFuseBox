using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json;

/// <summary>
/// Response model for GET /api/system/F15 — returns the list of physical GPIO pins
/// currently assigned to any relay, sensor, or peripheral on the device.
/// </summary>
public class SystemPinsResponseModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; }

    [JsonPropertyName("pins")]
    public List<int> Pins { get; set; } = [];
}
