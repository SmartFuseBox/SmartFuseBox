using PowerControlHubApp.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class SensorsModel
    {
        [JsonPropertyName("system")]
        [JsonConverter(typeof(SingleOrArrayConverter<SystemSensorModel>))]
        public SystemSensorModel[] System { get; set; }

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
        /// </summary>
        public SensorType SensorType { get; set; } = SensorType.Unknown;
    }
}
