#pragma once

#include <Arduino.h>

class IWifiRadio
{
public:
    virtual ~IWifiRadio() = default;

    virtual bool beginAP(
        const char* ssid,
        const char* password,
        IPAddress ip,
        IPAddress subnet) = 0;

    virtual bool beginClient(
        const char* ssid,
        const char* password) = 0;

    virtual void disconnect() = 0;
    virtual void end() = 0;
    virtual int status() = 0;
    virtual int32_t rssi() = 0;
    virtual IPAddress localIP() = 0;
    virtual bool hasModule() = 0;
};