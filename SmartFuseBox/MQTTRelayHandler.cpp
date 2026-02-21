#include "Local.h"

#if defined(MQTT_SUPPORT)

#include "MQTTRelayHandler.h"
#include "SystemDefinitions.h"
#include <string.h>
#include <stdio.h>

MQTTRelayHandler::MQTTRelayHandler(MQTTController* mqttController, MessageBus* messageBus, RelayController* relayController)
    : MQTTHandler(mqttController, messageBus)
    , _relayController(relayController)
    , _config(nullptr)
    , _discoveryPending(false)
    , _discoveryIndex(0)
    , _lastDiscoveryPublish(0)
    , _dirtyRelayMask(0)
    , _lastStatePublish(0)
{
    _commandTopic[0] = '\0';
    _config = ConfigManager::getConfigPtr();
}

bool MQTTRelayHandler::begin()
{
    if (_mqttController == nullptr || _messageBus == nullptr || _relayController == nullptr || _config == nullptr)
    {
        Serial.println(F("[MQTT Relay] Error: Required components not available"));
        return false;
    }

    // Subscribe to MessageBus events
    _messageBus->subscribe<RelayStatusChanged>(
        [this](uint8_t status)
        {
            this->onRelayStatusChanged(status);
        }
    );

    // Subscribe to MQTT connected event to re-subscribe and publish states
    _messageBus->subscribe<MqttConnected>(
        [this]()
        {
            this->subscribe();

            // Trigger discovery if enabled (will be processed in update())
            if (_config != nullptr && _config->mqtt.useHomeAssistantDiscovery)
            {
                _discoveryPending = true;
                _discoveryIndex = 0;
            }

            // Mark all relays as dirty - they will be published by update() cycle
            // This prevents spam on connect/reconnect
            this->markAllRelaysDirty();
        }
    );

    // Subscribe to MQTT messages
    _messageBus->subscribe<MqttMessageReceived>(
        [this](const char* topic, const char* payload)
        {
            this->onMessage(topic, payload);
        }
    );

    return true;
}

void MQTTRelayHandler::update()
{
    unsigned long now = millis();

    if (_discoveryPending && _discoveryIndex < ConfigRelayCount)
    {
        if (_mqttController != nullptr && _mqttController->isConnected())
        {
            // Only publish if enough time has elapsed since last discovery message (200ms minimum)
            if (now - _lastDiscoveryPublish >= MinPublishInterval)
            {
                publishRelayDiscoveryConfig(_discoveryIndex);
                _lastDiscoveryPublish = now;
                _discoveryIndex++;

                // Check if discovery is complete
                if (_discoveryIndex >= ConfigRelayCount)
                {
                    Serial.println(F("[MQTT] Home Assistant discovery complete"));
                    _discoveryPending = false;
                }
            }
        }
    }

    // Throttled state publishing
    // Publish if: (dirty AND min interval passed) OR max interval passed
    if (_dirtyRelayMask != 0)
    {
        unsigned long timeSinceLastPublish = now - _lastStatePublish;

        // Publish immediately if dirty and min interval passed
        // OR if max interval passed (periodic update)
        if (timeSinceLastPublish >= MinPublishInterval || timeSinceLastPublish >= MaxPublishInterval)
        {
            publishDirtyRelayStates();
        }
    }
}

void MQTTRelayHandler::end()
{
    unsubscribe();
    _isSubscribed = false;
}

void MQTTRelayHandler::onMessage(const char* topic, const char* payload)
{
    if (topic == nullptr || payload == nullptr)
    {
        return;
    }

    // Debug: log incoming MQTT message for troubleshooting
    Serial.print("MQTT message received: ");
    Serial.println(topic);
    Serial.print("MQTT payload: ");
    Serial.println(payload);

    // Check if this is a relay command topic (e.g., "home/<device>/relay/3/set")
    uint8_t relayIndex;
    if (extractIndexFromTopic(topic, "relay/", &relayIndex))
    {
        // Verify topic ends with "/set"
        if (strstr(topic, "/set") != nullptr)
        {
            handleRelayCommand(relayIndex, payload);
        }
    }
}

bool MQTTRelayHandler::subscribe()
{
    if (_mqttController == nullptr || _config == nullptr)
    {
        Serial.println(F("[MQTT Relay] Error: Controller or config not available"));
        return false;
    }

    MQTTClient* client = _mqttController->getClient();
    if (client == nullptr || !client->isConnected())
    {
        return false;
    }

    // Subscribe to relay command topics: homeassistant/<device>/relay/+/set
    // Subscribe to command topics that the device actually uses for commands
    // Command topics use the "home/<device>/relay/<index>/set" pattern
    char topic[MqttMaxTopicLength];
    snprintf(topic, sizeof(topic), "home/%s/relay/+/set", _config->mqtt.deviceId);

    bool result = client->subscribe(topic, MqttQoS::AtMostOnce);
    if (result)
    {
        _isSubscribed = true;
    }
    Serial.print("Subscribe: ");
    Serial.println(topic);

    return result;
}

void MQTTRelayHandler::unsubscribe()
{
    if (_mqttController == nullptr || _config == nullptr)
    {
        return;
    }
    
    MQTTClient* client = _mqttController->getClient();
    if (client == nullptr)
    {
        return;
    }
    
    // Unsubscribe from relay command topics
    char topic[MqttMaxTopicLength];
    snprintf(topic, sizeof(topic), "home/%s/relay/+/set", _config->mqtt.deviceId);
    Serial.print("Unsubscribe: ");
    Serial.println(topic);

    client->unsubscribe(topic);
    _isSubscribed = false;
}

// ============================================================================
// Private Methods
// ============================================================================

void MQTTRelayHandler::handleRelayCommand(uint8_t relayIndex, const char* payload)
{
    if (_relayController == nullptr || payload == nullptr)
    {
        return;
    }

    // Debug: log command handling entry
    Serial.print("Handling relay command for index: ");
    Serial.println(relayIndex);
    Serial.print("Command payload: ");
    Serial.println(payload);

    // Validate relay index
    if (relayIndex >= ConfigRelayCount)
    {
        Serial.print(F("[MQTT Relay] Error: Invalid relay index: "));
        Serial.println(relayIndex);
        return;
    }
    
    // Parse command (ON, OFF, TOGGLE, 1, 0)
    bool turnOn = false;
    
    if (strcasecmp(payload, "ON") == 0 || strcmp(payload, "1") == 0)
    {
        turnOn = true;
    }
    else if (strcasecmp(payload, "OFF") == 0 || strcmp(payload, "0") == 0)
    {
        turnOn = false;
    }
    else if (strcasecmp(payload, "TOGGLE") == 0)
    {
        // Toggle current state
        CommandResult statusResult = _relayController->getRelayStatus(relayIndex);
        if (statusResult.success)
        {
            bool currentState = (statusResult.status == 1);
            turnOn = !currentState;
        }
        else
        {
            return; // Failed to get current state
        }
    }
    else
    {
        // Unknown command
        Serial.print(F("[MQTT Relay] Unknown command payload: "));
        Serial.println(payload);
        return;
    }

    // Set relay state
    _relayController->setRelayState(relayIndex, turnOn);
    // Log result of setting relay
    CommandResult res = _relayController->getRelayStatus(relayIndex);
    Serial.print("Relay ");
    Serial.print(relayIndex);
    Serial.print(" set to ");
    Serial.println(res.success ? (res.status == 1 ? "ON" : "OFF") : "ERROR");

    // Publish state will be handled by RelayStatusChanged event
}

void MQTTRelayHandler::publishRelayState(uint8_t relayIndex, bool isOn)
{
    if (_mqttController == nullptr || _config == nullptr)
    {
        return;
    }

    if (!_mqttController->isConnected())
    {
        return;
    }

    // Validate relay index
    if (relayIndex >= ConfigRelayCount)
    {
        return;
    }

    // Build state topic: home/<device>/relay<index>/state
    char topic[MqttMaxTopicLength];
    snprintf(topic, sizeof(topic), "home/%s/relay/%u/state", _config->mqtt.deviceId, relayIndex);

    Serial.print("Relay State: ");
	Serial.println(topic);

    // Publish state
    const char* payload = isOn ? "ON" : "OFF";
    _mqttController->publishState(topic, payload);
}

void MQTTRelayHandler::publishDirtyRelayStates()
{
    if (_mqttController == nullptr || _relayController == nullptr)
    {
        return;
    }

    if (!_mqttController->isConnected())
    {
        return;
    }

    // Publish state for each dirty relay
    for (uint8_t i = 0; i < ConfigRelayCount; i++)
    {
        if (_dirtyRelayMask & (1 << i))
        {
            CommandResult statusResult = _relayController->getRelayStatus(i);
            if (statusResult.success)
            {
                bool isOn = (statusResult.status == 1);
                publishRelayState(i, isOn);
            }
        }
    }

    // Clear dirty mask and update last publish time
    _dirtyRelayMask = 0;
    _lastStatePublish = millis();
}

void MQTTRelayHandler::markRelayDirty(uint8_t relayIndex)
{
    if (relayIndex < ConfigRelayCount)
    {
        _dirtyRelayMask |= (1 << relayIndex);
    }
}

void MQTTRelayHandler::markAllRelaysDirty()
{
    // Mark all relays as dirty (set all bits for ConfigRelayCount relays)
    _dirtyRelayMask = (1 << ConfigRelayCount) - 1;
}

void MQTTRelayHandler::onRelayStatusChanged(uint8_t relayBitmask)
{
    // Mark all relays as dirty when status changes
    // This ensures all relay states are published in the next update cycle
    (void)relayBitmask;
    markAllRelaysDirty();
}

void MQTTRelayHandler::publishDiscoveryConfig()
{
    // Trigger non-blocking discovery
    _discoveryPending = true;
    _discoveryIndex = 0;
}

void MQTTRelayHandler::publishRelayDiscoveryConfig(uint8_t relayIndex)
{
    if (_mqttController == nullptr || _config == nullptr)
    {
        return;
    }

    if (!_mqttController->isConnected())
    {
        return;
    }

    // Validate relay index
    if (relayIndex >= ConfigRelayCount)
    {
        return;
    }

    MQTTClient* client = _mqttController->getClient();
    if (client == nullptr)
    {
        return;
    }

    // Build discovery topic: <discoveryPrefix>/switch/<device_id>/<object_id>/config
    // Use a single-segment object_id (relay_<index>) so Home Assistant can parse it
    char topic[MqttMaxTopicLength];
    snprintf(topic, sizeof(topic), "%s/switch/%s/relay_%u/config",
        _config->mqtt.discoveryPrefix, _config->mqtt.deviceId, relayIndex);
    Serial.print("Discovery Topic: ");
    Serial.println(topic);

    // Build JSON payload
    // Note: Keep this simple to avoid buffer overflow - max ~512 bytes
    char payload[MqttMaxPayloadLength];

    // Get relay name from config (use long name for display)
    const char* relayName = _config->relayLongNames[relayIndex];

    // Build minimal JSON discovery payload
    snprintf(payload, sizeof(payload),
        "{\"name\":\"%s\","
        "\"state_topic\":\"home/%s/relay/%u/state\","
        "\"command_topic\":\"home/%s/relay/%u/set\","
        "\"payload_on\":\"ON\","
        "\"payload_off\":\"OFF\","
        "\"state_on\":\"ON\","
        "\"state_off\":\"OFF\","
        "\"unique_id\":\"%s_relay_%u\","
        "\"device\":{\"ids\":[\"%s\"],\"name\":\"Smart Fuse Box\",\"mf\":\"Simon Carter\",\"mdl\":\"SFB v1\"}}",
        relayName,
        _config->mqtt.deviceId, relayIndex,
        _config->mqtt.deviceId, relayIndex,
        _config->mqtt.deviceId, relayIndex,
        _config->mqtt.deviceId
    );
    Serial.print("Payload: ");
    Serial.println(payload);
    // Publish with retain flag so HA can discover even if it restarts
    client->publish(topic, payload, MqttQoS::AtMostOnce, true);
}

#endif