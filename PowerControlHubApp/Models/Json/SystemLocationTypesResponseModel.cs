using PowerControlHubApp.Internal;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json;

public class LocationTypeModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; } = Constants.RtcPinDisabled;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    public bool IsBoat => Type == 0;
}

public class SystemLocationTypesResponseModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("locations")]
    public List<LocationTypeModel> Locations { get; set; } = [];
}
