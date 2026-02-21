#pragma once
#include "Local.h"

#if defined(MQTT_SUPPORT)

#include "MQTTHandler.h"
#include "ConfigManager.h"

// Forward declaration
class SerialCommandManager;

// Bitmask positions for dirty sensor tracking
constexpr uint8_t SensorDirtyTemperature = 0x01;
constexpr uint8_t SensorDirtyHumidity = 0x02;
constexpr uint8_t SensorDirtyLight = 0x04;
constexpr uint8_t SensorDirtyWater = 0x08;
constexpr uint8_t SensorDirtyAll = 0x0F;
constexpr uint8_t SensorDiscoveryCount = 4;

class MQTTSensorHandler : public MQTTHandler
{
private:
    Config* _config;
    SerialCommandManager* _commandMgr;

    // Latest sensor values
    float _temperature;
    uint8_t _humidity;
    bool _isDaytime;
    uint16_t _waterLevel;

    // Discovery state
    bool _discoveryPending;
    uint8_t _discoveryIndex;
    unsigned long _lastDiscoveryPublish;

    // Throttled state publishing
    uint8_t _dirtySensorMask;
    unsigned long _lastStatePublish;
    static constexpr uint16_t MinPublishInterval = 200;
    static constexpr uint16_t MaxPublishInterval = 5000;

    // State publishing
    void publishSensorState(uint8_t sensorBit);
    void publishDirtySensorStates();
    void markSensorDirty(uint8_t sensorBit);
    void markAllSensorsDirty();

    // Home Assistant Discovery
    void publishDiscoveryConfig();
    void publishSensorDiscoveryConfig(uint8_t index);

    // MessageBus event handlers
    void onTemperatureUpdated(float temperature);
    void onHumidityUpdated(uint8_t humidity);
    void onLightSensorUpdated(bool isDaytime);
    void onWaterLevelUpdated(uint16_t waterLevel, uint16_t averageWaterLevel);

public:
    MQTTSensorHandler(MQTTController* mqttController, MessageBus* messageBus, SerialCommandManager* commandMgr = nullptr);

    bool begin() override;
    void update() override;
    void end() override;

    void onMessage(const char* topic, const char* payload) override;

    bool subscribe() override;
    void unsubscribe() override;
};

#endif