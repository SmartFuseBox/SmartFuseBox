using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class LedColorsModel
    {
        [JsonPropertyName("dayGood")]
        public int[] DayGood { get; set; }

        [JsonPropertyName("dayBad")]
        public int[] DayBad { get; set; }

        [JsonPropertyName("nightGood")]
        public int[] NightGood { get; set; }

        [JsonPropertyName("nightBad")]
        public int[] NightBad { get; set; }
    }
}
