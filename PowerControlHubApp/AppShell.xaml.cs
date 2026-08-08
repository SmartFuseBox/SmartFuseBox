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
            Routing.RegisterRoute(nameof(MqttSettingsPage), typeof(MqttSettingsPage));
            Routing.RegisterRoute(nameof(SdCardSettingsPage), typeof(SdCardSettingsPage));
            Routing.RegisterRoute(nameof(RtcSettingsPage), typeof(RtcSettingsPage));
            Routing.RegisterRoute(nameof(NetworkSecurityPage), typeof(NetworkSecurityPage));
            Routing.RegisterRoute(nameof(NextionSettingsPage), typeof(NextionSettingsPage));
            Routing.RegisterRoute(nameof(XpdzToneSettingsPage), typeof(XpdzToneSettingsPage));
        }
    }
}
