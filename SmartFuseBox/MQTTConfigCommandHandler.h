#pragma once

#include "ConfigController.h"
#include "SystemDefinitions.h"

// Forward declaration
class MQTTController;
class SerialCommandManager;

class MQTTConfigCommandHandler
{
private:
    ConfigController* _configController;
    MQTTController* _mqttController;
    Config* _config;
    SerialCommandManager* _commandMgr;

    // Helper to parse uint16_t value from string
    bool parseUint16(const char* value, uint16_t* result);

public:
    MQTTConfigCommandHandler(ConfigController* configController, MQTTController* mqttController, SerialCommandManager* commandMgr = nullptr);

    // Process MQTT configuration commands (M0-M8)
    // Returns true on success, false on failure
    bool processCommand(const char* command, const char* params);
    
    // Individual command handlers
    bool handleMqttEnable(const char* params);
    bool handleMqttBroker(const char* params);
    bool handleMqttPort(const char* params);
    bool handleMqttUsername(const char* params);
    bool handleMqttPassword(const char* params);
    bool handleMqttDeviceId(const char* params);
    bool handleMqttHADiscovery(const char* params);
    bool handleMqttKeepAlive(const char* params);
    bool handleMqttState(const char* params);
    bool handleMqttDiscoveryPrefix(const char* params);
};
