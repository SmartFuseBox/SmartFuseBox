using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{

    public sealed class IndexModel
    {
        [JsonPropertyName("system")]
        public SystemModel System { get; set; }

        [JsonPropertyName("config")]
        public ConfigModel Config { get; set; }

        [JsonPropertyName("relays")]
        public List<RelayModel> Relays { get; set; }

        [JsonPropertyName("homeMap")]
        public int[] HomeMap { get; set; }

        [JsonPropertyName("linked")]
        public int[][] Linked { get; set; }

        [JsonPropertyName("sound")]
        public SoundModel Sound { get; set; }

        [JsonPropertyName("warning")]
        public WarningModel Warning { get; set; }

        [JsonPropertyName("sensors")]
        public SensorsModel Sensors { get; set; }

        /// <summary>
        /// Populated from the sensors JSON object — contains one entry per sensor
        /// with Name, SensorType, and ExtraFields set.
        /// </summary>
        [JsonIgnore]
        public List<SensorsModel> SensorsList { get; set; } = [];

        [JsonPropertyName("schedule")]
        public ScheduleModel Schedule { get; set; }

        [JsonPropertyName("externalSensors")]
        public ExternalSensorsModel ExternalSensors { get; set; }
    }
}
