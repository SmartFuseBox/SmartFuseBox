using System.Text.Json;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Models.Json
{
    /// <summary>
    /// Represents a single sensor entry from the device /api/index sensors object.
    /// ExtraFields contains the raw JSON object for the sensor and we expose
    /// convenience computed properties for the XAML templates to bind to.
    /// </summary>
    public sealed class SensorsModel
    {
        /// <summary>
        /// Sensor UID from the device ("uid" JSON property).
        /// </summary>
        public int Uid { get; set; }

        /// <summary>
        /// Raw numeric idType (matches firmware SensorIdList values).
        /// </summary>
        public byte IdType { get; set; }

        /// <summary>
        /// Sensor name (key in the sensors JSON object from /api/index).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Raw JSON element containing all sensor-specific fields.
        /// </summary>
        public JsonElement? ExtraFields { get; set; }

        /// <summary>
        /// Sensor type id mapped to the <see cref="SensorType"/> enum.
        /// This is set by the caller using the firmware's idType value.
        /// </summary>
        public SensorType SensorType { get; set; } = SensorType.Unknown;

        // --- Helper accessors for reading fields from ExtraFields ---
        private static bool TryGet(JsonElement? src, string name, out JsonElement val)
        {
            val = default;

            if (!src.HasValue)
                return false;

            return src.Value.TryGetProperty(name, out val);
        }

        private static string FormatDouble(JsonElement el, string format)
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out double d))
                return d.ToString(format);

            if (el.ValueKind == JsonValueKind.String)
            {
                var s = el.GetString();
                return s ?? string.Empty;
            }

            return string.Empty;
        }

        private static string FormatInt(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int i))
                return i.ToString();

            if (el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? string.Empty;

            return string.Empty;
        }

        private static bool TryGetBool(JsonElement el, out bool v)
        {
            v = false;

            if (el.ValueKind == JsonValueKind.True)
            {
                v = true;
                return true;
            }

            if (el.ValueKind == JsonValueKind.False)
            {
                v = false;
                return true;
            }

            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int i))
            {
                v = i != 0;
                return true;
            }

            if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out bool b))
            {
                v = b;
                return true;
            }

            return false;
        }

        // --- DHT11 ---
        public string Temperature
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorTemperature, out var el))
                {
                    var s = FormatDouble(el, SensorFmtTempDouble);
                    return string.IsNullOrEmpty(s) ? string.Empty : s + SensorSuffixCelsius;
                }

                return string.Empty;
            }
        }

        public string Humidity
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorHumidity, out var el))
                {
                    var s = FormatDouble(el, SensorFmtTempDouble);
                    return string.IsNullOrEmpty(s) ? string.Empty : s + SensorSuffixPercent;
                }

                return string.Empty;
            }
        }

        public string DewPoint
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorDewPoint, out var el))
                {
                    var s = FormatDouble(el, SensorFmtTempDouble);
                    return string.IsNullOrEmpty(s) ? string.Empty : s + SensorSuffixCelsius;
                }

                return string.Empty;
            }
        }

        public string Comfort
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorComfort, out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;

                return string.Empty;
            }
        }

        public string CondensationRisk
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorCondensationRisk, out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;

                return string.Empty;
            }
        }

        // --- Light ---
        public string DayNightIcon
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorIsDaytime, out var el) && TryGetBool(el, out bool isDay))
                    return isDay ? SensorIconSun : SensorIconMoon;

                return string.Empty;
            }
        }

        public string DayNightLabel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorIsDaytime, out var el) && TryGetBool(el, out bool isDay))
                    return isDay ? SensorLabelDay : SensorLabelNight;

                return string.Empty;
            }
        }

        public string LightLevel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorLightLevel, out var el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        public string LightLevelAvg
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorAvgLightLevel, out var el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        // --- GPS ---
        private JsonElement? GpsObject
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorGps, out var el) && el.ValueKind == JsonValueKind.Object)
                    return el;

                return null;
            }
        }

        public string GpsLatitude
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsLat, out var el))
                    return FormatDouble(el, SensorFmtGpsCoord);

                return string.Empty;
            }
        }

        public string GpsLongitude
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsLon, out var el))
                    return FormatDouble(el, SensorFmtGpsCoord);

                return string.Empty;
            }
        }

        public string GpsAltitude
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsAlt, out var el))
                    return FormatDouble(el, SensorFmtDouble2) + SensorSuffixMetre;

                return string.Empty;
            }
        }

        public string GpsSpeed
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsSpeed, out var el))
                    return FormatDouble(el, SensorFmtDouble2) + SensorSuffixKmh;

                return string.Empty;
            }
        }

        public string GpsCourse
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsCourse, out var el))
                    return FormatDouble(el, SensorFmtDouble2) + SensorSuffixDegree;

                return string.Empty;
            }
        }

        public string GpsSatellites
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsSats, out var el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        public string GpsFixLabel => (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsValid, out var el) && TryGetBool(el, out bool v) && v) ? SensorLabelFix : SensorLabelNoFix;

        // --- System ---
        public string FreeMemory
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorFreeMemory, out var el) && el.TryGetInt32(out int mem))
                    return $"{Math.Round((double)mem / KilobyteBytes, 0)}{SensorSuffixKb}";

                return string.Empty;
            }
        }

        public string CpuUsage
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorCpuUsage, out var el) && el.TryGetInt32(out int cpu))
                    return $"{cpu}{SensorSuffixPercent}";

                return string.Empty;
            }
        }

        // --- Binary presence ---
        public string BinaryStateIcon
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorState, out var el) && el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();

                    if (s == SensorStateDetected)
                        return SensorIconRedCircle;

                    if (s == SensorStateClear)
                        return SensorIconGreenCircle;
                }

                return string.Empty;
            }
        }

        public string BinaryStateLabel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorState, out var el) && el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();

                    if (s == SensorStateDetected)
                        return SensorLabelDetected;

                    if (s == SensorStateClear)
                        return SensorLabelClear;

                    return s ?? string.Empty;
                }

                return string.Empty;
            }
        }

        // --- Voltage ---
        public string Voltage
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorVoltage, out var el))
                    return FormatDouble(el, SensorFmtDouble2) + SensorSuffixVolt;

                return string.Empty;
            }
        }

        public string VoltageAvg
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorVoltageAvg, out var el))
                    return FormatDouble(el, SensorFmtDouble2) + SensorSuffixVolt;

                return string.Empty;
            }
        }

        // --- Water ---
        public string WaterLevel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorWaterLevel, out var el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        public string WaterLevelAvg
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorWaterLevelAvg, out var el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        // --- Generic fallback ---
        public string ValueSummary
        {
            get
            {
                if (!ExtraFields.HasValue || ExtraFields.Value.ValueKind != JsonValueKind.Object)
                    return string.Empty;

                var parts = new List<string>();

                foreach (var p in ExtraFields.Value.EnumerateObject())
                {
                    if (p.NameEquals(JsonSensorUid) || p.NameEquals(JsonSensorIdType) || p.NameEquals(JsonSensorType))
                        continue;

                    if (p.Value.ValueKind == JsonValueKind.Object)
                    {
                        // Flatten small nested objects (gps)
                        foreach (var sub in p.Value.EnumerateObject())
                        {
                            parts.Add($"{sub.Name}={sub.Value.ToString()}");
                        }
                    }
                    else
                    {
                        parts.Add($"{p.Name}={p.Value.ToString()}");
                    }
                }

                return string.Join(SensorSummarySeparator, parts);
            }
        }
    }
}
