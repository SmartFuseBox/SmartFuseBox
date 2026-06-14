using Microsoft.Extensions.Logging;
using PowerControlHubApp.Services;
using PowerControlHubApp.ViewModels;
using PowerControlHubApp.Views;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp
{
    public static class MauiProgram
    {

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(FontOpenSansRegular, FontOpenSansRegularName);
                    fonts.AddFont(FontSansSemiBold, FontSansSemiBoldName);
                });

            // Services
            builder.Services.AddSingleton<PowerHubService>(sp =>
            {
                var service = new PowerHubService();
                string ip = Preferences.Get(KeyDeviceIpAddress, string.Empty);
                string port = Preferences.Get(KeyDeviceIpPort, DefaultDeviceIpPort);

                if (!string.IsNullOrEmpty(ip) && int.TryParse(port, out int p))
                    service.Configure(ip, p);

                return service;
            });

            // Dashboard poller - single instance that also runs as a hosted background service
            builder.Services.AddSingleton<IDashboardProvider, DashboardPoller>();
            builder.Services.AddHostedService(sp => (DashboardPoller)sp.GetRequiredService<IDashboardProvider>());
            builder.Services.AddSingleton<LogService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<RelayStore>();

            // ViewModels
            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddSingleton<RelayConfigViewModel>();
            builder.Services.AddTransient<RelayDetailViewModel>();

            // Pages
            builder.Services.AddSingleton<DashboardPage>();
            builder.Services.AddSingleton<SettingsPage>();
            builder.Services.AddSingleton<RelayConfigPage>();
            builder.Services.AddTransient<RelayDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
