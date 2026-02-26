#pragma once

#include <WiFi.h>
#include "IWifiRadio.h"

class Esp32WifiRadio : public IWifiRadio
{
public:
    bool beginAP(
        const char* ssid,
        const char* password,
        IPAddress ip,
        IPAddress subnet) override
    {
        WiFi.mode(WIFI_AP);
        WiFi.softAPConfig(ip, ip, subnet);

        if (password && strlen(password) > 0)
        {
            return WiFi.softAP(ssid, password);
        }

        return WiFi.softAP(ssid);
    }

    bool beginClient(const char* ssid, const char* password) override
    {
        WiFi.mode(WIFI_STA);
        WiFi.begin(ssid, password);
        return true;
    }

    void disconnect() override
    {
        WiFi.disconnect();
    }

    void end() override
    {
        WiFi.disconnect(true);
        WiFi.mode(WIFI_OFF);
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
        return true;
    }
};