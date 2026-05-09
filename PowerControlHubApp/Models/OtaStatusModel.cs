using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models;

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
    public string State { get; set; } = "idle";

    /// <summary>"1" when the device will auto-apply updates.</summary>
    [JsonPropertyName("auto")]
    public string Auto { get; set; } = "0";

    // ── Computed helpers ──────────────────────────────────────────────────────

    public bool UpdateAvailable =>
        string.Equals(State, "available", StringComparison.OrdinalIgnoreCase);

    public bool IsBusy =>
        State is "checking" or "downloading" or "rebooting";

    public bool HasFailed =>
        string.Equals(State, "failed", StringComparison.OrdinalIgnoreCase);

    /// <summary>Human-readable label shown in the update banner.</summary>
    public string BannerLabel => State switch
    {
        "available"   => $"Update available: {AvailableVersion}  (installed: {CurrentVersion})",
        "checking"    => "Checking for firmware update…",
        "downloading" => "Downloading firmware update…",
        "rebooting"   => "Applying update — device rebooting…",
        "failed"      => "Firmware update failed. Tap to retry.",
        "uptodate"    => $"Firmware is up to date ({CurrentVersion})",
        _             => string.Empty
    };
}
