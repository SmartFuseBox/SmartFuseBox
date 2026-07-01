using PowerControlHubApp.ViewModels;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Views;

public partial class LocalSensorDetailPage : ContentPage
{
    public LocalSensorDetailPage(LocalSensorDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(NavBack);
    }
}
