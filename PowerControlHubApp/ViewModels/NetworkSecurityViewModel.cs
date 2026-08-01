using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public sealed class NetworkSecurityViewModel : BaseViewModel
{
    private string _apiKey = string.Empty;
    private string _hmacKey = string.Empty;
    private bool _isEnabled;
    private bool _isRefreshing;
    private bool _isSaving;
    private bool _isGenerating;

    public NetworkSecurityViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
        RefreshCommand = new Command(async () => await RefreshAsync());
        SaveAllCommand = new Command(async () => await SaveAllAsync());
        GenerateKeysCommand = new Command(async () => await GenerateKeysAsync());
    }

    public ICommand SaveAllCommand { get; }

    public ICommand GenerateKeysCommand { get; }

    public string ApiKey
    {
        get => _apiKey;

        set
        {
            _apiKey = value;
            OnPropertyChanged();
        }
    }

    public string HmacKey
    {
        get => _hmacKey;

        set
        {
            _hmacKey = value;
            OnPropertyChanged();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;

        set
        {
            _isEnabled = value;
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

    public bool IsGenerating
    {
        get => _isGenerating;

        set
        {
            _isGenerating = value;
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
            var config = await Service.GetAuthConfigAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (config != null)
                {
                    IsEnabled = config.Enabled;
                    ApiKey = config.ApiKey ?? string.Empty;
                    HmacKey = config.HmacKey ?? string.Empty;
                }

                IsConnected = true;
                StatusMessage = $"{NetworkSecurityMsgRefreshed} {DateTime.Now:HH:mm:ss}";
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
        bool enableFailed = false;
        bool apiKeyFailed = false;
        bool hmacKeyFailed = false;

        try
        {
            var apiKeyOk = await Service.SetAuthApiKeyAsync(ApiKey);
            apiKeyFailed = !apiKeyOk;

            var hmacKeyOk = await Service.SetAuthHmacKeyAsync(HmacKey);
            hmacKeyFailed = !hmacKeyOk;

            var enabledOk = await Service.SetAuthEnabledAsync(IsEnabled);
            enableFailed = !enabledOk;

            await Service.SaveSettingsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (enableFailed && apiKeyFailed && hmacKeyFailed)
                {
                    StatusMessage = NetworkSecurityMsgSaveFailed;
                }
                else if (enableFailed || apiKeyFailed || hmacKeyFailed)
                {
                    StatusMessage = NetworkSecurityMsgSaveFailed;
                }
                else
                {
                    StatusMessage = NetworkSecurityMsgSaved;
                }

                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = NetworkSecurityMsgSaveFailed;
                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task GenerateKeysAsync()
    {
        if (!Service.IsConfigured || _isGenerating)
            return;

        IsGenerating = true;

        try
        {
            var ok = await Service.GenerateAuthKeysAsync();

            if (ok)
            {
                await RefreshAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusMessage = NetworkSecurityMsgKeysGenerated;
                    OnPropertyChanged(nameof(HasStatusMessage));
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusMessage = NetworkSecurityMsgGenerateFailed;
                    OnPropertyChanged(nameof(HasStatusMessage));
                });
            }
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = NetworkSecurityMsgGenerateFailed;
                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        finally
        {
            IsGenerating = false;
        }
    }

    protected override void OnDataFetched(IndexModel index)
    {
    }
}
