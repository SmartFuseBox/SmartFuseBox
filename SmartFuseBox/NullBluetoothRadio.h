#pragma once

#include "IBluetoothRadio.h"

// Null implementation of IBluetoothRadio for boards without Bluetooth hardware.
// All methods return safe defaults or failure states.
// Allows Bluetooth subsystem code to compile and gracefully handle missing hardware at runtime.
class NullBluetoothRadio : public IBluetoothRadio
{
public:
    bool isEnabled() const override
    {
        return false;
    }

    bool setEnabled(bool enabled) override
    {
        (void)enabled;
        return false;
    }

    void applyConfig(const Config* cfg) override
    {
        (void)cfg;
    }

    void loop() override
    {
    }

    void advertise() override
    {
    }

    void stopAdvertise() override
    {
    }

    void setLocalName(const char* name) override
    {
        (void)name;
    }

    void setDeviceName(const char* name) override
    {
        (void)name;
    }

    void poll() override
    {
    }

    bool isConnected() const override
    {
        return false;
    }

    void setAdvertisedServiceUUID(const char* uuid) override
    {
        (void)uuid;
    }

    void setAdvertisedService(void* service) override
    {
        (void)service;
    }
};
