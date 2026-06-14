using PowerControlHubApp.Converters;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class SensorsModel
    {
        [JsonPropertyName("system")]
        [JsonConverter(typeof(SingleOrArrayConverter<SystemSensorModel>))]
        public SystemSensorModel[] System { get; set; }
        public object Name { get; internal set; }
        public object ExtraFields { get; internal set; }
        public SensorType SensorType { get; internal set; }
    }
}
