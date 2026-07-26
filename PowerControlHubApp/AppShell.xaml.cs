using PowerControlHubApp.Views;

namespace PowerControlHubApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(RelayDetailPage), typeof(RelayDetailPage));
            Routing.RegisterRoute(nameof(ExternalSensorDetailPage), typeof(ExternalSensorDetailPage));
            Routing.RegisterRoute(nameof(LocalSensorDetailPage), typeof(LocalSensorDetailPage));
            Routing.RegisterRoute(nameof(TimeSettingsPage), typeof(TimeSettingsPage));
        }
    }
}
