using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.ViewModels;

public sealed class RtcSettingsViewModel : BaseViewModel
{
    private string _dataPin = string.Empty;
    private string _clockPin = string.Empty;
    private string _resetPin = string.Empty;
    private bool _isRefreshing;
    private bool _isSaving;

    public RtcSettingsViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
        RefreshCommand = new Command(async () => await RefreshAsync());
        SaveAllCommand = new Command(async () => await SaveAllAsync());
    }

    public ICommand SaveAllCommand { get; }

    public string DataPin
    {
        get => _dataPin;

        set
        {
            _dataPin = value;
            OnPropertyChanged();
        }
    }

    public string ClockPin
    {
        get => _clockPin;

        set
        {
            _clockPin = value;
            OnPropertyChanged();
        }
    }

    public string ResetPin
    {
        get => _resetPin;

        set
        {
            _resetPin = value;
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
                if (index?.Config?.Rtc != null)
                {
                    DataPin = index.Config.Rtc.Dat == RtcPinDisabled ? string.Empty : index.Config.Rtc.Dat.ToString();
                    ClockPin = index.Config.Rtc.Clk == RtcPinDisabled ? string.Empty : index.Config.Rtc.Clk.ToString();
                    ResetPin = index.Config.Rtc.Rst == RtcPinDisabled ? string.Empty : index.Config.Rtc.Rst.ToString();
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
            bool pinsFailed = false;

            if (int.TryParse(DataPin, out int data) && int.TryParse(ClockPin, out int clock) && int.TryParse(ResetPin, out int reset))
            {
                bool ok = await Service.SetRtcPinsAsync(data, clock, reset);
                pinsFailed = !ok;
            }

            await Service.SaveSettingsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (pinsFailed)
                {
                    StatusMessage = SaveFailed;
                }
                else
                {
                    StatusMessage = RtcSettingsSaved;
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
