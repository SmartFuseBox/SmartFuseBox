#pragma once

#include <WiFiS3.h>
#include "IWifiRadio.h"

class R4WifiRadio : public IWifiRadio
{
public:
    bool beginAP(
        const char* ssid,
        const char* password,
        IPAddress ip,
        IPAddress subnet) override
    {
        WiFi.config(ip, ip, subnet);

        if (password && strlen(password) > 0)
        {
            return WiFi.beginAP(ssid, password);
        }

        return WiFi.beginAP(ssid);
    }

    bool beginClient(const char* ssid, const char* password) override
    {
        WiFi.begin(ssid, password);
        return true;
    }

    void disconnect() override
    {
        WiFi.disconnect();
    }

    void end() override
    {
        WiFi.end();
    }

    int status() override
    {
        return WiFi.status();
    }

    int32_t rssi() override
    {
        return WiFi.RSSI();
    }

    IPAddress localIP() override
    {
        return WiFi.localIP();
    }

    bool hasModule() override
    {
        return WiFi.status() != WL_NO_MODULE;
    }
};