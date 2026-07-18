using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private readonly ThemeService _themeService;
    private string _ipAddress = string.Empty;
    private string _port = DefaultDeviceIpPort;
    private string _statusMessage = string.Empty;
    private string _selectedTheme;
    private string _pinsInUseText = string.Empty;
    private bool _isPinsRefreshing;
    private int _pinsCount;
    private OtaStatusModel _otaStatus;
    private bool _isCheckingOta;

    public string IpAddress
    {
        get => _ipAddress;

        set
        {
            _ipAddress = value;
            OnPropertyChanged();
        }
    }

    public string Port
    {
        get => _port;

        set
        {
            _port = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;

        set
        {
            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

    /// <summary>Options shown in the theme Picker: "System", "Light", "Dark".</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Exposed to XAML bindings as instance property")]
    public string[] ThemeOptions => ThemeService.ThemeOptions;

    /// <summary>Applying theme immediately so the user sees a live preview.</summary>
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value)
                return;

            _selectedTheme = value;
            OnPropertyChanged();
            ThemeService.Apply(value);
        }
    }

    public string PinsInUseText
    {
        get => _pinsInUseText;
        set
        {
            _pinsInUseText = value;
            OnPropertyChanged();
        }
    }

    public int PinsCount
    {
        get => _pinsCount;
        set
        {
            _pinsCount = value;
            OnPropertyChanged();
        }
    }

    public bool IsPinsRefreshing
    {
        get => _isPinsRefreshing;
        set
        {
            _isPinsRefreshing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotPinsRefreshing));
        }
    }

    public bool IsNotPinsRefreshing => !_isPinsRefreshing;

    public bool HasPins => PinsCount > 0;

    public ICommand SaveCommand { get; }
    public ICommand RefreshPinsCommand { get; }
    public ICommand CheckForUpdateCommand { get; }
    public ICommand InstallFirmwareCommand { get; }

    public SettingsViewModel(PowerHubService service, ThemeService themeService)
    {
        _service = service;
        _themeService = themeService;
        SaveCommand = new Command(Save);
        RefreshPinsCommand = new Command(async () => await RefreshPinsAsync());
        CheckForUpdateCommand = new Command(async () => await CheckForUpdateAsync());
        InstallFirmwareCommand = new Command(async () => await InstallFirmwareAsync(), () => CanInstallFirmware);

        IpAddress = Preferences.Get(KeyDeviceIpAddress, string.Empty);
        Port = Preferences.Get(KeyDeviceIpPort, DefaultDeviceIpPort);
        _selectedTheme = ThemeService.Current;
    }

    public string CurrentFirmwareVersion => _otaStatus?.CurrentVersion ?? DoubleDash;

    public string AvailableFirmwareVersion => string.IsNullOrEmpty(_otaStatus?.AvailableVersion) ? DoubleDash : _otaStatus.AvailableVersion;

    public bool IsUpdateAvailable => _otaStatus?.UpdateAvailable == true;

    public bool IsOtaBusy => (_otaStatus?.IsBusy == true) || _isCheckingOta;

    public bool CanInstallFirmware => _otaStatus?.UpdateAvailable == true && !_isCheckingOta;

    public async Task CheckForUpdateAsync()
    {
        if (!_service.IsConfigured || _isCheckingOta)
            return;

        _isCheckingOta = true;
        try
        {
            var ota = await _service.GetOtaStatusAsync();
            _otaStatus = ota;
            NotifyOtaProperties();
        }
        catch
        {
            StatusMessage = MessageDeviceUnreachable;
        }
        finally
        {
            _isCheckingOta = false;
            OnPropertyChanged(nameof(IsOtaBusy));
            ((Command)InstallFirmwareCommand).ChangeCanExecute();
        }
    }

    public async Task InstallFirmwareAsync()
    {
        if (!CanInstallFirmware)
            return;

        string message = string.Format(OtaDialogMessage, AvailableFirmwareVersion);
        bool confirmed = await Application.Current!.MainPage!.DisplayAlertAsync(
                OtaDialogTitle,
                message,
                OtaDialogAccept,
                MsgCancel);

        if (!confirmed)
            return;

        try
        {
            var ok = await _service.TriggerOtaInstallAsync();

            if (ok)
            {
                // give the device a moment then refresh status
                await Task.Delay(TimeSpan.FromSeconds(SecondsTwo));
                await CheckForUpdateAsync();
            }
            else
            {
                StatusMessage = OtaTriggerFailed;
            }
        }
        catch
        {
            StatusMessage = OtaTriggerFailed;
        }
    }

    private void NotifyOtaProperties()
    {
        OnPropertyChanged(nameof(CurrentFirmwareVersion));
        OnPropertyChanged(nameof(AvailableFirmwareVersion));
        OnPropertyChanged(nameof(IsUpdateAvailable));
        OnPropertyChanged(nameof(IsOtaBusy));
        OnPropertyChanged(nameof(CanInstallFirmware));
        ((Command)InstallFirmwareCommand).ChangeCanExecute();
    }

    public async Task RefreshPinsAsync()
    {
        if (!_service.IsConfigured || _isPinsRefreshing)
            return;

        IsPinsRefreshing = true;

        try
        {
            var result = await _service.GetSystemPinsAsync();

            if (result?.Success == true && result.Pins?.Count > 0)
            {
                PinsCount = result.Pins.Count;
                PinsInUseText = string.Join(CommaSpace, result.Pins);
            }
            else
            {
                PinsCount = 0;
                PinsInUseText = DoubleDash;
            }
        }
        catch
        {
            PinsCount = 0;
            PinsInUseText = MessageDeviceUnreachable;
        }
        finally
        {
            IsPinsRefreshing = false;
        }
    }

    private void Save()
    {
        string ip = IpAddress.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            StatusMessage = MsgIpRequired;
            return;
        }

        if (!int.TryParse(Port.Trim(), out int port) || port < PortMin || port > PortMax)
        {
            StatusMessage = MsgInvalidPort;
            return;
        }

        Preferences.Set(KeyDeviceIpAddress, ip);
        Preferences.Set(KeyDeviceIpPort, port.ToString());

        _service.Configure(ip, port);
        StatusMessage = $"Saved. Connecting to {ip}:{port}";
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
