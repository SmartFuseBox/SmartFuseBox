#include "Local.h"

#if defined(MQTT_SUPPORT)

#include "MQTTController.h"
#include "Config.h"
#include <stdio.h>


// Static instance pointer for callbacks
MQTTController* MQTTController::_instance = nullptr;

MQTTController::MQTTController(MessageBus* messageBus, Config* config)
    : _mqttClient(nullptr)
    , _messageBus(messageBus)
    , _config(config)
    , _retryCount(0)
    , _lastRetryTime(0)
    , _isEnabled(false)
    , _connectedSince(0)
    , _reconnectCount(0)
{
    _instance = this;
}

MQTTController::~MQTTController()
{
    end();
    _instance = nullptr;
}

bool MQTTController::begin()
{
    if (_config == nullptr || _messageBus == nullptr)
    {
        Serial.println(F("[MQTT Controller] Error: Config or MessageBus not available"));
        return false;
    }

    // Check if MQTT is enabled in config
    if (!_config->mqtt.enabled)
    {
        _isEnabled = false;
        return false;
    }

    // Create MQTT client
    if (_mqttClient == nullptr)
    {
        _mqttClient = new MQTTClient();
    }

    // Configure client from config
    _mqttClient->setBroker(_config->mqtt.broker, _config->mqtt.port);

    if (_config->mqtt.username[0] != '\0' && _config->mqtt.password[0] != '\0')
    {
        _mqttClient->setCredentials(_config->mqtt.username, _config->mqtt.password);
    }

    _mqttClient->setClientId(_config->mqtt.deviceId);
    _mqttClient->setKeepAlive(_config->mqtt.keepAliveInterval);

    // Set callbacks
    _mqttClient->setConnectionCallback(connectionCallbackStatic);
    _mqttClient->setMessageCallback(messageCallbackStatic);
    _mqttClient->setEventCallback(eventCallbackStatic);

    _isEnabled = true;

    // Attempt initial connection (but don't fail initialization if connection fails)
    connect();

    // Return true if MQTT is configured and enabled
    // Actual connection will be handled by retry logic in update()
    return true;
}

void MQTTController::update()
{
    if (!_isEnabled || _mqttClient == nullptr)
    {
        return;
    }

    // Update MQTT client (non-blocking)
    _mqttClient->update();

    // Handle reconnection logic - only retry if we're fully disconnected, not if we're already connecting
    MqttConnectionState state = _mqttClient->getState();
    if (state == MqttConnectionState::Disconnected && shouldRetry())
    {
        _lastRetryTime = millis();
        connect();
    }
}

void MQTTController::end()
{
    if (_mqttClient != nullptr)
    {
        _mqttClient->disconnect();
        delete _mqttClient;
        _mqttClient = nullptr;
    }
    
    _isEnabled = false;
}

bool MQTTController::connect()
{
    if (_mqttClient == nullptr || !_isEnabled)
    {
        return false;
    }

    if (_mqttClient->isConnected())
    {
        return true;
    }

    bool result = _mqttClient->connect();

    return result;
}

void MQTTController::disconnect()
{
    if (_mqttClient != nullptr)
    {
        _mqttClient->disconnect();
    }
    
    _retryCount = 0;
}

bool MQTTController::isConnected() const
{
    if (_mqttClient == nullptr)
    {
        return false;
    }
    
    return _mqttClient->isConnected();
}

bool MQTTController::isEnabled() const
{
    return _isEnabled;
}

void MQTTController::setEnabled(bool enabled)
{
    _isEnabled = enabled;
    
    if (!enabled && _mqttClient != nullptr)
    {
        disconnect();
    }
}

bool MQTTController::publishState(const char* topic, const char* value)
{
    if (!isConnected() || topic == nullptr || value == nullptr)
    {
        return false;
    }

    // Use retain=true for state topics so subscribers get last known state
    return _mqttClient->publish(topic, value, MqttQoS::AtMostOnce, true);
}

uint32_t MQTTController::getReconnectCount() const
{
    return _reconnectCount;
}

unsigned long MQTTController::getUptime() const
{
    if (!isConnected() || _connectedSince == 0)
    {
        return 0;
    }
    
    return (millis() - _connectedSince) / 1000; // Convert to seconds
}

MQTTClient* MQTTController::getClient()
{
    return _mqttClient;
}

// ============================================================================
// Private Methods
// ============================================================================

void MQTTController::onMqttConnected(bool connected)
{
    if (connected)
    {
        Serial.println(F("[MQTT] Connected"));
        _retryCount = 0;
        _connectedSince = millis();
        _reconnectCount++;

        // Publish to MessageBus
        if (_messageBus != nullptr)
        {
            _messageBus->publish<MqttConnected>();
        }
    }
    else
    {
        Serial.println(F("[MQTT] Disconnected"));
        _connectedSince = 0;

        // Increment retry count on disconnection (includes timeouts and connection failures)
        _retryCount++;

        // Publish to MessageBus
        if (_messageBus != nullptr)
        {
            _messageBus->publish<MqttDisconnected>();
        }
    }
}

void MQTTController::onMqttMessage(const char* topic, const char* payload, uint16_t length)
{
    (void)length;
    // Publish to MessageBus for routing
    if (_messageBus != nullptr)
    {
        _messageBus->publish<MqttMessageReceived>(topic, payload);
    }
}

void MQTTController::onMqttEvent(MqttEvent event, uint8_t errorCode)
{
    // Handle MQTT events and log them as appropriate
    switch (event)
    {
        case MqttEvent::Connected:
            Serial.println(F("[MQTT] Connected"));
            break;

        case MqttEvent::Disconnected:
            Serial.println(F("[MQTT] Disconnected"));
            break;

        case MqttEvent::ConnectionRefused:
            Serial.print(F("[MQTT] Error: Connection refused, code: "));
            Serial.println(errorCode);
            break;

        case MqttEvent::ConnectionTimeout:
            Serial.println(F("[MQTT] Error: Connection timeout"));
            break;

        case MqttEvent::BrokerNotConfigured:
            Serial.println(F("[MQTT] Error: No broker configured"));
            break;

        case MqttEvent::BufferOverflow:
            Serial.println(F("[MQTT] Error: Buffer overflow"));
            break;

        case MqttEvent::InvalidPacket:
            Serial.println(F("[MQTT] Error: Invalid packet"));
            break;

        case MqttEvent::NetworkError:
            Serial.println(F("[MQTT] Error: Network error"));
            break;

        case MqttEvent::KeepAliveTimeout:
            Serial.println(F("[MQTT] Error: Keep-alive timeout"));
            break;

        default:
            // Ignore other events (PacketSent, PacketReceived, etc.)
            break;
    }
}

bool MQTTController::shouldRetry()
{
    if (_retryCount >= MqttMaxRetries)
    {
        if (_retryCount == MqttMaxRetries)
        {
            Serial.println(F("[MQTT] Error: Max retry attempts reached"));
            _retryCount++; // Increment to prevent this message from repeating
        }
        return false;
    }

    unsigned long now = millis();
    uint16_t delay = getRetryDelay();
    unsigned long elapsed = now - _lastRetryTime;

    return elapsed >= delay;
}

uint16_t MQTTController::getRetryDelay()
{
    if (_retryCount >= MqttMaxRetries)
    {
        return MqttRetryDelays[MqttMaxRetries - 1];
    }
    
    return MqttRetryDelays[_retryCount];
}

// ============================================================================
// Static Callbacks
// ============================================================================

void MQTTController::connectionCallbackStatic(bool connected)
{
    if (_instance != nullptr)
    {
        _instance->onMqttConnected(connected);
    }
}

void MQTTController::messageCallbackStatic(const char* topic, const char* payload, uint16_t length)
{
    if (_instance != nullptr)
    {
        _instance->onMqttMessage(topic, payload, length);
    }
}

void MQTTController::eventCallbackStatic(MqttEvent event, uint8_t errorCode)
{
    if (_instance != nullptr)
    {
        _instance->onMqttEvent(event, errorCode);
    }
}

#endif