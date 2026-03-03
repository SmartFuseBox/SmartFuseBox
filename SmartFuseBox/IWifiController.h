#pragma once

// Forward declarations to avoid heavy includes
struct Config;
class WifiServer;
class INetworkCommandHandler;
class NetworkJsonVisitor;

// Include only what's needed for the interface
#include "SystemDefinitions.h"

// Lightweight, platform-agnostic WiFi controller interface.
// Keep this header free of Arduino or platform headers so it compiles everywhere.
class IWifiController
{
public:
    virtual ~IWifiController() = default;

    // Return whether the controller is currently enabled.
    virtual bool isEnabled() const = 0;

    // Enable/disable the controller; return true on success.
    virtual bool setEnabled(bool enabled) = 0;

    // Apply configuration (may be called at runtime).
    virtual void applyConfig(const Config* config) = 0;

    // Per-loop processing called from the app loop.
    virtual void update(unsigned long currentMillis) = 0;

    // Register network command handlers (for dependency injection).
    virtual void registerHandlers(INetworkCommandHandler** handlers, size_t count) = 0;

    // Register JSON status visitors (for status reporting).
    virtual void registerJsonVisitors(NetworkJsonVisitor** visitors, uint8_t count) = 0;

    // Get the underlying server instance (nullable) - use sparingly, prefer interface methods.
    virtual WifiServer* getServer() = 0;

    // Get connection state without accessing WifiServer directly.
    virtual WifiConnectionState getConnectionState() const = 0;

    // Get IP address without accessing WifiServer directly.
    virtual void getIpAddress(char* buffer, size_t bufferSize) const = 0;
};
