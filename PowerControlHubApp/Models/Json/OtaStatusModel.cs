using System.Text.Json.Serialization;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

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
        String.Equals(State, OtaState_Available, StringComparison.OrdinalIgnoreCase);

    public bool IsBusy =>
        State is OtaState_Checking or OtaState_Downloading or OtaState_Rebooting;

    public bool HasFailed =>
        String.Equals(State, OtaState_Failed, StringComparison.OrdinalIgnoreCase);

    /// <summary>Human-readable label shown in the update banner.</summary>
    public string BannerLabel => State switch
    {
        string s when String.Equals(s, OtaState_Available, StringComparison.OrdinalIgnoreCase) => String.Format(OtaLabelAvailable, AvailableVersion, CurrentVersion),
        string s when String.Equals(s, OtaState_Checking, StringComparison.OrdinalIgnoreCase) => OtaLabelChecking,
        string s when String.Equals(s, OtaState_Downloading, StringComparison.OrdinalIgnoreCase) => OtaLabelDownloading,
        string s when String.Equals(s, OtaState_Rebooting, StringComparison.OrdinalIgnoreCase) => OtaLabelRebooting,
        string s when String.Equals(s, OtaState_Failed, StringComparison.OrdinalIgnoreCase) => OtaLabelFailed,
        string s when String.Equals(s, OtaState_UpToDate, StringComparison.OrdinalIgnoreCase) => String.Format(OtaLabelUptodate, CurrentVersion),
        _ => String.Empty
    };
}
