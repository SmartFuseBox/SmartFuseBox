using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class LocalSensorDetailPage : BasePowerControlHubContentPage
{
    public LocalSensorDetailPage(LocalSensorDetailViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
