using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views
{
    public partial class BasePowerControlHubContentPage : ContentPage
    {
        private readonly BaseViewModel _viewModel;

        public BasePowerControlHubContentPage(BaseViewModel baseViewModel)
        {
            _viewModel = baseViewModel ?? throw new ArgumentNullException(nameof(baseViewModel));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.StartAutoRefresh();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.StopAutoRefresh();
        }
    }
}
