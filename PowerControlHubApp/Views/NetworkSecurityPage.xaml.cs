using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class NetworkSecurityPage : BasePowerControlHubContentPage
{
    private readonly NetworkSecurityViewModel _viewModel;

    public NetworkSecurityPage(NetworkSecurityViewModel viewModel)
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
}
