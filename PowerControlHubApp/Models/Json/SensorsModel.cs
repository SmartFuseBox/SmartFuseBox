using System.Text.Json;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

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
                string s = el.GetString();
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
                if (TryGet(ExtraFields, JsonSensorTemperature, out JsonElement el))
                {
                    string s = FormatDouble(el, SensorFmtTempDouble);
                    return string.IsNullOrEmpty(s) ? string.Empty : s + SensorSuffixCelsius;
                }

                return string.Empty;
            }
        }

        public string Humidity
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorHumidity, out JsonElement el))
                {
                    string s = FormatDouble(el, SensorFmtTempDouble);
                    return string.IsNullOrEmpty(s) ? string.Empty : s + SuffixPercent;
                }

                return string.Empty;
            }
        }

        public string DewPoint
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorDewPoint, out JsonElement el))
                {
                    string s = FormatDouble(el, SensorFmtTempDouble);
                    return string.IsNullOrEmpty(s) ? string.Empty : s + SensorSuffixCelsius;
                }

                return string.Empty;
            }
        }

        public string Comfort
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorComfort, out JsonElement el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;

                return string.Empty;
            }
        }

        public string CondensationRisk
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorCondensationRisk, out JsonElement el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;

                return string.Empty;
            }
        }

        // --- Light ---
        public string DayNightIcon
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorIsDaytime, out JsonElement el) && TryGetBool(el, out bool isDay))
                    return isDay ? IconSun : IconMoon;

                return string.Empty;
            }
        }

        public string DayNightLabel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorIsDaytime, out JsonElement el) && TryGetBool(el, out bool isDay))
                    return isDay ? LabelDay : LabelNight;

                return string.Empty;
            }
        }

        public string LightLevel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorLightLevel, out JsonElement el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        public string LightLevelAvg
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorAvgLightLevel, out JsonElement el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        // --- GPS ---
        private JsonElement? GpsObject
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorGps, out JsonElement el) && el.ValueKind == JsonValueKind.Object)
                    return el;

                return null;
            }
        }

        public string GpsLatitude
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsLat, out JsonElement el))
                    return FormatDouble(el, SensorFmtGpsCoord);

                return string.Empty;
            }
        }

        public string GpsLongitude
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsLon, out JsonElement el))
                    return FormatDouble(el, SensorFmtGpsCoord);

                return string.Empty;
            }
        }

        public string GpsAltitude
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsAlt, out JsonElement el))
                    return $"{FormatDouble(el, SensorFmtDouble2)} {SuffixMetre}";

                return string.Empty;
            }
        }

        public string GpsSpeed
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsSpeed, out JsonElement el))
                    return $"{FormatDouble(el, SensorFmtDouble2)} {SuffixKmh}";

                return string.Empty;
            }
        }

        public string GpsCourse
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsCourse, out JsonElement el))
                    return $"{FormatDouble(el, SensorFmtDouble2)} {SuffixDegree}";

                return string.Empty;
            }
        }

        public string GpsSatellites
        {
            get
            {
                if (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsSats, out JsonElement el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        public string GpsFixLabel => (GpsObject.HasValue && TryGet(GpsObject, JsonSensorGpsValid, out JsonElement el) && TryGetBool(el, out bool v) && v) ? LabelFix : LabelNoFix;

        // --- System ---
        public string FreeMemory
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorFreeMemory, out JsonElement el) && el.TryGetInt32(out int mem))
                    return $"{Math.Round((double)mem / KilobyteBytes, 0)}{SuffixKb}";

                return string.Empty;
            }
        }

        public string CpuUsage
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorCpuUsage, out JsonElement el) && el.TryGetInt32(out int cpu))
                    return $"{cpu}{SuffixPercent}";

                return string.Empty;
            }
        }

        // --- Binary presence ---
        public string BinaryStateIcon
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorState, out JsonElement el) && el.ValueKind == JsonValueKind.String)
                {
                    string s = el.GetString();

                    if (s == SensorStateDetected)
                        return IconRedCircle;

                    if (s == SensorStateClear)
                        return IconGreenCircle;
                }

                return string.Empty;
            }
        }

        public string BinaryStateLabel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorState, out JsonElement el) && el.ValueKind == JsonValueKind.String)
                {
                    string s = el.GetString();

                    if (s == SensorStateDetected)
                        return LabelDetected;

                    if (s == SensorStateClear)
                        return LabelClear;

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
                if (TryGet(ExtraFields, JsonSensorVoltage, out JsonElement el))
                    return FormatDouble(el, SensorFmtDouble2) + SuffixVolt;

                return string.Empty;
            }
        }

        public string VoltageAvg
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorVoltageAvg, out JsonElement el))
                    return FormatDouble(el, SensorFmtDouble2) + SuffixVolt;

                return string.Empty;
            }
        }

        // --- Water ---
        public string WaterLevel
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorWaterLevel, out JsonElement el))
                    return FormatInt(el);

                return string.Empty;
            }
        }

        public string WaterLevelAvg
        {
            get
            {
                if (TryGet(ExtraFields, JsonSensorWaterLevelAvg, out JsonElement el))
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

                List<string> parts = [];

                foreach (JsonProperty p in ExtraFields.Value.EnumerateObject())
                {
                    if (p.NameEquals(JsonSensorUid) || p.NameEquals(JsonSensorIdType) || p.NameEquals(JsonSensorType))
                        continue;

                    if (p.Value.ValueKind == JsonValueKind.Object)
                    {
                        // Flatten small nested objects (gps)
                        foreach (JsonProperty sub in p.Value.EnumerateObject())
                        {
                            parts.Add($"{sub.Name}={sub.Value.ToString()}");
                        }
                    }
                    else
                    {
                        parts.Add($"{p.Name}={p.Value.ToString()}");
                    }
                }

                return string.Join(SummarySeparator, parts);
            }
        }
    }
}
