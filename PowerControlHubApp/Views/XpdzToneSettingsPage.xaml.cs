using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class XpdzToneSettingsPage : BasePowerControlHubContentPage
{
    private readonly XpdzToneSettingsViewModel _viewModel;

    public XpdzToneSettingsPage(XpdzToneSettingsViewModel viewModel)
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
