#include "Local.h"

#if defined(MQTT_SUPPORT)

#include "MQTTSensorHandler.h"
#include "SystemDefinitions.h"
#include <SerialCommandManager.h>
#include <string.h>
#include <stdio.h>

MQTTSensorHandler::MQTTSensorHandler(MQTTController* mqttController, MessageBus* messageBus, SerialCommandManager* commandMgr)
    : MQTTHandler(mqttController, messageBus)
    , _config(nullptr)
    , _commandMgr(commandMgr)
    , _temperature(0.0f)
    , _humidity(0)
    , _isDaytime(true)
    , _waterLevel(0)
    , _discoveryPending(false)
    , _discoveryIndex(0)
    , _lastDiscoveryPublish(0)
    , _dirtySensorMask(0)
    , _lastStatePublish(0)
{
    _config = ConfigManager::getConfigPtr();
}

bool MQTTSensorHandler::begin()
{
    if (_mqttController == nullptr || _messageBus == nullptr || _config == nullptr)
    {
        if (_commandMgr != nullptr)
        {
            _commandMgr->sendError(F("Required components not available"), F("MQTT Sensor"));
        }
        return false;
    }

    _messageBus->subscribe<TemperatureUpdated>(
        [this](float temp)
        {
            this->onTemperatureUpdated(temp);
        }
    );

    _messageBus->subscribe<HumidityUpdated>(
        [this](uint8_t humidity)
        {
            this->onHumidityUpdated(humidity);
        }
    );

    _messageBus->subscribe<LightSensorUpdated>(
        [this](bool isDaytime)
        {
            this->onLightSensorUpdated(isDaytime);
        }
    );

    _messageBus->subscribe<WaterLevelUpdated>(
        [this](uint16_t waterLevel, uint16_t averageWaterLevel)
        {
            this->onWaterLevelUpdated(waterLevel, averageWaterLevel);
        }
    );

    _messageBus->subscribe<MqttConnected>(
        [this]()
        {
            this->subscribe();

            if (_config != nullptr && _config->mqtt.useHomeAssistantDiscovery)
            {
                _discoveryPending = true;
                _discoveryIndex = 0;
            }

            this->markAllSensorsDirty();
        }
    );

    return true;
}

void MQTTSensorHandler::update()
{
    unsigned long now = millis();

    if (_discoveryPending && _discoveryIndex < SensorDiscoveryCount)
    {
        if (_mqttController != nullptr && _mqttController->isConnected())
        {
            if (now - _lastDiscoveryPublish >= MinPublishInterval)
            {
                publishSensorDiscoveryConfig(_discoveryIndex);
                _lastDiscoveryPublish = now;
                _discoveryIndex++;

                if (_discoveryIndex >= SensorDiscoveryCount)
                {
                    if (_commandMgr != nullptr)
                    {
                        _commandMgr->sendDebug(F("Sensor discovery complete"), F("MQTT"));
                    }
                    _discoveryPending = false;
                }
            }
        }
    }

    if (_dirtySensorMask != 0)
    {
        unsigned long timeSinceLastPublish = now - _lastStatePublish;

        if (timeSinceLastPublish >= MinPublishInterval || timeSinceLastPublish >= MaxPublishInterval)
        {
            publishDirtySensorStates();
        }
    }
}

void MQTTSensorHandler::end()
{
    unsubscribe();
    _isSubscribed = false;
}

void MQTTSensorHandler::onMessage(const char* topic, const char* payload)
{
    // Sensors are read-only; no commands expected
    (void)topic;
    (void)payload;
}

bool MQTTSensorHandler::subscribe()
{
    // Sensors publish only; no MQTT topics to subscribe to
    _isSubscribed = true;
    return true;
}

void MQTTSensorHandler::unsubscribe()
{
    _isSubscribed = false;
}

// ============================================================================
// Private Methods
// ============================================================================

void MQTTSensorHandler::onTemperatureUpdated(float temperature)
{
    _temperature = temperature;
    markSensorDirty(SensorDirtyTemperature);
}

void MQTTSensorHandler::onHumidityUpdated(uint8_t humidity)
{
    _humidity = humidity;
    markSensorDirty(SensorDirtyHumidity);
}

void MQTTSensorHandler::onLightSensorUpdated(bool isDaytime)
{
    _isDaytime = isDaytime;
    markSensorDirty(SensorDirtyLight);
}

void MQTTSensorHandler::onWaterLevelUpdated(uint16_t waterLevel, uint16_t averageWaterLevel)
{
    (void)waterLevel;
    _waterLevel = averageWaterLevel;
    markSensorDirty(SensorDirtyWater);
}

void MQTTSensorHandler::publishSensorState(uint8_t sensorBit)
{
    if (_mqttController == nullptr || _config == nullptr)
    {
        return;
    }

    if (!_mqttController->isConnected())
    {
        return;
    }

    char topic[MqttMaxTopicLength];
    char payload[32];

    switch (sensorBit)
    {
    case SensorDirtyTemperature:
        snprintf(topic, sizeof(topic), "home/%s/sensor/temperature/state", _config->mqtt.deviceId);
        dtostrf(_temperature, 1, 1, payload);
        break;

    case SensorDirtyHumidity:
        snprintf(topic, sizeof(topic), "home/%s/sensor/humidity/state", _config->mqtt.deviceId);
        snprintf(payload, sizeof(payload), "%u", _humidity);
        break;

    case SensorDirtyLight:
        snprintf(topic, sizeof(topic), "home/%s/sensor/light/state", _config->mqtt.deviceId);
        snprintf(payload, sizeof(payload), "%s", _isDaytime ? "ON" : "OFF");
        break;

    case SensorDirtyWater:
        snprintf(topic, sizeof(topic), "home/%s/sensor/water_level/state", _config->mqtt.deviceId);
        snprintf(payload, sizeof(payload), "%u", _waterLevel);
        break;

    default:
        return;
    }

    if (_commandMgr != nullptr)
    {
        _commandMgr->sendDebug(topic, F("MQTT Sensor State"));
    }

    _mqttController->publishState(topic, payload);
}

void MQTTSensorHandler::publishDirtySensorStates()
{
    if (_mqttController == nullptr)
    {
        return;
    }

    if (!_mqttController->isConnected())
    {
        return;
    }

    const uint8_t sensorBits[] = { SensorDirtyTemperature, SensorDirtyHumidity, SensorDirtyLight, SensorDirtyWater };

    for (uint8_t i = 0; i < SensorDiscoveryCount; i++)
    {
        if (_dirtySensorMask & sensorBits[i])
        {
            publishSensorState(sensorBits[i]);
        }
    }

    _dirtySensorMask = 0;
    _lastStatePublish = millis();
}

void MQTTSensorHandler::markSensorDirty(uint8_t sensorBit)
{
    _dirtySensorMask |= sensorBit;
}

void MQTTSensorHandler::markAllSensorsDirty()
{
    _dirtySensorMask = SensorDirtyAll;
}

void MQTTSensorHandler::publishDiscoveryConfig()
{
    _discoveryPending = true;
    _discoveryIndex = 0;
}

void MQTTSensorHandler::publishSensorDiscoveryConfig(uint8_t index)
{
    if (_mqttController == nullptr || _config == nullptr)
    {
        return;
    }

    if (!_mqttController->isConnected())
    {
        return;
    }

    if (index >= SensorDiscoveryCount)
    {
        return;
    }

    MQTTClient* client = _mqttController->getClient();
    if (client == nullptr)
    {
        return;
    }

    char topic[MqttMaxTopicLength];
    char payload[MqttMaxPayloadLength];

    switch (index)
    {
    case 0: // Temperature
        snprintf(topic, sizeof(topic), "%s/sensor/%s/temperature/config",
            _config->mqtt.discoveryPrefix, _config->mqtt.deviceId);
        snprintf(payload, sizeof(payload),
            "{\"name\":\"Temperature\","
            "\"state_topic\":\"home/%s/sensor/temperature/state\","
            "\"device_class\":\"temperature\","
            "\"unit_of_measurement\":\"\xc2\xb0" "C\","
            "\"unique_id\":\"%s_temperature\","
            "\"device\":{\"ids\":[\"%s\"],\"name\":\"Smart Fuse Box\",\"mf\":\"Simon Carter\",\"mdl\":\"SFB v1\"}}",
            _config->mqtt.deviceId,
            _config->mqtt.deviceId,
            _config->mqtt.deviceId);
        break;

    case 1: // Humidity
        snprintf(topic, sizeof(topic), "%s/sensor/%s/humidity/config",
            _config->mqtt.discoveryPrefix, _config->mqtt.deviceId);
        snprintf(payload, sizeof(payload),
            "{\"name\":\"Humidity\","
            "\"state_topic\":\"home/%s/sensor/humidity/state\","
            "\"device_class\":\"humidity\","
            "\"unit_of_measurement\":\"%%\","
            "\"unique_id\":\"%s_humidity\","
            "\"device\":{\"ids\":[\"%s\"],\"name\":\"Smart Fuse Box\",\"mf\":\"Simon Carter\",\"mdl\":\"SFB v1\"}}",
            _config->mqtt.deviceId,
            _config->mqtt.deviceId,
            _config->mqtt.deviceId);
        break;

    case 2: // Light (binary sensor)
        snprintf(topic, sizeof(topic), "%s/binary_sensor/%s/light/config",
            _config->mqtt.discoveryPrefix, _config->mqtt.deviceId);
        snprintf(payload, sizeof(payload),
            "{\"name\":\"Daylight\","
            "\"state_topic\":\"home/%s/sensor/light/state\","
            "\"device_class\":\"light\","
            "\"payload_on\":\"ON\","
            "\"payload_off\":\"OFF\","
            "\"unique_id\":\"%s_light\","
            "\"device\":{\"ids\":[\"%s\"],\"name\":\"Smart Fuse Box\",\"mf\":\"Simon Carter\",\"mdl\":\"SFB v1\"}}",
            _config->mqtt.deviceId,
            _config->mqtt.deviceId,
            _config->mqtt.deviceId);
        break;

    case 3: // Water level
        snprintf(topic, sizeof(topic), "%s/sensor/%s/water_level/config",
            _config->mqtt.discoveryPrefix, _config->mqtt.deviceId);
        snprintf(payload, sizeof(payload),
            "{\"name\":\"Water Level\","
            "\"state_topic\":\"home/%s/sensor/water_level/state\","
            "\"unique_id\":\"%s_water_level\","
            "\"device\":{\"ids\":[\"%s\"],\"name\":\"Smart Fuse Box\",\"mf\":\"Simon Carter\",\"mdl\":\"SFB v1\"}}",
            _config->mqtt.deviceId,
            _config->mqtt.deviceId,
            _config->mqtt.deviceId);
        break;

    default:
        return;
    }

    client->publish(topic, payload, MqttQoS::AtMostOnce, true);
}

#endif