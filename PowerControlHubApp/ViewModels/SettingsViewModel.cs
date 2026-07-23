using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly ThemeService _themeService;
    private string _ipAddress = string.Empty;
    private string _port = DefaultDeviceIpPort;
    private string _selectedTheme;
    private string _pinsInUseText = string.Empty;
    private bool _isPinsRefreshing;
    private int _pinsCount;


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

    // StatusMessage is provided by BaseViewModel

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

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

    public SettingsViewModel(PowerHubService service, ThemeService themeService, LogService log)
        : base(service, log)
    {
        _themeService = themeService;
        SaveCommand = new Command(Save);
        RefreshPinsCommand = new Command(async () => await RefreshPinsAsync());


        IpAddress = Preferences.Get(KeyDeviceIpAddress, string.Empty);
        Port = Preferences.Get(KeyDeviceIpPort, DefaultDeviceIpPort);
        _selectedTheme = ThemeService.Current;
    }

    public async Task RefreshPinsAsync()
    {
        if (!Service.IsConfigured || _isPinsRefreshing)
            return;

        IsPinsRefreshing = true;

        try
        {
            var result = await Service.GetSystemPinsAsync();

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

        Service.Configure(ip, port);
        StatusMessage = $"Saved. Connecting to {ip}:{port}";
    }

    protected override void OnDataFetched(IndexModel index)
    {
        // Settings page doesn't process dashboard data
    }
}
