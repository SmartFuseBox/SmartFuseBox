using System;
using System.Collections.Generic;
using System.Text;

namespace PowerControlHubApp.Internal
{
    internal static class Constants
    {
#if WINDOWS
        public const string MinimumWidth = "win_w";
        public const string MinimumHeight = "win_h";
        public const string PositionX = "win_x";
        public const string PositionY = "win_y";
        public const int NoSavedPosition = int.MinValue;
        public const int DefaultSize = 0;
#endif
        public const string ColorAsHex1 = "#44cc44";
        public const string ColorError = "#cc4444";
        public const string ColorBusy = "#4488cc";
        public const string ColorWarning = "#e8a020";
        public const double OpacityFull = 1.0;
        public const double OpacityDim = 0.4;
        public const string TimeFormat = "HH:mm:ss";
        public const string ColorLogWarning = "#ffaa00";
        public const string ColorLogError = "#ff4444";
        public const string ColorLogDefault = "#888888";
        public const string DoubleDash = "--";
        public const string FontOpenSansRegular = "OpenSans-Regular.ttf";
        public const string FontSansSemiBold = "OpenSans-Semibold.ttf";
        public const string FontSansSemiBoldName = "OpenSansSemibold";
        public const string FontOpenSansRegularName = "OpenSansRegular";
        public const string KeyDeviceIpAddress = "device_ip";
        public const string KeyDeviceIpPort = "device_port";
        public const string DefaultDeviceIpPort = "80";
        public const string MessageNotConfigured = "Not configured — tap ⚙ to set device IP";
        public const string MessageDeviceUnreachable = "Device unreachable";
        public const string MessageToggleFailed = "Toggle failed — see log";
        public const string LogOtaTrigger = "OTA: triggering firmware install…";
        public const int RelayCount = 8;
        public const int UnconfiguredPin = 255;
        // Dashboard poller log messages
        public const string LogDashboardStarted = "DashboardPoller started with interval {IntervalMs}ms";
        public const string LogDashboardFetched = "DashboardPoller fetched index at {Time}";
        public const string LogDashboardSkipping = "DashboardPoller skipping poll: service not configured";
        public const string LogDeviceInvalidJson = "Device returned invalid JSON while polling index";
        public const string LogUnexpectedPollingError = "Unexpected error while polling index";
        public const string LogDashboardStopping = "DashboardPoller stopping";

        // Theme keys
        public const string ThemeKey_AppPageBg = "AppPageBg";
        public const string ThemeKey_AppBarBg = "AppBarBg";
        public const string ThemeKey_AppLogPanelBg = "AppLogPanelBg";
        public const string ThemeKey_AppCardBg = "AppCardBg";
        public const string ThemeKey_AppCardStroke = "AppCardStroke";
        public const string ThemeKey_AppSensorCardBg = "AppSensorCardBg";
        public const string ThemeKey_AppHelpCardBg = "AppHelpCardBg";
        public const string ThemeKey_AppHelpCardStroke = "AppHelpCardStroke";
        public const string ThemeKey_AppAccent = "AppAccent";
        public const string ThemeKey_AppLabelPrimary = "AppLabelPrimary";
        public const string ThemeKey_AppLabelMuted = "AppLabelMuted";
        public const string ThemeKey_AppLabelSubtle = "AppLabelSubtle";
        public const string ThemeKey_AppBarText = "AppBarText";
        public const string ThemeKey_AppLogTimestamp = "AppLogTimestamp";
        public const string ThemeKey_AppLogText = "AppLogText";
        public const string ThemeKey_AppSwitchOn = "AppSwitchOn";
        public const string ThemeKey_AppEntryBg = "AppEntryBg";
        public const string ThemeKey_AppEntryStroke = "AppEntryStroke";
        public const string ThemeKey_AppEntryText = "AppEntryText";
        public const string ThemeKey_AppPlaceholderText = "AppPlaceholderText";

        // Common theme colours used multiple times
        public const string ThemeColor_Accent = "#1a73e8";
        public const string ThemeColor_AccentAlt = "#00d4ff";
        public const string ThemeColor_White = "#ffffff";
        public const string ThemeColor_PrimaryText = "#1a1a2a";
        public const string ThemeColor_LabelMuted = "#555555";
        public const string ThemeColor_LabelSubtle = "#888888";
        public const string ThemeColor_EntryBgLight = "#e8f0fe";
        public const string ThemeColor_CardStrokeLight = "#c8d8e8";
        public const string ThemeColor_EntryStrokeDark = "#0f3460";
        public const string ThemeColor_AppBarDark = "#16213e";
        public const string ThemeColor_PageBg_Light = "#f0f4f8";
        public const string ThemeColor_LogPanelBg_Light = "#f8f8ff";
        public const string ThemeColor_SensorCardBg_Light = "#eaf2ff";
        public const string ThemeColor_HelpCardBg_Light = "#eef2ff";
        public const string ThemeColor_HelpCardStroke_Light = "#c0ccee";
        public const string ThemeColor_LogTimestamp_Light = "#8888aa";
        public const string ThemeColor_LogText_Light = "#444444";
        public const string ThemeColor_EntryStrokeLight = "#aabbd4";
        public const string ThemeColor_Placeholder_Light = "#8899aa";

        public const string ThemeColor_PageBg_Dark = "#0a0a1a";
        public const string ThemeColor_LogPanelBg_Dark = "#0d0d1f";
        public const string ThemeColor_CardBg_Dark = "#1a1a2e";
        public const string ThemeColor_SensorCardBg_Dark = "#0d1b2a";
        public const string ThemeColor_HelpCardBg_Dark = "#111122";
        public const string ThemeColor_HelpCardStroke_Dark = "#333355";
        public const string ThemeColor_LabelSubtle_Dark = "#666666";
        public const string ThemeColor_LogTimestamp_Dark = "#555577";
        public const string ThemeColor_LogText_Dark = "#aaaaaa";
        public const string ThemeColor_Placeholder_Dark = "#555555";
        // Networking / settings
        public const int PortMin = 1;
        public const int PortMax = 65535;
        public const string MsgIpRequired = "IP address is required.";
        public const string MsgInvalidPort = "Enter a valid port number (1–65535).";

        // Relay UI
        public const string NoneString = "None";
        public const string SavedOk = "✓ Saved successfully";
        public const string SavedFailed = "⚠ One or more commands failed";
        public const int ColorOptionNoneIndex = 6;
        public static readonly string[] ColorOptionNames = new[] { ColorName_Blue, ColorName_Green, ColorName_Orange, ColorName_Purple, ColorName_Red, ColorName_Yellow, NoneString };
        public static readonly string[] ActionOptionNames = new[] { ActionName_Default, ActionName_Horn, ActionName_NightRelay };
        public static readonly string[] DefaultStateOptionNames = new[] { DefaultState_Off, DefaultState_On };
        public const string NavBack = "..";
        public const string RelayPageTitle = "Relay";
        public const string CheckMark = "✓";
        // Relay panel colours
        public const int RelayColorBlue = 0;
        public const int RelayColorGreen = 1;
        public const int RelayColorOrange = 2;
        public const int RelayColorPurple = 3;
        public const int RelayColorRed = 4;
        public const int RelayColorYellow = 5;
        public const string ColorRelayPanelBlue = "#2255cc";
        public const string ColorRelayPanelGreen = "#22aa44";
        public const string ColorRelayPanelOrange = "#dd7722";
        public const string ColorRelayPanelPurple = "#8833cc";
        public const string ColorRelayPanelRed = "#cc3333";
        public const string ColorRelayPanelYellow = "#ccbb22";
        public const string WarningMark = "⚠";

        // Color option names
        public const string ColorName_Blue = "Blue";
        public const string ColorName_Green = "Green";
        public const string ColorName_Orange = "Orange";
        public const string ColorName_Purple = "Purple";
        public const string ColorName_Red = "Red";
        public const string ColorName_Yellow = "Yellow";

        // Action option names
        public const string ActionName_Default = "Default";
        public const string ActionName_Horn = "Horn";
        public const string ActionName_NightRelay = "Night Relay";

        // Default state option names
        public const string DefaultState_Off = "Off";
        public const string DefaultState_On = "On";

        // OTA states and labels
        public const string OtaState_Idle = "idle";
        public const string OtaState_Available = "available";
        public const string OtaState_Checking = "checking";
        public const string OtaState_Downloading = "downloading";
        public const string OtaState_Rebooting = "rebooting";
        public const string OtaState_Failed = "failed";
        public const string OtaState_UpToDate = "uptodate";

        public const string OtaLabel_Available = "Update available: {0}  (installed: {1})";
        public const string OtaLabel_Checking = "Checking for firmware update…";
        public const string OtaLabel_Downloading = "Downloading firmware update…";
        public const string OtaLabel_Rebooting = "Applying update — device rebooting…";
        public const string OtaLabel_Failed = "Firmware update failed. Tap to retry.";
        public const string OtaLabel_Uptodate = "Firmware is up to date ({0})";

        public const string OtaAuto_Off = "0";

        public const int DefaultIntervalMs = 750;
        public const string RouteApiIndex = "api/index";
        public const string RouteSaveConfig = "api/config/C0";
        public const string RouteOtaUpdate = "api/system/F13";
        public const string RouteUpdateOta = "api/system/F12?apply=1";
        public const string ForwardSlash = "/";
        public const string ResultSuccess = "success";
        public const string RouteDashboardPage = "//DashboardPage";
        public const string RouteSettingsPage = "//SettingsPage";
        public const string ConnectionTypeKey = "X-Connection-Type";
        public const string ConnectionTypePersistent = "persistent";
        public const int MaximumPermanentConnections = 2;
        public const int SecondsSixty = 60;
        public const int SecondsTen = 10;
        public const int SecondsFive = 5;
        public const int SecondsThree = 3;
        public const int SecondsTwo = 2;
        public const string UserAgentKey = "User-Agent";
        public const string UserAgentValue = "PowerControlHub/1.0";
        public const string PowerHubNotConfigured = "PowerHubService is not configured. Call Configure() first.";
        public const string ErrorKey = "error";
        public const string PreferenceKey = "app_theme";
        public const string ThemeLight = "Light";
        public const string ThemeDark = "Dark";


        public const int KilobyteBytes = 1024;
        public const int DefaultDecimalPlaces = 2;
    }
}
