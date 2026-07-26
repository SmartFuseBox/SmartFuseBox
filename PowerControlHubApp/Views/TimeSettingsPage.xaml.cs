using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class TimeSettingsPage : BasePowerControlHubContentPage
{
    private readonly TimeSettingsViewModel _viewModel;

    public TimeSettingsPage(TimeSettingsViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.RefreshAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Cleanup();
    }
}
