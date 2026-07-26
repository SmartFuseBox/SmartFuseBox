using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class MqttSettingsPage : BasePowerControlHubContentPage
{
    private readonly MqttSettingsViewModel _viewModel;

    public MqttSettingsPage(MqttSettingsViewModel viewModel)
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
