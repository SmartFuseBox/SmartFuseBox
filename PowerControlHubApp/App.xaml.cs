using Microsoft.Extensions.Logging;
using PowerControlHubApp.Services;
using static PowerControlHubApp.Internal.Constants;

#if WINDOWS
using WinRT.Interop;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace PowerControlHubApp
{
    public partial class App : Application
    {
        private readonly DashboardPoller _dashboardPoller;
        private readonly ConfigPoller _configPoller;
        private readonly SensorMetaCache _sensorMetaCache;
        private readonly IConfigConnection _configConnection;
        private readonly TimeSyncService _timeSyncService;
        private readonly ILogger<App> _log;

        public App(
            ThemeService themeService,
            DashboardPoller dashboardPoller,
            ConfigPoller configPoller,
            SensorMetaCache sensorMetaCache,
            IConfigConnection configConnection,
            TimeSyncService timeSyncService,
            ILogger<App> log)
        {
            InitializeComponent();
            _dashboardPoller = dashboardPoller;
            _configPoller = configPoller;
            _sensorMetaCache = sensorMetaCache;
            _configConnection = configConnection;
            _timeSyncService = timeSyncService;
            _log = log;

            // Apply after InitializeComponent so Application.Resources is populated.
            ThemeService.ApplySaved();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.HandlerChanged += OnWindowHandlerChanged;
#endif

            return window;
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Start background pollers
            _dashboardPoller.Start();
            _configPoller.Start();
            _timeSyncService.Start();

            // Startup orchestration: after first successful dashboard poll,
            // fetch sensor meta data over the config connection so config pages are ready.
            AttachStartupOrchestration();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            _ = StopPollersAsync();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Restart pollers when the app comes back to foreground
            _dashboardPoller.Start();
            _configPoller.Start();
            _timeSyncService.Start();
        }

        private async Task StopPollersAsync()
        {
            await Task.WhenAll(
                _dashboardPoller.StopAsync(),
                _configPoller.StopAsync(),
                _timeSyncService.StopAsync());
        }

        private void AttachStartupOrchestration()
        {
            EventHandler handler = null;
            handler = async (sender, args) =>
            {
                _dashboardPoller.DataUpdated -= handler;

                try
                {
                    _log.LogDebug(LogStartupMetaFetch);
                    await _sensorMetaCache.RefreshAsync(_configConnection);
                    _log.LogDebug(LogStartupMetaPopulated);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, LogMetaRefreshFailed);
                }
            };

            if (_dashboardPoller.CurrentIndex != null)
            {
                _dashboardPoller.DataUpdated -= handler;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _log.LogDebug(LogStartupMetaAlready);
                        await _sensorMetaCache.RefreshAsync(_configConnection);
                        _log.LogDebug(LogStartupMetaPopulated);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex, LogMetaRefreshFailed);
                    }
                });
            }
            else
            {
                _dashboardPoller.DataUpdated += handler;
            }
        }

#if WINDOWS
        private static void OnWindowHandlerChanged(object sender, EventArgs e)
        {
            if (sender is not Window mauiWindow)
                return;

            // Detach — only needs to run once
            mauiWindow.HandlerChanged -= OnWindowHandlerChanged;

            if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
                return;

            var appWindow = nativeWindow.AppWindow;

            // Restore saved position and size (stored in physical pixels)
            int savedW = Preferences.Get(MinimumWidth, DefaultSize);
            int savedH = Preferences.Get(MinimumHeight, DefaultSize);
            int savedX = Preferences.Get(PositionX, NoSavedPosition);
            int savedY = Preferences.Get(PositionY, NoSavedPosition);

            if (savedW > DefaultSize && savedH > DefaultSize)
                appWindow.Resize(new SizeInt32(savedW, savedH));

            // Determine target position (start from current position)
            int targetX = appWindow.Position.X;
            int targetY = appWindow.Position.Y;

            if (savedX != NoSavedPosition && savedY != NoSavedPosition)
            {
                targetX = savedX;
                targetY = savedY;

                // Validate against the display WorkArea so window isn't positioned off-screen.
                var hwnd = WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest)
                                ?? DisplayArea.GetFromPoint(new PointInt32(targetX, targetY), DisplayAreaFallback.Nearest)
                                ?? DisplayArea.GetFromPoint(new PointInt32(appWindow.Position.X, appWindow.Position.Y), DisplayAreaFallback.Nearest);

                if (displayArea != null)
                {
                    var wa = displayArea.WorkArea; // RectInt32 { X, Y, Width, Height }

                    // Clamp top-left to WorkArea

                    if (targetX < wa.X)
                        targetX = wa.X;

                    if (targetY < wa.Y)
                        targetY = wa.Y;

                    // If size was restored, ensure right/bottom edges fit into WorkArea
                    if (savedW > 0 && savedH > 0)
                    {
                        if (targetX + savedW > wa.X + wa.Width)
                            targetX = wa.X + wa.Width - savedW;

                        if (targetY + savedH > wa.Y + wa.Height)
                            targetY = wa.Y + wa.Height - savedH;
                    }

                    // Final safety: don't move to negative infinity
                    targetX = Math.Max(targetX, wa.X);
                    targetY = Math.Max(targetY, wa.Y);
                }
            }

            appWindow.Move(new PointInt32(targetX, targetY));

            // Persist position/size whenever the window moves or is resized
            appWindow.Changed += (aw, args) =>
            {
                if (!args.DidPositionChange && !args.DidSizeChange)
                    return;

                Preferences.Set(PositionX, aw.Position.X);
                Preferences.Set(PositionY, aw.Position.Y);
                Preferences.Set(MinimumWidth, aw.Size.Width);
                Preferences.Set(MinimumHeight, aw.Size.Height);
            };
        }

#endif
    }
}