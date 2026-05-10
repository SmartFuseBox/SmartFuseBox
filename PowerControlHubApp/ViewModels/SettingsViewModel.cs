using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PowerControlHubApp.Services;

namespace PowerControlHubApp.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private readonly ThemeService _themeService;
    private string _ipAddress = string.Empty;
    private string _port = "80";
    private string _statusMessage = string.Empty;
    private string _selectedTheme;

    public string IpAddress
    {
        get => _ipAddress;
        set { _ipAddress = value; OnPropertyChanged(); }
    }

    public string Port
    {
        get => _port;
        set { _port = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatusMessage)); }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

    /// <summary>Options shown in the theme Picker: "System", "Light", "Dark".</summary>
    public string[] ThemeOptions => ThemeService.ThemeOptions;

    /// <summary>Applying theme immediately so the user sees a live preview.</summary>
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value) return;
            _selectedTheme = value;
            OnPropertyChanged();
            _themeService.Apply(value);
        }
    }

    public ICommand SaveCommand { get; }

    public SettingsViewModel(PowerHubService service, ThemeService themeService)
    {
        _service = service;
        _themeService = themeService;
        SaveCommand = new Command(Save);

        IpAddress = Preferences.Get("device_ip", string.Empty);
        Port = Preferences.Get("device_port", "80");
        _selectedTheme = _themeService.Current;
    }

    private void Save()
    {
        string ip = IpAddress.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            StatusMessage = "IP address is required.";
            return;
        }

        if (!int.TryParse(Port.Trim(), out int port) || port < 1 || port > 65535)
        {
            StatusMessage = "Enter a valid port number (1–65535).";
            return;
        }

        Preferences.Set("device_ip", ip);
        Preferences.Set("device_port", port.ToString());

        _service.Configure(ip, port);
        StatusMessage = $"Saved. Connecting to {ip}:{port}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
