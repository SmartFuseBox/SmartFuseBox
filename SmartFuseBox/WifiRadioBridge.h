#pragma once

#include "Local.h"

#if defined(WIFI_SUPPORT)
    #if defined(ESP32)
        #include "Esp32WifiRadio.h"
        using PlatformWifiRadio = Esp32WifiRadio;
    #else
        #include "R4WifiRadio.h"
        using PlatformWifiRadio = R4WifiRadio;
    #endif
#else
    #include "NullWifiRadio.h"
    using PlatformWifiRadio = NullWifiRadio;
#endif
