using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private readonly LogService _log;
    private readonly RelayStore _relayStore;
    private CancellationTokenSource _refreshCts;
    private bool _isBusy;
    private bool _isConnected;
    private string _statusMessage = string.Empty;
    private string _deviceUrl = string.Empty;

    // OTA
    private OtaStatusModel _otaStatus;
    private bool _otaSupported;

    public ObservableCollection<RelayViewModel> Relays => _relayStore.Relays;
    public ObservableCollection<SensorsModel> Sensors { get; } = [];
    public ObservableCollection<LogEntryViewModel> LogEntries => _log.Entries;

    public bool HasRelays => _isConnected && Relays.Count > 0;
    public bool HasNoRelays => _isConnected && Relays.Count == 0;
    public bool HasSensors => _isConnected && Sensors.Count > 0;
    public bool HasNoSensors => _isConnected && Sensors.Count == 0;

    public ICommand RefreshCommand { get; }
    public ICommand ToggleRelayCommand { get; }
    public ICommand InstallUpdateCommand { get; }

    /// <summary>True when the firmware reported an update is available.</summary>
    public bool UpdateAvailable => _otaStatus?.UpdateAvailable == true;

    /// <summary>True while the device is actively checking/downloading/rebooting.</summary>
    public bool OtaBusy => _otaStatus?.IsBusy == true;

    /// <summary>True when OTA is not supported by this firmware build (F13 not present).</summary>
    public bool OtaSupported
    {
        get => _otaSupported;
        private set
        {
            _otaSupported = value;
            OnPropertyChanged();
        }
    }

    private string _systemFreeMemory = DoubleDash;
    private string _systemCpuUsage = DoubleDash;
    private string _systemTime = DoubleDash;
    private string _systemFirmware = DoubleDash;
    private string _systemUptime = DoubleDash;

    public string SystemFreeMemory
    {
        get => _systemFreeMemory;

        private set
        {
            _systemFreeMemory = value;
            OnPropertyChanged();
        }
    }

    public string SystemCpuUsage
    {
        get => _systemCpuUsage;

        private set
        {
            _systemCpuUsage = value;
            OnPropertyChanged();
        }
    }

    public string SystemTime
    {
        get => _systemTime;

        private set
        {
            _systemTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SystemFirmwareAndTime));
        }
    }

    public string SystemFirmware
    {
        get => _systemFirmware;

        private set
        {
            _systemFirmware = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SystemFirmwareAndTime));
        }
    }

    /// <summary>
    /// Uptime string reported by the device.
    /// </summary>
    public string SystemUptime
    {
        get => _systemUptime;
        private set
        {
            _systemUptime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SystemFirmwareAndTime));
        }
    }

    /// <summary>
    /// Combined firmware and time display used by the status bar.
    /// </summary>
    public string SystemFirmwareAndTime => $"FW: {SystemFirmware}  •  {SystemUptime}";

    /// <summary>Label shown in the update banner.</summary>
    public string OtaBannerLabel => _otaStatus?.BannerLabel ?? string.Empty;

    /// <summary>True when the update banner should be visible.</summary>
    public bool ShowOtaBanner =>
        _otaSupported &&
        (_otaStatus?.UpdateAvailable == true ||
         _otaStatus?.IsBusy == true ||
         _otaStatus?.HasFailed == true);

    /// <summary>Accent colour for the banner (amber = available, red = failed, blue = busy).</summary>
    public Color OtaBannerColor => _otaStatus switch
    {
        { HasFailed: true } => Color.FromArgb(ColorError),
        { IsBusy: true } => Color.FromArgb(ColorBusy),
        { UpdateAvailable: true } => Color.FromArgb(ColorWarning),
        _ => Color.FromArgb(ColorWarning)
    };

    /// <summary>Install button is active only when an update is available and nothing is in progress.</summary>
    public bool CanInstallUpdate => _otaStatus?.UpdateAvailable == true && !OtaBusy && !IsBusy;

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

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDisconnected));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(HasRelays));
            OnPropertyChanged(nameof(HasNoRelays));
            OnPropertyChanged(nameof(HasSensors));
            OnPropertyChanged(nameof(HasNoSensors));
        }
    }

    public bool IsDisconnected => !_isConnected;

    public Color StatusColor => _isConnected ? Color.FromArgb(ColorAsHex1) : Color.FromArgb(ColorError);

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string DeviceUrl
    {
        get => _deviceUrl;
        set
        {
            _deviceUrl = value;
            OnPropertyChanged();
        }
    }

    // When true the view should ignore Switch.Toggled events because the
    // viewmodel is applying authoritative state from the remote device.
    public bool IsApplyingRemoteState { get; private set; } = true;

    public DashboardViewModel(PowerHubService service, LogService log, RelayStore relayStore)
    {
        _service = service;
        _log = log;
        _relayStore = relayStore;
        RefreshCommand = new Command(async () => await RefreshAsync());
        ToggleRelayCommand = new Command<RelayViewModel>(async relay => await ToggleRelayAsync(relay));
        InstallUpdateCommand = new Command(async () => await InstallUpdateAsync(), () => CanInstallUpdate);

        _relayStore.Relays.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasRelays));
            OnPropertyChanged(nameof(HasNoRelays));
        };

        Sensors.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSensors));
            OnPropertyChanged(nameof(HasNoSensors));
        };
    }

    public void ClearLog() => _log.Clear();

    public void StartAutoRefresh()
    {
        StopAutoRefresh();
        _refreshCts = new CancellationTokenSource();
        _ = AutoRefreshLoopAsync(_refreshCts.Token);
    }

    public void StopAutoRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
    }

    public async Task RefreshAsync()
    {
        if (!_service.IsConfigured)
        {
            StatusMessage = MessageNotConfigured;
            IsConnected = false;
            return;
        }

        if (IsBusy)
            return;

        IsBusy = true;
        DeviceUrl = _service.BaseUrl;

        try
        {
            IndexModel index = await _service.GetDashboardDataAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // When applying authoritative state from the device we want to
                // ignore any Switch.Toggled events raised by the UI as a result
                // of the programmatic property updates. Mark a short-lived flag
                // so the view can drop those events.
                IsApplyingRemoteState = true;
                try
                {
                    UpdateSystem(index.System);
                    UpdateRelays(index.Relays);
                    UpdateSensors(index);
                    IsConnected = true;
                    StatusMessage = $"Updated {DateTime.Now:HH:mm:ss}";
                }
                finally
                {
                    IsApplyingRemoteState = false;
                }
            });

            // Poll OTA status as a true fire-and-forget so any failure or timeout
            // here can never affect IsConnected, HasSensors, or the IsBusy flag.
            _ = Task.Run(async () =>
            {
                var ota = await _service.GetOtaStatusAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OtaSupported = ota != null;
                    _otaStatus = ota;
                    NotifyOtaProperties();
                });
            });
        }
        catch (DeviceResponseException ex)
        {
            // Device is reachable but sent bad data — keep last known values visible
            _log.Warning(ex.Message);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = $"Bad data {DateTime.Now:HH:mm:ss} — see log";
            });
        }
        catch (Exception ex)
        {
            // Network / timeout failure — device unreachable
            _log.Error($"Connection failed: {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsConnected = false;
                StatusMessage = MessageDeviceUnreachable;
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AutoRefreshLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await RefreshAsync();
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ToggleRelayAsync(RelayViewModel relay)
    {
        if (!_service.IsConfigured || relay == null)
            return;

        bool newState = !relay.IsOn;

        try
        {
            bool success = await _service.SetRelayStateAsync(relay.Index, newState);

            if (success)
            {
                relay.State = newState ? 1 : 0;
                // Refresh to get confirmed state from device
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Toggle relay {relay.Index} failed: {ex.Message}");
            StatusMessage = MessageToggleFailed;
        }
    }

    private async Task InstallUpdateAsync()
    {
        if (!CanInstallUpdate)
            return;

        try
        {
            _log.Info(LogOtaTrigger);
            await _service.TriggerOtaInstallAsync();

            // Give the device a moment then poll for updated status
            await Task.Delay(TimeSpan.FromSeconds(SecondsTwo));
            var ota = await _service.GetOtaStatusAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _otaStatus = ota;
                NotifyOtaProperties();
            });
        }
        catch (Exception ex)
        {
            _log.Error($"OTA trigger failed: {ex.Message}");
        }
    }

    private void NotifyOtaProperties()
    {
        OnPropertyChanged(nameof(UpdateAvailable));
        OnPropertyChanged(nameof(OtaBusy));
        OnPropertyChanged(nameof(OtaBannerLabel));
        OnPropertyChanged(nameof(ShowOtaBanner));
        OnPropertyChanged(nameof(OtaBannerColor));
        OnPropertyChanged(nameof(CanInstallUpdate));
        ((Command)InstallUpdateCommand).ChangeCanExecute();
    }

    private void UpdateSystem(SystemModel system)
    {
        SystemFreeMemory = $"{Math.Round((double)system.Mem / KilobyteBytes, DefaultDecimalPlaces)} kb";
        SystemCpuUsage = $"{system.Cpu}%";
        // Populate firmware and uptime so the status bar can show them
        SystemFirmware = string.IsNullOrEmpty(system.Fw) ? DoubleDash : system.Fw;
        SystemUptime = string.IsNullOrEmpty(system.Uptime) ? DoubleDash : system.Uptime;
    }

    private void UpdateRelays(IReadOnlyList<RelayModel> incoming)
    {
        var vms = RelayStore.FromModels(incoming ?? []);
        _relayStore.ReplaceAll(vms);
    }

    private void UpdateSensors(IndexModel index)
    {
        Sensors.Clear();

        foreach (SensorsModel sensor in index.SensorsList)
        {
            Sensors.Add(sensor);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
