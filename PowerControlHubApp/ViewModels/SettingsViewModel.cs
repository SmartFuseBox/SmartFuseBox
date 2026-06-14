using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PowerControlHubApp.Services;
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

    public string IpAddress
    {
        get => _ipAddress;

        set 
        { _ipAddress = value;
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

    public ICommand SaveCommand { get; }

    public SettingsViewModel(PowerHubService service, ThemeService themeService)
    {
        _service = service;
        _themeService = themeService;
        SaveCommand = new Command(Save);

        IpAddress = Preferences.Get(KeyDeviceIpAddress, string.Empty);
        Port = Preferences.Get(KeyDeviceIpPort, DefaultDeviceIpPort);
            _selectedTheme = ThemeService.Current;
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
