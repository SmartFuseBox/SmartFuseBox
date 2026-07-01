using PowerControlHubApp.ViewModels;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Views;

public partial class ExternalSensorDetailPage : ContentPage
{
    private readonly ExternalSensorDetailViewModel _vm;

    public ExternalSensorDetailPage(ExternalSensorDetailViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(NavBack);
    }
}
