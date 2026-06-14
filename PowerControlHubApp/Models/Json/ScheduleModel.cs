using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class ScheduleModel
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("slots")]
        public int[] Slots { get; set; }
    }
}
