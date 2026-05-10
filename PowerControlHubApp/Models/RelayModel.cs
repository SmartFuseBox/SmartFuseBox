using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Models;

/// <summary>
/// Represents a single relay returned by GET /api/relay.
/// JSON field names match what the ESP32 firmware emits via RelayNetworkHandler.
/// </summary>
public class RelayModel : INotifyPropertyChanged
{
    private int _state;

    /// <summary>Zero-based index within the relay array (set by the client after deserialisation).</summary>
    [JsonIgnore]
    public int Index { get; set; }

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("longName")]
    public string LongName { get; set; } = string.Empty;

    /// <summary>Hardware pin number. 255 (0xFF) means not configured / disabled.</summary>
    [JsonPropertyName("pin")]
    public int Pin { get; set; }

    [JsonPropertyName("img")]
    public int ButtonImage { get; set; }

    [JsonPropertyName("defaultState")]
    public int DefaultState { get; set; }

    /// <summary>0 = Default, 1 = Horn, 2 = NightRelay</summary>
    [JsonPropertyName("actionType")]
    public int ActionType { get; set; }

    /// <summary>Current state: 1 = on, 0 = off.</summary>
    [JsonPropertyName("state")]
    public int State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOn));
        }
    }

    [JsonIgnore]
    public bool IsEnabled => Pin != 255;

    [JsonIgnore]
    public bool IsOn => State == 1;

    [JsonIgnore]
    public string DisplayName => !string.IsNullOrWhiteSpace(LongName) ? LongName : ShortName;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
