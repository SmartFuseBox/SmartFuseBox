using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class NextionSettingsPage : BasePowerControlHubContentPage
{
    public NextionSettingsPage(NextionSettingsViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
