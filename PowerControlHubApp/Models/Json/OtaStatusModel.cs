using System.Text.Json.Serialization;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Models.Json;

/// <summary>
/// Firmware OTA state returned by GET /api/system/F13.
/// Response shape: {"v":"0.9.0.3","av":"0.9.1.0","s":"available","auto":"0"}
/// </summary>
public class OtaStatusModel
{
    /// <summary>Currently installed firmware version, e.g. "v0.9.0.3".</summary>
    [JsonPropertyName("v")]
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>Available firmware version, empty when none found yet.</summary>
    [JsonPropertyName("av")]
    public string AvailableVersion { get; set; } = string.Empty;

    /// <summary>
    /// OTA state string from the firmware:
    /// idle | checking | available | downloading | rebooting | failed | uptodate
    /// </summary>
    [JsonPropertyName("s")]
    public string State { get; set; } = OtaState_Idle;

    /// <summary>"1" when the device will auto-apply updates.</summary>
    [JsonPropertyName("auto")]
    public string Auto { get; set; } = OtaAuto_Off;

    public bool UpdateAvailable =>
        string.Equals(State, OtaState_Available, StringComparison.OrdinalIgnoreCase);

    public bool IsBusy =>
        State is OtaState_Checking or OtaState_Downloading or OtaState_Rebooting;

    public bool HasFailed =>
        string.Equals(State, OtaState_Failed, StringComparison.OrdinalIgnoreCase);

    /// <summary>Human-readable label shown in the update banner.</summary>
    public string BannerLabel => State switch
    {
        var s when string.Equals(s, OtaState_Available, StringComparison.OrdinalIgnoreCase) => string.Format(OtaLabel_Available, AvailableVersion, CurrentVersion),
        var s when string.Equals(s, OtaState_Checking, StringComparison.OrdinalIgnoreCase) => OtaLabel_Checking,
        var s when string.Equals(s, OtaState_Downloading, StringComparison.OrdinalIgnoreCase) => OtaLabel_Downloading,
        var s when string.Equals(s, OtaState_Rebooting, StringComparison.OrdinalIgnoreCase) => OtaLabel_Rebooting,
        var s when string.Equals(s, OtaState_Failed, StringComparison.OrdinalIgnoreCase) => OtaLabel_Failed,
        var s when string.Equals(s, OtaState_UpToDate, StringComparison.OrdinalIgnoreCase) => string.Format(OtaLabel_Uptodate, CurrentVersion),
        _ => string.Empty
    };
}
