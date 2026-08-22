using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.ViewModels;

public sealed class MqttSettingsViewModel : BaseViewModel
{
    private const string MsgMqttSettingsSaved = "MQTT settings saved";
    private const string MsgSaveFailedDeviceUnreachable = "Save failed — device unreachable";
    private const string MsgRefreshed = "Refreshed";

    private bool _mqttEnabled;
    private string _mqttBroker = string.Empty;
    private string _mqttPort = string.Empty;
    private string _mqttUsername = string.Empty;
    private string _mqttPassword = string.Empty;
    private string _mqttDeviceId = string.Empty;
    private bool _mqttHADiscovery;
    private string _mqttKeepAlive = string.Empty;
    private bool _mqttConnectionState;
    private string _mqttDiscoveryPrefix = string.Empty;
    private bool _isRefreshing;
    private bool _isSaving;

    public MqttSettingsViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
        RefreshCommand = new Command(async () => await RefreshAsync());
        SaveAllCommand = new Command(async () => await SaveAllAsync());
    }

    public ICommand SaveAllCommand { get; }

    public bool MqttEnabled
    {
        get => _mqttEnabled;
        set
        {
            _mqttEnabled = value;
            OnPropertyChanged();
        }
    }

    public string MqttBroker
    {
        get => _mqttBroker;
        set
        {
            _mqttBroker = value;
            OnPropertyChanged();
        }
    }

    public string MqttPort
    {
        get => _mqttPort;
        set
        {
            _mqttPort = value;
            OnPropertyChanged();
        }
    }

    public string MqttUsername
    {
        get => _mqttUsername;
        set
        {
            _mqttUsername = value;
            OnPropertyChanged();
        }
    }

    public string MqttPassword
    {
        get => _mqttPassword;
        set
        {
            _mqttPassword = value;
            OnPropertyChanged();
        }
    }

    public string MqttDeviceId
    {
        get => _mqttDeviceId;
        set
        {
            _mqttDeviceId = value;
            OnPropertyChanged();
        }
    }

    public bool MqttHADiscovery
    {
        get => _mqttHADiscovery;
        set
        {
            _mqttHADiscovery = value;
            OnPropertyChanged();
        }
    }

    public string MqttKeepAlive
    {
        get => _mqttKeepAlive;
        set
        {
            _mqttKeepAlive = value;
            OnPropertyChanged();
        }
    }

    public bool MqttConnectionState
    {
        get => _mqttConnectionState;
        set
        {
            _mqttConnectionState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MqttConnectionStateText));
        }
    }

    public string MqttConnectionStateText => _mqttConnectionState ? MqttConnected : MqttDisconnected;

    public string MqttDiscoveryPrefix
    {
        get => _mqttDiscoveryPrefix;
        set
        {
            _mqttDiscoveryPrefix = value;
            OnPropertyChanged();
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            _isRefreshing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotRefreshing));
        }
    }

    public bool IsNotRefreshing => !_isRefreshing;

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            _isSaving = value;
            OnPropertyChanged();
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public async Task RefreshAsync()
    {
        if (!Service.IsConfigured || _isRefreshing)
            return;

        IsRefreshing = true;

        try
        {
            bool? enabled = await Service.GetMqttEnabledAsync();
            string broker = await Service.GetMqttBrokerAsync();
            int? port = await Service.GetMqttPortAsync();
            string username = await Service.GetMqttUsernameAsync();
            string deviceId = await Service.GetMqttDeviceIdAsync();
            bool? haDiscovery = await Service.GetMqttHADiscoveryAsync();
            int? keepAlive = await Service.GetMqttKeepAliveAsync();
            bool? connectionState = await Service.GetMqttConnectionStateAsync();
            string discoveryPrefix = await Service.GetMqttDiscoveryPrefixAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MqttEnabled = enabled ?? false;
                MqttBroker = broker ?? string.Empty;
                MqttPort = port?.ToString() ?? string.Empty;
                MqttUsername = username ?? string.Empty;
                MqttPassword = string.Empty;
                MqttDeviceId = deviceId ?? string.Empty;
                MqttHADiscovery = haDiscovery ?? false;
                MqttKeepAlive = keepAlive?.ToString() ?? string.Empty;
                MqttConnectionState = connectionState ?? false;
                MqttDiscoveryPrefix = discoveryPrefix ?? string.Empty;

                IsConnected = true;
                StatusMessage = $"{MsgRefreshed} {DateTime.Now:HH:mm:ss}";
                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsConnected = false;
                StatusMessage = MessageDeviceUnreachable;
                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task SaveAllAsync()
    {
        if (!Service.IsConfigured || _isSaving)
            return;

        IsSaving = true;
        bool anyFailed = false;

        try
        {
            bool enabledOk = await Service.SetMqttEnabledAsync(MqttEnabled);
            anyFailed |= !enabledOk;

            bool brokerOk = await Service.SetMqttBrokerAsync(MqttBroker);
            anyFailed |= !brokerOk;

            if (int.TryParse(MqttPort, out int port) && port >= PortMin && port <= PortMax)
            {
                bool portOk = await Service.SetMqttPortAsync(port);
                anyFailed |= !portOk;
            }

            bool usernameOk = await Service.SetMqttUsernameAsync(MqttUsername);
            anyFailed |= !usernameOk;

            if (!string.IsNullOrEmpty(MqttPassword))
            {
                bool passwordOk = await Service.SetMqttPasswordAsync(MqttPassword);
                anyFailed |= !passwordOk;
            }

            bool deviceIdOk = await Service.SetMqttDeviceIdAsync(MqttDeviceId);
            anyFailed |= !deviceIdOk;

            bool haOk = await Service.SetMqttHADiscoveryAsync(MqttHADiscovery);
            anyFailed |= !haOk;

            if (int.TryParse(MqttKeepAlive, out int keepAlive) && keepAlive > 0)
            {
                bool kaOk = await Service.SetMqttKeepAliveAsync(keepAlive);
                anyFailed |= !kaOk;
            }

            bool prefixOk = await Service.SetMqttDiscoveryPrefixAsync(MqttDiscoveryPrefix);
            anyFailed |= !prefixOk;

            await Service.SaveSettingsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MqttPassword = string.Empty;

                if (anyFailed)
                {
                    StatusMessage = SaveFailed;
                }
                else
                {
                    StatusMessage = MsgMqttSettingsSaved;
                }

                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = SaveFailed;
                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        finally
        {
            IsSaving = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Called polymorphically from OnDisappearing")]
    public void Cleanup()
    {
    }

    protected override void OnDataFetched(IndexModel index)
    {
    }
}
