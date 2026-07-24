using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models;

/// <summary>
/// Describes one field (pin, option1, or option2) within a sensor type descriptor,
/// enabling the UI to render appropriate labels and input constraints dynamically.
/// </summary>
public sealed class SensorFieldDescriptorModel
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("default")]
    public int Default { get; set; }
}

/// <summary>
/// Describes one sensor type (e.g. Water Sensor, DHT11, Voltage Sensor),
/// including its available pin slots and option fields.
/// </summary>
public sealed class SensorTypeDescriptorModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pins")]
    public List<SensorFieldDescriptorModel> Pins { get; set; } = [];

    [JsonPropertyName("options1")]
    public List<SensorFieldDescriptorModel> Options1 { get; set; } = [];

    [JsonPropertyName("options2")]
    public List<SensorFieldDescriptorModel> Options2 { get; set; } = [];
}

/// <summary>
/// Container for the meta block returned by S0?meta=1.
/// </summary>
public sealed class SensorMetaResponseModel
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("descriptors")]
    public List<SensorTypeDescriptorModel> Descriptors { get; set; } = [];
}
