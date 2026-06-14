using PowerControlHubApp.Internal;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models.Json
{
    public sealed class RelayModel
    {
        [JsonIgnore]
        public int Index { get; set; }

        [JsonPropertyName("shortName")]
        public string ShortName { get; set; }

        [JsonPropertyName("longName")]
        public string LongName { get; set; }

        [JsonPropertyName("pin")]
        public int Pin { get; set; }

        [JsonPropertyName("img")]
        public int ButtonImage { get; set; }

        [JsonPropertyName("defaultState")]
        public int DefaultState { get; set; }

        [JsonPropertyName("actionType")]
        public int ActionType { get; set; }

        [JsonPropertyName("state")]
        public int State { get; set; }

        public int LinkedIndex { get; internal set; }

        [JsonIgnore]
        public bool IsOn => State == 1;

        public bool IsEnabled => Pin < Constants.UnconfiguredPin;
    }
}
