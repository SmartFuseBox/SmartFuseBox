#pragma once

#include "IWifiRadio.h"

// Null implementation of IWifiRadio for boards without WiFi hardware.
// All methods return safe defaults or failure states.
// Allows WiFi subsystem code to compile and gracefully handle missing hardware at runtime.
class NullWifiRadio : public IWifiRadio
{
public:
    bool beginAP(
        const char* ssid,
        const char* password,
        IPAddress ip,
        IPAddress subnet) override
    {
		(void)ssid;
        (void)password;
		(void)ip;
		(void)subnet;
        return false;
    }

    bool beginClient(
        const char* ssid,
        const char* password) override
    {
        (void)ssid;
		(void)password;
        return false;
    }

    void disconnect() override
    {
    }

    void end() override
    {
    }

    int status() override
    {
        return 255;
    }

    int32_t rssi() override
    {
        return 0;
    }

    IPAddress localIP() override
    {
        return IPAddress(0, 0, 0, 0);
    }

    bool hasModule() override
    {
        return false;
    }
};
