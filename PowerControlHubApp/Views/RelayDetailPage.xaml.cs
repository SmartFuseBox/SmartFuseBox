using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class RelayDetailPage : BasePowerControlHubContentPage
{
    public RelayDetailPage(RelayDetailViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
