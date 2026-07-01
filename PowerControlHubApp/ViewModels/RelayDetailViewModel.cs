using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

[QueryProperty(nameof(RelayIndex), "relayIndex")]
public class RelayDetailViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private readonly IDashboardProvider _provider;
    private readonly RelayStore _relayStore;

    private RelayViewModel _original;

    private int _relayIndex = -1;
    private string _shortName = string.Empty;
    private string _longName = string.Empty;
    private int _pin = UnconfiguredPin;
    private int _defaultState;
    private int _colorIndex = UnconfiguredPin;
    private int _actionType;
    private int _linkedIndex = UnconfiguredPin;
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    public ObservableCollection<string> ColorOptions { get; } = new(ColorOptionNames);

    public ObservableCollection<string> ActionOptions { get; } = new(ActionOptionNames);

    public ObservableCollection<string> LinkedRelayOptions { get; } = [];

    public ObservableCollection<string> DefaultStateOptions { get; } = new(DefaultStateOptionNames);

    public ICommand SaveCommand { get; }

    public ICommand SetColorCommand { get; }

    public int RelayIndex
    {
        get => _relayIndex;
        set
        {
            _relayIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitle));
            _ = LoadRelayAsync(value);
        }
    }

    public string PageTitle => _relayIndex >= 0 ? $"Relay {_relayIndex}" : RelayPageTitle;

    public string ShortName
    {
        get => _shortName;

        set 
        { _shortName = value; 
            OnPropertyChanged(); 
        }
    }

    public string LongName
    {
        get => _longName;

        set 
        { 
            _longName = value; 
            OnPropertyChanged(); 
        }
    }

    public int Pin
    {
        get => _pin;

        set 
        { 
            _pin = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(PinDisplay));
        }
    }

    public string PinDisplay
    {
        get => _pin == UnconfiguredPin ? string.Empty : _pin.ToString();

        set
        {
            if (int.TryParse(value, out int p))
                Pin = p;
            else if (string.IsNullOrWhiteSpace(value))
                Pin = UnconfiguredPin;
        }
    }

    /// <summary>Selected index into <see cref="DefaultStateOptions"/> (0=Off, 1=On).</summary>
    public int DefaultStateIndex
    {
        get => _defaultState;
        set 
        { 
            _defaultState = value; 
            OnPropertyChanged(); 
        }
    }

    /// <summary>Selected index into <see cref="ColorOptions"/> (0‥5 = colour, 6 = none/255).</summary>
    public int SelectedColorIndex
    {
        get => _colorIndex == UnconfiguredPin ? ColorOptionNoneIndex : _colorIndex;
        set
        {
            _colorIndex = value == ColorOptionNoneIndex ? UnconfiguredPin : value;
            OnPropertyChanged();
        }
    }

    /// <summary>Selected index into <see cref="ActionOptions"/> (mirrors ActionType directly).</summary>
    public int SelectedActionIndex
    {
        get => _actionType;
        set
        {
            _actionType = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Selected index into <see cref="LinkedRelayOptions"/>.
    /// Index 0 = "None" (255), indices 1‥8 = relay 0‥7 (excluding self).
    /// </summary>
    public int SelectedLinkedIndex
    {
        get
        {
        if (_linkedIndex == UnconfiguredPin)
                return 0;

            // Account for the self-relay being absent from the list
            return _linkedIndex < _relayIndex ? _linkedIndex + 1 : _linkedIndex;
        }

        set
        {
            if (value == 0)
            {
                _linkedIndex = UnconfiguredPin;
            }
            else
            {
                // Re-map back, skipping self
                int mapped = value - 1;
                _linkedIndex = mapped >= _relayIndex ? mapped + 1 : mapped;
            }
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;

        set 
        { 
            _isBusy = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsNotBusy)); 
        }
    }

    public bool IsNotBusy => !_isBusy;

    public string StatusMessage
    {
        get => _statusMessage;

        set 
        { 
            _statusMessage = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatus));
            OnPropertyChanged(nameof(IsError)); 
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    public bool IsError => HasStatus && !_statusMessage.StartsWith(CheckMark);

    public RelayDetailViewModel(PowerHubService service, IDashboardProvider provider, RelayStore relayStore)
    {
        _service = service;
        _provider = provider;
        _relayStore = relayStore;
        SaveCommand = new Command(async () => await SaveAsync(), () => IsNotBusy);

        SetColorCommand = new Command<string>(idx => 
        { 
            if (int.TryParse(idx, out int i))
                SelectedColorIndex = i; 
        });

        BuildLinkedRelayOptions(-1);
    }

    private async Task LoadRelayAsync(int index)
    {
        if (!_service.IsConfigured || index < 0)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

            try
            {
                List<RelayViewModel> relays;

                // Prefer the dashboard provider snapshot when available
                if (_provider.CurrentIndex?.Relays != null)
                {
                    relays = RelayStore.FromModels(_provider.CurrentIndex.Relays);
                }
                else if (_service.IsConfigured)
                {
                    try
                    {
                        var idx = await _service.GetDashboardDataAsync();
                        relays = RelayStore.FromModels(idx.Relays ?? []);
                    }
                    catch
                    {
                        relays = [];
                    }
                }
                else
                {
                    relays = [];
                }

            RelayViewModel relay = index < relays.Count
                ? relays[index]
                : new RelayViewModel { Index = index, Pin = UnconfiguredPin };

            relay.Index = index;
            _original = relay;

            ShortName = relay.ShortName;
            LongName = relay.LongName;
            Pin = relay.Pin;
            DefaultStateIndex = relay.DefaultState;
            // Device stores Nextion picture IDs (2-7); convert to 0-based index for the UI
            _colorIndex = relay.ButtonImage is >= NextionImageIdMin and <= NextionImageIdMax
                ? relay.ButtonImage - NextionImageIdMin
                : UnconfiguredPin;
            _actionType = relay.ActionType;
            _linkedIndex = relay.LinkedIndex;

            BuildLinkedRelayOptions(index);

            OnPropertyChanged(nameof(SelectedColorIndex));
            OnPropertyChanged(nameof(SelectedActionIndex));
            OnPropertyChanged(nameof(SelectedLinkedIndex));
            OnPropertyChanged(nameof(PinDisplay));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load relay: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildLinkedRelayOptions(int selfIndex)
    {
        LinkedRelayOptions.Clear();
        LinkedRelayOptions.Add(NoneString);

        for (int i = 0; i < RelayCount; i++)
        {
            if (i == selfIndex)
                continue;

            LinkedRelayOptions.Add($"Relay {i}");
        }
    }

    private async Task SaveAsync()
    {
        if (!_service.IsConfigured || _relayIndex < 0)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            bool ok = true;

            if (_original == null ||
                _original.ShortName != ShortName ||
                _original.LongName != LongName)
                ok &= await _service.RenameRelayAsync(_relayIndex, ShortName, LongName);

            if (_original == null || _original.Pin != Pin)
                ok &= await _service.SetRelayPinAsync(_relayIndex, Pin);

            if (_original == null || _original.DefaultState != DefaultStateIndex)
                ok &= await _service.SetRelayDefaultStateAsync(_relayIndex, DefaultStateIndex);

            if (_original == null || NormalizedButtonImage(_original.ButtonImage) != _colorIndex)
                ok &= await _service.SetRelayColorAsync(_relayIndex, _colorIndex);

            if (_original == null || _original.ActionType != _actionType)
                ok &= await _service.SetRelayActionTypeAsync(_relayIndex, _actionType);

            if (_original == null || _original.LinkedIndex != _linkedIndex)
                ok &= await _service.LinkRelayAsync(_relayIndex, _linkedIndex);

            if (ok)
                ok &= await _service.SaveSettingsAsync();

            StatusMessage = ok ? SavedOk : SavedFailed;

            if (ok && _original != null)
            {
                _original.ShortName = ShortName;
                _original.LongName = LongName;
                _original.Pin = Pin;
                _original.DefaultState = DefaultStateIndex;
                _original.ButtonImage = _colorIndex;
                _original.ActionType = _actionType;
                _original.LinkedIndex = _linkedIndex;

                // Immediately reflect changes in the dashboard's live relay collection
                _relayStore.UpdateRelay(_relayIndex, _original);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static int NormalizedButtonImage(int deviceValue)
        => deviceValue is >= NextionImageIdMin and <= NextionImageIdMax
            ? deviceValue - NextionImageIdMin
            : UnconfiguredPin;

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
