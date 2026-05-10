using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PowerControlHubApp.Models;
using PowerControlHubApp.Services;

namespace PowerControlHubApp.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private readonly LogService _log;
    private CancellationTokenSource? _refreshCts;
    private bool _isBusy;
    private bool _isConnected;
    private string _statusMessage = string.Empty;
    private string _deviceUrl = string.Empty;

    // OTA
    private OtaStatusModel? _otaStatus;
    private bool _otaSupported;

    public ObservableCollection<RelayModel> Relays { get; } = new();
    public ObservableCollection<SensorModel> Sensors { get; } = new();
    public ObservableCollection<LogEntry> LogEntries => _log.Entries;

    public bool HasRelays => _isConnected && Relays.Count > 0;
    public bool HasNoRelays => _isConnected && Relays.Count == 0;
    public bool HasSensors => _isConnected && Sensors.Count > 0;
    public bool HasNoSensors => _isConnected && Sensors.Count == 0;

    public ICommand RefreshCommand { get; }
    public ICommand ToggleRelayCommand { get; }
    public ICommand InstallUpdateCommand { get; }

    // ── OTA properties ────────────────────────────────────────────────────────

    /// <summary>True when the firmware reported an update is available.</summary>
    public bool UpdateAvailable => _otaStatus?.UpdateAvailable == true;

    /// <summary>True while the device is actively checking/downloading/rebooting.</summary>
    public bool OtaBusy => _otaStatus?.IsBusy == true;

    /// <summary>True when OTA is not supported by this firmware build (F13 not present).</summary>
    public bool OtaSupported
    {
        get => _otaSupported;
        private set { _otaSupported = value; OnPropertyChanged(); }
    }

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
        { HasFailed: true }      => Color.FromArgb("#cc4444"),
        { IsBusy: true }         => Color.FromArgb("#4488cc"),
        { UpdateAvailable: true } => Color.FromArgb("#e8a020"),
        _                        => Color.FromArgb("#e8a020")
    };

    /// <summary>Install button is active only when an update is available and nothing is in progress.</summary>
    public bool CanInstallUpdate => _otaStatus?.UpdateAvailable == true && !OtaBusy && !IsBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
    }

    public bool IsNotBusy => !_isBusy;

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsDisconnected)); OnPropertyChanged(nameof(StatusColor)); OnPropertyChanged(nameof(HasRelays)); OnPropertyChanged(nameof(HasNoRelays)); OnPropertyChanged(nameof(HasSensors)); OnPropertyChanged(nameof(HasNoSensors)); }
    }

    public bool IsDisconnected => !_isConnected;

    public Color StatusColor => _isConnected ? Color.FromArgb("#44cc44") : Color.FromArgb("#cc4444");

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string DeviceUrl
    {
        get => _deviceUrl;
        set { _deviceUrl = value; OnPropertyChanged(); }
    }

    public DashboardViewModel(PowerHubService service, LogService log)
    {
        _service = service;
        _log     = log;
        RefreshCommand = new Command(async () => await RefreshAsync());
        ToggleRelayCommand = new Command<RelayModel>(async relay => await ToggleRelayAsync(relay));
        InstallUpdateCommand = new Command(async () => await InstallUpdateAsync(), () => CanInstallUpdate);

        Relays.CollectionChanged += (_, _) =>
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

    // ── Public methods ────────────────────────────────────────────────────────

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
            StatusMessage = "Not configured — tap ⚙ to set device IP";
            IsConnected = false;
            return;
        }

        if (IsBusy)
            return;

        IsBusy = true;
        DeviceUrl = _service.BaseUrl;

        try
        {
            var (relays, sensors) = await _service.GetDashboardDataAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateRelays(relays);
                UpdateSensors(sensors);
                IsConnected = true;
                StatusMessage = $"Updated {DateTime.Now:HH:mm:ss}";
            });

            // Poll OTA status as a true fire-and-forget so any failure or timeout
            // here can never affect IsConnected, HasSensors, or the IsBusy flag.
            _ = Task.Run(async () =>
            {
                var ota = await _service.GetOtaStatusAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OtaSupported = ota != null;
                    _otaStatus   = ota;
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
                StatusMessage = "Device unreachable";
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task AutoRefreshLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await RefreshAsync();
            try { await Task.Delay(TimeSpan.FromSeconds(5), ct); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task ToggleRelayAsync(RelayModel relay)
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
            StatusMessage = "Toggle failed — see log";
        }
    }

    private async Task InstallUpdateAsync()
    {
        if (!CanInstallUpdate)
            return;

        try
        {
            _log.Info("OTA: triggering firmware install…");
            await _service.TriggerOtaInstallAsync();

            // Give the device a moment then poll for updated status
            await Task.Delay(TimeSpan.FromSeconds(2));
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

    private void UpdateRelays(List<RelayModel> incoming)
    {
        var enabled = incoming.Where(r => r.IsEnabled).ToList();

        // Update state on items that already exist in the collection
        foreach (var existing in Relays)
        {
            var updated = enabled.FirstOrDefault(r => r.Index == existing.Index);
            if (updated != null)
                existing.State = updated.State;
        }

        // Remove items no longer present
        var removedIndices = Relays
            .Where(r => !enabled.Any(e => e.Index == r.Index))
            .ToList();
        foreach (var r in removedIndices)
            Relays.Remove(r);

        // Append genuinely new items
        foreach (var r in enabled.Where(e => !Relays.Any(x => x.Index == e.Index)))
            Relays.Add(r);
    }

    private void UpdateSensors(List<SensorModel> incoming)
    {
        // Update extra fields on items that already exist
        foreach (var existing in Sensors)
        {
            var updated = incoming.FirstOrDefault(s => s.Name == existing.Name);
            if (updated != null)
                existing.ExtraFields = updated.ExtraFields;
        }

        // Remove stale items
        var removed = Sensors
            .Where(s => !incoming.Any(i => i.Name == s.Name))
            .ToList();
        foreach (var s in removed)
            Sensors.Remove(s);

        // Append new items
        foreach (var s in incoming.Where(i => !Sensors.Any(x => x.Name == i.Name)))
            Sensors.Add(s);
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
