using PowerControlHubApp.Models.Json;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models
{
    public sealed class ConfigModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("spiPins")]
        public SpipinsModel SpiPins { get; set; }

        [JsonPropertyName("vesselType")]
        public int VesselType { get; set; }

        [JsonPropertyName("hornRelayIndex")]
        public int HornRelayIndex { get; set; }

        [JsonPropertyName("soundStartDelayMs")]
        public int SoundStartDelayMs { get; set; }

        [JsonPropertyName("bluetoothEnabled")]
        public bool BluetoothEnabled { get; set; }

        [JsonPropertyName("wifiEnabled")]
        public bool WifiEnabled { get; set; }

        [JsonPropertyName("accessMode")]
        public int AccessMode { get; set; }

        [JsonPropertyName("apSSID")]
        public string ApSSID { get; set; }

        [JsonPropertyName("apPassword")]
        public string ApPassword { get; set; }

        [JsonPropertyName("wifiPort")]
        public int WifiPort { get; set; }

        [JsonPropertyName("wifiState")]
        public int WifiState { get; set; }

        [JsonPropertyName("apIpAddress")]
        public string ApIpAddress { get; set; }

        [JsonPropertyName("timezoneOffset")]
        public int TimezoneOffset { get; set; }

        [JsonPropertyName("mmsi")]
        public string Mmsi { get; set; }

        [JsonPropertyName("callSign")]
        public string CallSign { get; set; }

        [JsonPropertyName("homePort")]
        public string HomePort { get; set; }

        [JsonPropertyName("ledColors")]
        public LedColorsModel LedColors { get; set; }

        [JsonPropertyName("ledBrightness")]
        public LedbrightnessModel LedBrightness { get; set; }

        [JsonPropertyName("ledAutoSwitch")]
        public bool LedAutoSwitch { get; set; }

        [JsonPropertyName("ledEnable")]
        public LedEnableModel LedEnable { get; set; }

        [JsonPropertyName("soundConfig")]
        public SoundConfigModel SoundConfig { get; set; }

        [JsonPropertyName("sdCardInitializeSpeed")]
        public int SdCardInitializeSpeed { get; set; }

        [JsonPropertyName("sdCardCsPin")]
        public int SdCardCsPin { get; set; }

        [JsonPropertyName("rtcPins")]
        public RtcConfigModel Rtc { get; set; }

        [JsonPropertyName("xpdzPin")]
        public int XpdzTonePin { get; set; }

        [JsonPropertyName("locationType")]
        public int LocationType { get; set; }
    }
}
