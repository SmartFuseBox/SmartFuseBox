#pragma once

// Forward declare Config to avoid heavy includes
struct Config;

// Lightweight, platform-agnostic Bluetooth radio interface.
// Keep this header free of Arduino or other platform headers so it compiles everywhere.
class IBluetoothRadio
{
public:
    virtual ~IBluetoothRadio() = default;

    // Return whether the radio is currently enabled.
    virtual bool isEnabled() const = 0;

    // Enable/disable the radio; return true on success.
    virtual bool setEnabled(bool enabled) = 0;

    // Apply configuration (may be called at runtime).
    virtual void applyConfig(const Config* cfg) = 0;

    // Per-loop processing called from the app loop.
    virtual void loop() = 0;

    // Start advertising BLE services.
    virtual void advertise() = 0;

    // Stop advertising BLE services.
    virtual void stopAdvertise() = 0;

    // Set the local device name.
    virtual void setLocalName(const char* name) = 0;

    // Set the device name.
    virtual void setDeviceName(const char* name) = 0;

    // Poll for BLE events.
    virtual void poll() = 0;

    // Check if a client is currently connected.
    virtual bool isConnected() const = 0;

    // Set an advertised service by UUID.
    virtual void setAdvertisedServiceUUID(const char* uuid) = 0;

    // Set an advertised service (platform-specific service object as void*).
    virtual void setAdvertisedService(void* service) = 0;
};

