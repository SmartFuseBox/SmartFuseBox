namespace PowerControlHubApp.Internal
{

#pragma warning disable CC0003

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
        public const string CommaSpace = ", ";
        public const string FontOpenSansRegular = "OpenSans-Regular.ttf";
        public const string FontSansSemiBold = "OpenSans-Semibold.ttf";
        public const string FontSansSemiBoldName = "OpenSansSemibold";
        public const string FontOpenSansRegularName = "OpenSansRegular";
        public const string KeyDeviceIpAddress = "device_ip";
        public const string KeyDeviceIpPort = "device_port";
        public const string DefaultDeviceIpPort = "80";
        public const string KeyAuthApiKey = "auth_apikey";
        public const string KeyAuthHmacKey = "auth_hmackey";
        public const string MessageNotConfigured = "Not configured — tap ⚙ to set device IP";
        public const string MessageDeviceUnreachable = "Device unreachable";
        public const string MessageNoActiveWarnings = "No active warnings";
        public const string MessageToggleFailed = "Toggle failed — see log";
        public const string LogOtaTrigger = "OTA: triggering firmware install…";
        public const string OtaTriggerFailed = "OTA trigger failed";
        public const string OtaDialogTitle = "Install Firmware";
        public const string OtaDialogMessage = "An OTA update to {0} is available. Download and install now?";
        public const string OtaDialogAccept = "Install";
        public const int RelayCount = 8;
        public const int UnconfiguredPin = 255;
        // Dashboard poller log messages
        public const string LogDashboardStarted = "DashboardPoller started with interval {IntervalMs}ms";
        public const string LogDashboardFetched = "DashboardPoller fetched index at {Time}";
        public const string LogDashboardSkipping = "DashboardPoller skipping poll: service not configured";
        public const string LogDeviceInvalidJson = "Device returned invalid JSON while polling index";
        public const string LogUnexpectedPollingError = "Unexpected error while polling index";
        public const string LogDashboardStopping = "DashboardPoller stopping";
        public const string LogMetaRefreshFailed = "Failed to refresh sensor meta cache at startup";

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
        public const string MessageNotConnected = "Not connected";

        // Relay UI
        public const string NoneString = "None";
        public const string SavedOk = "✓ Saved successfully";
        public const string SavedFailed = "⚠ One or more commands failed";
        public const int ColorOptionNoneIndex = 6;
        public static readonly string[] ColorOptionNames = [ColorName_Blue, ColorName_Green, ColorName_Grey, ColorName_Orange, ColorName_Red, ColorName_Yellow, NoneString];
        public static readonly string[] ActionOptionNames = [ActionName_Default, ActionName_Horn, ActionName_NightRelay];
        public static readonly string[] DefaultStateOptionNames = [DefaultState_Off, DefaultState_On];
        public const string NavBack = "..";
        public const string RelayPageTitle = "Relay";
        public const string CheckMark = "✓";
        // Relay panel colours (order matches Nextion picture IDs with +2 offset)
        public const int RelayColorBlue = 0;
        public const int RelayColorGreen = 1;
        public const int RelayColorGrey = 2;
        public const int RelayColorOrange = 3;
        public const int RelayColorRed = 4;
        public const int RelayColorYellow = 5;
        // Nextion picture IDs for button colours (stored on device)
        public const int NextionImageIdBlue = 2;
        public const int NextionImageIdGreen = 3;
        public const int NextionImageIdGrey = 4;
        public const int NextionImageIdOrange = 5;
        public const int NextionImageIdRed = 6;
        public const int NextionImageIdYellow = 7;
        public const int NextionImageIdMin = NextionImageIdBlue;
        public const int NextionImageIdMax = NextionImageIdYellow;
        public const string ColorRelayPanelBlue = "#2255aa";
        public const string ColorRelayPanelGreen = "#22aa44";
        public const string ColorRelayPanelGrey = "#888888";
        public const string ColorRelayPanelOrange = "#dd7722";
        public const string ColorRelayPanelRed = "#cc3333";
        public const string ColorRelayPanelYellow = "#ccbb22";
        public const string WarningMark = "⚠";

        // Color option names
        public const string ColorName_Blue = "Blue";
        public const string ColorName_Green = "Green";
        public const string ColorName_Grey = "Grey";
        public const string ColorName_Orange = "Orange";
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
        public const int TimeSyncIntervalMinutes = 60;
        public const int TimeSyncDriftThresholdSeconds = 120;
        public const int MinimumValidDateTimeYear = 2000;
        public const string DeviceTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string RouteApiIndex = "api/index";
        public const string RouteSaveConfig = "api/config/C0";
        public const string RouteOtaUpdate = "api/system/F13";
        public const string RouteUpdateOta = "api/system/F12?apply=1";
        public const string RouteSystemPins = "api/system/F15";
        public const string RouteSystemPinRestrictions = "api/system/F16";
        public const string RouteSystemGetDateTime = "api/system/F7";
        public const string RouteSystemSetDateTime = "api/system/F6";
        public const string RouteConfigTimezoneOffset = "api/config/C20";
        public const string RouteConfigSdCardSpiPins = "api/config/C4";
        public const string RouteConfigSdCardInitSpeed = "api/config/C31";
        public const string RouteConfigSdCardCsPin = "api/config/C32";
        // RTC config
        public const string RouteConfigRtc = "api/config/C18";
        public const string RouteRtcSettingsPage = "RtcSettingsPage";
        public const string RtcMsgSaveFailed = "Save failed — device unreachable";
        public const string RtcMsgSaved = "RTC settings saved";
        public const string RtcMsgRefreshed = "Refreshed";
        public const string JsonRtcDataPin = "dat";
        public const string JsonRtcClockPin = "clk";
        public const string JsonRtcResetPin = "rst";
        public const int RtcPinDisabled = 255;
        // XpdzTone / Buzzer config
        public const string RouteConfigXpdzTone = "api/config/C6";
        public const string RouteXpdzToneSettingsPage = "XpdzToneSettingsPage";
        public const string XpdzToneMsgSaveFailed = "Save failed — device unreachable";
        public const string XpdzToneMsgSaved = "Buzzer settings saved";
        public const string XpdzToneMsgRefreshed = "Refreshed";
        public const string JsonXpdzPin = "xpdzPin";
        // Nextion display config
        public const string RouteConfigNextionGet = "api/config/N0";
        public const string RouteConfigNextionSetFormat = "api/config/N{0}?v={1}";
        public const string RouteNextionSettingsPage = "NextionSettingsPage";
        public const string NextionMsgSaveFailed = "Save failed — device unreachable";
        public const string NextionMsgSaved = "Nextion settings saved";
        public const string NextionMsgRefreshed = "Refreshed";
        public const string NextionUart1 = "UART1";
        public const string NextionUart2 = "UART2";
        public const string NextionRebootTitle = "Reboot Required";
        public const string NextionRebootMessage = "Changes require a device reboot to take effect. Reboot device now? (If the device has no reboot endpoint you will need to power-cycle it manually)";
        public const string NextionRebootButton = "Reboot";
        public const string NextionRebootOk = "OK";
        public const string NextionRebootSaved = "Settings saved. Please power-cycle the device to apply Nextion configuration.";
        public const string NextionRebootFailed = "Failed to save settings before reboot.";
        public const string NextionRouteN1 = "api/config/N1";
        public const string NextionRouteN2 = "api/config/N2";
        public const string NextionRouteN3 = "api/config/N3";
        public const string NextionRouteN4 = "api/config/N4";
        public const string NextionRouteN5 = "api/config/N5";
        public const string NextionRouteN6 = "api/config/N6";
        // Nextion JSON property names (N0 response)
        public const string NextionJsonN1 = "n1";
        public const string NextionJsonN2 = "n2";
        public const string NextionJsonN3 = "n3";
        public const string NextionJsonN4 = "n4";
        public const string NextionJsonN5 = "n5";
        public const string NextionJsonN6 = "n6";
        public const string NextionJsonEnabled = "enabled";
        public const string NextionJsonHardwareSerial = "hardwareserial";
        public const string NextionJsonRxPin = "rxpin";
        public const string NextionJsonTxPin = "txpin";
        public const string NextionJsonBaudRate = "baudrate";
        public const string NextionJsonUartNumber = "uartnumber";
        public const string NextionJsonEn = "en";
        public const string NextionJsonHw = "hw";
        public const string NextionJsonRx = "rx";
        public const string NextionJsonTx = "tx";
        public const string NextionJsonBaud = "baud";
        public const string NextionJsonUart = "uart";
        public const string RouteConfigAuth = "api/config/C19";
        public const string ConfigAuthEnabledParam = "e";
        public const string ConfigAuthApiKeyParam = "k";
        public const string ConfigAuthHmacKeyParam = "h";
        public const string ConfigAuthGenerateParam = "g";
        public const string RouteConfigMqttGet = "api/mqtt/{0}";
        public const string RouteConfigMqttSet = "api/mqtt/{0}?v={1}";
        public const string MqttConfigEnabled = "M0";
        public const string MqttConfigBroker = "M1";
        public const string MqttConfigPort = "M2";
        public const string MqttConfigUsername = "M3";
        public const string MqttConfigPassword = "M4";
        public const string MqttConfigDeviceId = "M5";
        public const string MqttConfigHADiscovery = "M6";
        public const string MqttConfigKeepAlive = "M7";
        public const string MqttConfigState = "M8";
        public const string MqttConfigDiscoveryPrefix = "M9";
        public const string MqttConnectedLabel = "Connected";
        public const string MqttDisconnectedLabel = "Disconnected";
        public const string MqttMsgSaveFailed = "Save failed — device unreachable";
        public const string MqttMsgSaved = "MQTT settings saved";
        public const string SdCardMsgSaveFailed = "Save failed — device unreachable";
        public const string SdCardMsgSaved = "SD Card settings saved";
        public const string SdCardMsgPartiallySaved = "⚠ Partially saved — SPI pins rejected (check for pin conflicts)";
        public const string SdCardMsgSpiFailed = "⚠ SPI pins not saved (pin conflict or invalid pin)";
        public const string SdCardMsgRefreshed = "Refreshed";
        public const int SdCardDefaultSpeed = 4;
        public const int SdCardPinDisabled = 255;
        public const string SdCardConfigSpiPins = "C4";
        public const string SdCardConfigInitSpeed = "C31";
        public const string SdCardConfigCsPin = "C32";
        public const string NetworkSecurityMsgSaveFailed = "Save failed — device unreachable";
        public const string NetworkSecurityMsgSaved = "Network security settings saved";
        public const string NetworkSecurityMsgKeysGenerated = "New keys generated and saved";
        public const string NetworkSecurityMsgRefreshed = "Refreshed";
        public const string NetworkSecurityMsgGenerateFailed = "Key generation failed — device unreachable";
        public const string RouteWarnings = "api/warning/W5";
        public const string ForwardSlash = "/";
        public const string ResultSuccess = "success";
        public const string JsonSpiSckKey = "sck";
        public const string JsonSpiMosiKey = "mosi";
        public const string JsonSpiMisoKey = "miso";
        public const string RouteDashboardPage = "//DashboardPage";
        public const string RouteSettingsPage = "//SettingsPage";
        public const string RouteSystemPage = "//SystemPage";
        public const string RouteTimeSettingsPage = "TimeSettingsPage";
        public const string RouteMqttSettingsPage = "MqttSettingsPage";
        public const string RouteSdCardSettingsPage = "SdCardSettingsPage";
        public const string RouteNetworkSecurityPage = "NetworkSecurityPage";
        public const string ConnectionTypeKey = "X-Connection-Type";
        public const string ConnectionTypePersistent = "persistent";
        public const string HeaderApiKey = "X-API-Key";
        public const string HeaderAuthTimestamp = "X-Auth-Timestamp";
        public const string HeaderAuthSignature = "X-Auth-Signature";
        public const string HmacSignSeparator = "\n";
        public const string HmacHexDash = "-";
        public const int MaximumPermanentConnections = 2;
        public const int SecondsSixty = 60;
        public const int SecondsTen = 10;
        public const int SecondsFive = 5;
        public const int SecondsThree = 3;
        private const int TZOffsetRawMinValue = 12;
        private const int TZOffsetRawMaxValue = 14;
        public const int TimezoneOffsetMin = -TZOffsetRawMinValue;
        public const int TimezoneOffsetMax = TZOffsetRawMaxValue;
        public const int SecondsTwo = 2;
        public const string UserAgentKey = "User-Agent";
        public const string UserAgentValue = "PowerControlHub/1.0";
        public const string PowerHubNotConfigured = "PowerHubService is not configured. Call Configure() first.";
        public const string ErrorKey = "error";
        public const char QuoteChar = '"';
        public const string PreferenceKey = "app_theme";
        public const string ThemeLight = "Light";
        public const string ThemeDark = "Dark";
        public const string BoolFalseString = "0";
        public const string BoolTrueString = "1";


        public const int KilobyteBytes = 1024;
        public const int DefaultDecimalPlaces = 2;

        // External sensor API route and JSON property names
        public const string RouteExternalSensor = "api/externalsensor/";
        public const string RouteLocalSensorConfig = "api/sensor/";
        public const string JsonSensors = "sensors";
        public const string JsonSensorIndex = "i";
        public const string JsonSensorId = "id";
        public const string JsonSensorName = "n";
        public const string JsonSensorMqttName = "mn";
        public const string JsonSensorMqttSlug = "ms";
        public const string JsonSensorMqttType = "mt";
        public const string JsonSensorMqttDeviceClass = "md";
        public const string JsonSensorMqttUnit = "mu";
        public const string JsonSensorMqttBinary = "bin";
        public const string JsonValueKey = "v";
        public const char JsonObjectOpen = '{';
        public const char JsonObjectClose = '}';

        public const string ErrRemoveCommandFailed = "⚠ Remove command failed";

        // Local sensor config JSON property names
        public const string JsonLocalSensorIndex = "i";
        public const string JsonLocalSensorType = "t";
        public const string JsonLocalSensorName = "n";
        public const string JsonLocalSensorPin0 = "p0";
        public const string JsonLocalSensorPin1 = "p1";
        public const string JsonLocalSensorOpt1_0 = "u0";
        public const string JsonLocalSensorOpt1_1 = "u1";
        public const string JsonLocalSensorOpt2_0 = "o0";
        public const string JsonLocalSensorOpt2_1 = "o1";
        public const string JsonLocalSensorEnabled = "en";

        // Sensor config command strings
        public const string SensorConfigGetAll = "S0";

        // Sensor meta JSON property names (S0?meta=1 response)
        public const string JsonMeta = "meta";
        public const string JsonMetaCount = "count";
        public const string JsonMetaDescriptors = "descriptors";

        // JSON key for sensor type in the dashboard /api/index response
        public const string SensorTypeJsonKey = "type";

        // Sensor type display names
        public const string SensorTypeWater = "Water";
        public const string SensorTypeDht11 = "DHT11";
        public const string SensorTypeLight = "Light";
        public const string SensorTypeGps = "GPS";
        public const string SensorTypeSystem = "System";
        public const string SensorTypeBinaryPresence = "Binary Presence";
        public const string SensorTypeVoltage = "Voltage";
        public const string SensorTypeUnknown = "Unknown";
        public const string SensorTypeMetaDataUnavailable = "Sensor type metadata unavailable.";
        public const string FailAddUpdateSensor = "add/update sensor entry";
        public const string FailEnableDisableSensor = "change enabled state";
        public const string FailSaveSettings = "save settings to disk";
        public const string FailSeparator = "; ";
        public const string FailPrefix = "⚠ Failed to: ";

        // Sensor type display names with enum values for picker
        public const string SensorTypeWaterPicker = "Water (0)";
        public const string SensorTypeDht11Picker = "DHT11 (1)";
        public const string SensorTypeLightPicker = "Light (2)";
        public const string SensorTypeGpsPicker = "GPS (3)";
        public const string SensorTypeSystemPicker = "System (4)";
        public const string SensorTypeBinaryPresencePicker = "Binary Presence (5)";
        public const string SensorTypeVoltagePicker = "Voltage (6)";

        // Sensor page UI
        public const string SensorPageTitle = "Sensor";
        public const string SensorNotConfigured = "Not configured";
        public const string MsgRemoveSensor = "Remove Sensor";
        public const string MsgRemove = "Remove";
        public const string MsgCancel = "Cancel";
        public const string MsgPinSummaryFormat = "Pin {0}, {1}";
        public const string MsgPinFormat = "Pin {0}";

        // Sensor type enum values
        public const int SensorEnumWater = 0;
        public const int SensorEnumDht11 = 1;
        public const int SensorEnumLight = 2;
        public const int SensorEnumGps = 3;
        public const int SensorEnumSystem = 4;
        public const int SensorEnumBinaryPresence = 5;
        public const int SensorEnumVoltage = 6;

        // Sensor telemetry JSON property names (firmware sensor keys in /api/index)
        public const string JsonSensorIdType = "idType";
        public const string JsonSensorUid = "uid";
        public const string JsonSensorTemperature = "temperature";
        public const string JsonSensorHumidity = "humidity";
        public const string JsonSensorDewPoint = "dew_point";
        public const string JsonSensorComfort = "comfort";
        public const string JsonSensorCondensationRisk = "condensation_risk";
        public const string JsonSensorIsDaytime = "isDaytime";
        public const string JsonSensorLightLevel = "lightLevel";
        public const string JsonSensorAvgLightLevel = "avgLightLevel";
        public const string JsonSensorGps = "gps";
        public const string JsonSensorGpsLat = "lat";
        public const string JsonSensorGpsLon = "lon";
        public const string JsonSensorGpsAlt = "alt";
        public const string JsonSensorGpsSpeed = "speed";
        public const string JsonSensorGpsCourse = "course";
        public const string JsonSensorGpsSats = "sats";
        public const string JsonSensorGpsValid = "valid";
        public const string JsonSensorFreeMemory = "freeMemory";
        public const string JsonSensorCpuUsage = "cpuUsage";
        public const string JsonSensorState = "state";
        public const string JsonSensorVoltage = "voltage";
        public const string JsonSensorVoltageAvg = "avg";
        public const string JsonSensorWaterLevel = "level";
        public const string JsonSensorWaterLevelAvg = "average";
        public const string JsonSensorType = "type";

        // Sensor telemetry format strings
        public const string SensorFmtTempDouble = "0.0";
        public const string SensorFmtDouble1 = "0.0";
        public const string SensorFmtDouble2 = "0.00";
        public const string SensorFmtGpsCoord = "0.000000";

        // Sensor telemetry unit suffixes
        public const string SensorSuffixCelsius = "°C";
        public const string SensorSuffixPercent = "%";
        public const string SensorSuffixKb = " kb";
        public const string SensorSuffixKmh = " km/h";
        public const string SensorSuffixVolt = " V";
        public const string SensorSuffixMetre = " m";
        public const string SensorSuffixDegree = "°";

        // Sensor telemetry binary presence states
        public const string SensorStateDetected = "detected";
        public const string SensorStateClear = "clear";

        // Sensor telemetry display labels
        public const string SensorLabelDay = "Day";
        public const string SensorLabelNight = "Night";
        public const string SensorLabelFix = "Fix";
        public const string SensorLabelNoFix = "No Fix";
        public const string SensorLabelDetected = "Detected";
        public const string SensorLabelClear = "Clear";

        // Sensor telemetry icon literals
        public const string SensorIconSun = "☀";
        public const string SensorIconMoon = "🌙";
        public const string SensorIconRedCircle = "🔴";
        public const string SensorIconGreenCircle = "🟢";

        // Sensor telemetry generic fallback separator
        public const string SensorSummarySeparator = "; ";

        // Sensor detail page default label strings
        public const string LabelPin0Default = "GPIO Pin 0 (blank or 255 = disabled)";
        public const string LabelPin1Default = "GPIO Pin 1 (blank or 255 = disabled)";
        public const string LabelOpt1_0Default = "Option 1 (int8)";
        public const string LabelOpt1_1Default = "Option 2 (int8)";
        public const string LabelOpt2_0Default = "Option 3 (int16)";
        public const string LabelOpt2_1Default = "Option 4 (int16)";

        // ConfigConnection constants
        public const int ConfigConnectionQueueCapacity = 64;
        public const string LogConfigExecuting = "ConfigConnection executing: {Description}";
        public const string LogConfigSucceeded = "ConfigConnection succeeded: {Description}";
        public const string LogConfigFailed = "ConfigConnection failed: {Description}";
        public const string LogConfigFailureResponse = "Device returned failure response";
        public const string LogConfigConsumerError = "ConfigConnection consumer error";
        public const string LogPublishSuccessFailed = "Failed to publish success message for: {Description}";
        public const string LogPublishFailureFailed = "Failed to publish failure message for: {Description}";
        public const string ConfigSuccessRelay = "RelayConfigSucceeded";
        public const string ConfigSuccessSensor = "SensorConfigSucceeded";

        // ConfigPoller log messages
        public const string LogConfigMetaRefreshed = "ConfigPoller: SensorMetaCache refreshed after connection.";
        public const string LogConfigHealthCheckFailed = "ConfigPoller: Connection health check failed.";

        // TimeSyncService log messages
        public const string LogTimeSyncStarted = "TimeSyncService started with interval {IntervalMin}min, drift threshold {DriftSec}s";
        public const string LogTimeSyncDeviceTime = "TimeSyncService: device time is {DeviceTime}, delta {DeltaSec}s";
        public const string LogTimeSyncSetting = "TimeSyncService: drift {DeltaSec}s exceeds threshold, setting device time to {LocalTime}";
        public const string LogTimeSyncInSync = "TimeSyncService: device time is in sync (delta {DeltaSec}s)";
        public const string LogTimeSyncNotSet = "TimeSyncService: device time not set, synchronizing";
        public const string LogTimeSyncFailed = "TimeSyncService: time sync attempt failed";
        public const string LogTimeSyncStopping = "TimeSyncService stopping";

        // MauiProgram startup orchestration
        public const string LogStartupMetaFetch = "Startup: first dashboard data received, fetching sensor meta on connection 2.";
        public const string LogStartupMetaPopulated = "Startup: sensor meta cache populated.";
        public const string LogStartupMetaAlready = "Startup: dashboard data already available, fetching sensor meta on connection 2.";

        public const string NullByte = "0x00";
        public const string NibbleZero = "0x0";
    }

#pragma warning restore CC0003
}
