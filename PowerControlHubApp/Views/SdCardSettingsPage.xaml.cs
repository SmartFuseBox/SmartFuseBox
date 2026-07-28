using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class SdCardSettingsPage : BasePowerControlHubContentPage
{
    private readonly SdCardSettingsViewModel _viewModel;

    public SdCardSettingsPage(SdCardSettingsViewModel viewModel)
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
