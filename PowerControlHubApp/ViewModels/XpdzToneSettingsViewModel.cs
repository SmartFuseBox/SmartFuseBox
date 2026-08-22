using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.ViewModels;

public sealed class XpdzToneSettingsViewModel : BaseViewModel
{
    private string _pin = string.Empty;
    private bool _isRefreshing;
    private bool _isSaving;

    public XpdzToneSettingsViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
        RefreshCommand = new Command(async () => await RefreshAsync());
        SaveAllCommand = new Command(async () => await SaveAllAsync());
    }

    public ICommand SaveAllCommand { get; }

    public string Pin
    {
        get => _pin;

        set
        {
            _pin = value;
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
            IndexModel index = await Service.GetDashboardDataAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (index?.Config != null)
                {
                    Pin = index.Config.XpdzTonePin == UnconfiguredPin ? string.Empty : index.Config.XpdzTonePin.ToString();
                }

                IsConnected = true;
                StatusMessage = $"{Refreshed} {DateTime.Now:HH:mm:ss}";
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

        try
        {
            bool pinFailed = false;

            if (int.TryParse(Pin, out int pin))
            {
                bool ok = await Service.SetXpdzTonePinAsync(pin);
                pinFailed = !ok;
            }

            await Service.SaveSettingsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (pinFailed)
                {
                    StatusMessage = SaveFailed;
                }
                else
                {
                    StatusMessage = BuzzerSettingsSaved;
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
