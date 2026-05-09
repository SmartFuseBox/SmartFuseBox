using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models;

/// <summary>
/// Represents a single sensor entry returned inside the "sensors" object
/// from GET /api/sensor.
/// The firmware emits sensors as a JSON object keyed by sensor name, e.g.:
///   "sensors":{ "DHT11":{ "uid":1, "idType":7, "type":0, "temperature":23.4, "humidity":55 } }
/// </summary>
public class SensorModel : INotifyPropertyChanged
{
    private Dictionary<string, JsonElement>? _extraFields;

    /// <summary>Sensor name (the JSON key). Set by the client after deserialisation.</summary>
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("uid")]
    public int Uid { get; set; }

    /// <summary>SensorIdList enum value from the firmware.</summary>
    [JsonPropertyName("idType")]
    public int IdType { get; set; }

    /// <summary>SensorType enum value: 0 = Local, 1 = Remote.</summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>Strongly-typed sensor kind derived from IdType.</summary>
    [JsonIgnore]
    public SensorType SensorType => IdType switch
    {
        0 => SensorType.Water,
        1 => SensorType.Dht11,
        2 => SensorType.Light,
        3 => SensorType.Gps,
        4 => SensorType.System,
        5 => SensorType.BinaryPresence,
        6 => SensorType.Voltage,
        _ => SensorType.Unknown
    };

    /// <summary>
    /// All remaining sensor-specific fields (temperature, humidity, waterLevel, etc.)
    /// stored as a raw JSON element so we can display them without a rigid schema.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields
    {
        get => _extraFields;
        set
        {
            _extraFields = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValueSummary));
            NotifyTypedProperties();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private double GetDouble(string key, double fallback = 0) =>
        _extraFields != null &&
        _extraFields.TryGetValue(key, out var el) &&
        el.ValueKind == JsonValueKind.Number
            ? el.GetDouble() : fallback;

    private string GetString(string key, string fallback = "") =>
        _extraFields != null &&
        _extraFields.TryGetValue(key, out var el)
            ? el.ValueKind == JsonValueKind.String
                ? el.GetString() ?? fallback
                : el.ToString()
            : fallback;

    private bool GetBool(string key, bool fallback = false)
    {
        if (_extraFields == null || !_extraFields.TryGetValue(key, out var el))
            return fallback;
        if (el.ValueKind == JsonValueKind.True)  return true;
        if (el.ValueKind == JsonValueKind.False) return false;
        if (el.ValueKind == JsonValueKind.String)
            return bool.TryParse(el.GetString(), out bool b) ? b : fallback;
        return fallback;
    }

    /// <summary>
    /// Unwrap a nested JSON object stored under <paramref name="key"/> and
    /// return a helper that reads fields from inside it.
    /// </summary>
    private double GetNestedDouble(string objectKey, string fieldKey, double fallback = 0)
    {
        if (_extraFields == null ||
            !_extraFields.TryGetValue(objectKey, out var outer) ||
            outer.ValueKind != JsonValueKind.Object)
            return fallback;

        if (outer.TryGetProperty(fieldKey, out var el) && el.ValueKind == JsonValueKind.Number)
            return el.GetDouble();
        return fallback;
    }

    private bool GetNestedBool(string objectKey, string fieldKey, bool fallback = false)
    {
        if (_extraFields == null ||
            !_extraFields.TryGetValue(objectKey, out var outer) ||
            outer.ValueKind != JsonValueKind.Object)
            return fallback;

        if (outer.TryGetProperty(fieldKey, out var el))
        {
            if (el.ValueKind == JsonValueKind.True)  return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.String)
                return bool.TryParse(el.GetString(), out bool b) ? b : fallback;
        }
        return fallback;
    }

    private long GetNestedLong(string objectKey, string fieldKey, long fallback = 0)
    {
        if (_extraFields == null ||
            !_extraFields.TryGetValue(objectKey, out var outer) ||
            outer.ValueKind != JsonValueKind.Object)
            return fallback;

        if (outer.TryGetProperty(fieldKey, out var el) && el.ValueKind == JsonValueKind.Number)
            return el.GetInt64();
        return fallback;
    }

    private void NotifyTypedProperties()
    {
        // DHT11
        OnPropertyChanged(nameof(Temperature));
        OnPropertyChanged(nameof(Humidity));
        OnPropertyChanged(nameof(DewPoint));
        OnPropertyChanged(nameof(Comfort));
        OnPropertyChanged(nameof(CondensationRisk));
        // Voltage
        OnPropertyChanged(nameof(Voltage));
        OnPropertyChanged(nameof(VoltageAvg));
        // Water
        OnPropertyChanged(nameof(WaterLevel));
        OnPropertyChanged(nameof(WaterLevelAvg));
        // Light
        OnPropertyChanged(nameof(IsDaytime));
        OnPropertyChanged(nameof(DayNightIcon));
        OnPropertyChanged(nameof(DayNightLabel));
        OnPropertyChanged(nameof(LightLevel));
        OnPropertyChanged(nameof(LightLevelAvg));
        // GPS
        OnPropertyChanged(nameof(GpsValid));
        OnPropertyChanged(nameof(GpsFixLabel));
        OnPropertyChanged(nameof(GpsLatitude));
        OnPropertyChanged(nameof(GpsLongitude));
        OnPropertyChanged(nameof(GpsAltitude));
        OnPropertyChanged(nameof(GpsSpeed));
        OnPropertyChanged(nameof(GpsCourse));
        OnPropertyChanged(nameof(GpsSatellites));
        // System
        OnPropertyChanged(nameof(FreeMemory));
        OnPropertyChanged(nameof(CpuUsage));
        // Binary presence
        OnPropertyChanged(nameof(BinaryState));
        OnPropertyChanged(nameof(BinaryStateIcon));
        OnPropertyChanged(nameof(BinaryStateLabel));
    }

    // ── DHT11 ─────────────────────────────────────────────────────────────────

    [JsonIgnore] public string Temperature       => $"{GetDouble("temperature"):F1}°C";
    [JsonIgnore] public string Humidity          => $"{GetDouble("humidity"):F0}%";
    [JsonIgnore] public string DewPoint          => $"{GetDouble("dew_point"):F1}°C";
    [JsonIgnore] public string Comfort           => GetString("comfort", "--");
    [JsonIgnore] public string CondensationRisk  => GetString("condensation_risk", "--");

    // ── Voltage ───────────────────────────────────────────────────────────────

    [JsonIgnore] public string Voltage    => $"{GetDouble("voltage"):F2} V";
    [JsonIgnore] public string VoltageAvg => $"{GetDouble("avg"):F2} V";

    // ── Water ─────────────────────────────────────────────────────────────────

    [JsonIgnore] public string WaterLevel    => $"{GetDouble("level"):F0}";
    [JsonIgnore] public string WaterLevelAvg => $"{GetDouble("average"):F0}";

    // ── Light ─────────────────────────────────────────────────────────────────

    [JsonIgnore] public bool   IsDaytime      => GetBool("isDaytime", true);
    [JsonIgnore] public string DayNightIcon   => IsDaytime ? "☀️" : "🌙";
    [JsonIgnore] public string DayNightLabel  => IsDaytime ? "Day" : "Night";
    [JsonIgnore] public string LightLevel     => GetDouble("lightLevel").ToString("F0");
    [JsonIgnore] public string LightLevelAvg  => GetDouble("avgLightLevel").ToString("F0");

    // ── GPS — payload is nested: "gps":{ "lat":..., "lon":..., ... } ─────────

    [JsonIgnore] public bool   GpsValid      => GetNestedBool("gps", "valid");
    [JsonIgnore] public string GpsFixLabel   => GpsValid ? "✅ Fix" : "❌ No Fix";
    [JsonIgnore] public string GpsLatitude   => $"{GetNestedDouble("gps", "lat"):F6}°";
    [JsonIgnore] public string GpsLongitude  => $"{GetNestedDouble("gps", "lon"):F6}°";
    [JsonIgnore] public string GpsAltitude   => $"{GetNestedDouble("gps", "alt"):F1} m";
    [JsonIgnore] public string GpsSpeed      => $"{GetNestedDouble("gps", "speed"):F1} kn";
    [JsonIgnore] public string GpsCourse     => $"{GetNestedDouble("gps", "course"):F1}°";
    [JsonIgnore] public string GpsSatellites => GetNestedLong("gps", "sats").ToString();

    // ── System ────────────────────────────────────────────────────────────────

    [JsonIgnore] public string FreeMemory => $"{GetDouble("freeMemory"):F0} B";
    [JsonIgnore] public string CpuUsage   => $"{GetDouble("cpuUsage"):F0}%";

    // ── Binary presence ───────────────────────────────────────────────────────

    [JsonIgnore] public string BinaryState      => GetString("state", "clear");
    [JsonIgnore] public string BinaryStateIcon  => BinaryState == "detected" ? "🔴" : "🟢";
    [JsonIgnore] public string BinaryStateLabel => BinaryState == "detected" ? "Detected" : "Clear";

    // ── Generic / fallback ────────────────────────────────────────────────────

    /// <summary>
    /// Returns a human-readable summary of the sensor's current value(s).
    /// Used by the generic fallback template only.
    /// </summary>
    public string ValueSummary
    {
        get
        {
            if (ExtraFields == null || ExtraFields.Count == 0)
                return "--";

            var parts = new List<string>();
            foreach (var kv in ExtraFields)
            {
                string label = FormatFieldLabel(kv.Key);
                string val = FormatValue(kv.Key, kv.Value);
                if (!string.IsNullOrEmpty(val))
                    parts.Add($"{label}: {val}");
            }

            return parts.Count > 0 ? string.Join("  |  ", parts) : "--";
        }
    }

    private static string FormatFieldLabel(string key) => key switch
    {
        "temperature"      => "Temp",
        "humidity"         => "Humidity",
        "waterLevel"       => "Water",
        "light"            => "Light",
        "voltage"          => "Voltage",
        "speed"            => "Speed",
        "bearing"          => "Bearing",
        "altitude"         => "Altitude",
        _                  => key
    };

    private static string FormatValue(string key, JsonElement element)
    {
        return (key, element.ValueKind) switch
        {
            ("temperature", JsonValueKind.Number) => $"{element.GetDouble():F1}°C",
            ("humidity",    JsonValueKind.Number) => $"{element.GetDouble():F0}%",
            ("voltage",     JsonValueKind.Number) => $"{element.GetDouble():F2}V",
            ("speed",       JsonValueKind.Number) => $"{element.GetDouble():F1} kn",
            (_,             JsonValueKind.Number) => element.GetDouble().ToString("G4"),
            (_,             JsonValueKind.String) => element.GetString() ?? string.Empty,
            (_,             JsonValueKind.True)   => "Yes",
            (_,             JsonValueKind.False)  => "No",
            _                                     => string.Empty
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
