using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public sealed class SdCardSettingsViewModel : BaseViewModel
{
    private string _spiSck = string.Empty;
    private string _spiMosi = string.Empty;
    private string _spiMiso = string.Empty;
    private string _csPin = string.Empty;
    private string _initSpeed = string.Empty;
    private bool _isRefreshing;
    private bool _isSaving;

    public SdCardSettingsViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
        RefreshCommand = new Command(async () => await RefreshAsync());
        SaveAllCommand = new Command(async () => await SaveAllAsync());
    }

    public ICommand SaveAllCommand { get; }

    public string SpiSck
    {
        get => _spiSck;

        set
        {
            _spiSck = value;
            OnPropertyChanged();
        }
    }

    public string SpiMosi
    {
        get => _spiMosi;

        set
        {
            _spiMosi = value;
            OnPropertyChanged();
        }
    }

    public string SpiMiso
    {
        get => _spiMiso;

        set
        {
            _spiMiso = value;
            OnPropertyChanged();
        }
    }

    public string CsPin
    {
        get => _csPin;

        set
        {
            _csPin = value;
            OnPropertyChanged();
        }
    }

    public string InitSpeed
    {
        get => _initSpeed;

        set
        {
            _initSpeed = value;
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
            var index = await Service.GetDashboardDataAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (index?.Config != null)
                {
                    SpiSck = index.Config.SpiPins?.Sck == SdCardPinDisabled ? string.Empty : index.Config.SpiPins?.Sck.ToString() ?? string.Empty;
                    SpiMosi = index.Config.SpiPins?.Mosi == SdCardPinDisabled ? string.Empty : index.Config.SpiPins?.Mosi.ToString() ?? string.Empty;
                    SpiMiso = index.Config.SpiPins?.Miso == SdCardPinDisabled ? string.Empty : index.Config.SpiPins?.Miso.ToString() ?? string.Empty;
                    CsPin = index.Config.SdCardCsPin == SdCardPinDisabled ? string.Empty : index.Config.SdCardCsPin.ToString();
                    InitSpeed = index.Config.SdCardInitializeSpeed.ToString();
                }

                IsConnected = true;
                StatusMessage = $"{SdCardMsgRefreshed} {DateTime.Now:HH:mm:ss}";
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
        bool spiFailed = false;
        bool csFailed = false;
        bool speedFailed = false;

        try
        {
            if (int.TryParse(SpiSck, out int sck) && int.TryParse(SpiMosi, out int mosi) && int.TryParse(SpiMiso, out int miso))
            {
                var spiOk = await Service.SetSdCardSpiPinsAsync(sck, mosi, miso);
                spiFailed = !spiOk;
            }

            if (int.TryParse(CsPin, out int cs))
            {
                var csOk = await Service.SetSdCardCsPinAsync(cs);
                csFailed = !csOk;
            }

            if (int.TryParse(InitSpeed, out int speed))
            {
                var speedOk = await Service.SetSdCardInitSpeedAsync(speed);
                speedFailed = !speedOk;
            }

            await Service.SaveSettingsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (spiFailed)
                {
                    StatusMessage = SdCardMsgSpiFailed;
                }
                else if (!csFailed && !speedFailed)
                {
                    StatusMessage = SdCardMsgSaved;
                }
                else
                {
                    StatusMessage = SdCardMsgPartiallySaved;
                }

                OnPropertyChanged(nameof(HasStatusMessage));
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = SdCardMsgSaveFailed;
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
