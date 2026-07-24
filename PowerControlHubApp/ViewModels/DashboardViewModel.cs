using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly RelayStore _relayStore;

    // OTA
    private OtaStatusModel _otaStatus;
    private bool _otaSupported;

    public ObservableCollection<RelayViewModel> Relays => _relayStore.Relays;
    public ObservableCollection<SensorsModel> Sensors { get; } = [];

    public bool HasRelays => IsConnected && Relays.Count > 0;
    public bool HasNoRelays => IsConnected && Relays.Count == 0;
    public bool HasSensors => IsConnected && Sensors.Count > 0;
    public bool HasNoSensors => IsConnected && Sensors.Count == 0;

    // RefreshCommand is provided by BaseViewModel
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

    // IsBusy, IsNotBusy, IsConnected, StatusMessage, DeviceUrl, LogEntries,
    // IsApplyingRemoteState, system properties, and refresh lifecycle are
    // provided by BaseViewModel

    public DashboardViewModel(PowerHubService service, LogService log, RelayStore relayStore)
        : base(service, log)
    {
        _relayStore = relayStore;
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

        // Relay the Has* property changes when IsConnected changes, so that
        // the UI re-evaluates bindings that depend on both connection state
        // and collection count.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsConnected))
            {
                OnPropertyChanged(nameof(HasRelays));
                OnPropertyChanged(nameof(HasNoRelays));
                OnPropertyChanged(nameof(HasSensors));
                OnPropertyChanged(nameof(HasNoSensors));
            }
        };
    }

    // Auto-refresh, log clearing, and ExecuteRefreshAsync are provided by BaseViewModel

    protected override void OnDataFetched(IndexModel index)
    {
        UpdateRelays(index.Relays);
        UpdateSensors(index);

        // Poll OTA status as a true fire-and-forget so any failure or timeout
        // here can never affect IsConnected, HasSensors, or the IsBusy flag.
        _ = Task.Run(async () =>
        {
            var ota = await Service.GetOtaStatusAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OtaSupported = ota != null;
                _otaStatus = ota;
                NotifyOtaProperties();
            });
        });
    }

    private async Task ToggleRelayAsync(RelayViewModel relay)
    {
        if (!Service.IsConfigured || relay == null)
            return;

        bool newState = !relay.IsOn;

        try
        {
            bool success = await Service.SetRelayStateAsync(relay.Index, newState);

            if (success)
            {
                relay.State = newState ? 1 : 0;
                // Refresh to get confirmed state from device
                await ExecuteRefreshAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Toggle relay {relay.Index} failed: {ex.Message}");
            StatusMessage = MessageToggleFailed;
        }
    }

    private async Task InstallUpdateAsync()
    {
        if (!CanInstallUpdate)
            return;

        try
        {
            Log.Info(LogOtaTrigger);
            await Service.TriggerOtaInstallAsync();

            // Give the device a moment then poll for updated status
            await Task.Delay(TimeSpan.FromSeconds(SecondsTwo));
            var ota = await Service.GetOtaStatusAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _otaStatus = ota;
                NotifyOtaProperties();
            });
        }
        catch (Exception ex)
        {
            Log.Error($"OTA trigger failed: {ex.Message}");
        }
    }

    private void NotifyOtaProperties()
    {
        OnPropertyChanged(nameof(UpdateAvailable));
        OnPropertyChanged(nameof(OtaBusy));
        OnPropertyChanged(nameof(OtaBannerLabel));
        OnPropertyChanged(nameof(ShowOtaBanner));
        OnPropertyChanged(nameof(OtaBannerColor));
        OnPropertyChanged(nameof(SystemFirmwareColor));
        OnPropertyChanged(nameof(CanInstallUpdate));
        ((Command)InstallUpdateCommand).ChangeCanExecute();
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

    // PropertyChanged and OnPropertyChanged are provided by BaseViewModel
}
