using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class RtcSettingsPage : BasePowerControlHubContentPage
{
    private readonly RtcSettingsViewModel _viewModel;

    public RtcSettingsPage(RtcSettingsViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        _viewModel.RefreshAsync().ConfigureAwait(false);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Cleanup();
    }
}
