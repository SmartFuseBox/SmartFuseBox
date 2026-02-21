#include "Local.h"

#if defined(MQTT_SUPPORT)

#include "MQTTHandler.h"
#include "ConfigManager.h"
#include <string.h>
#include <stdio.h>

MQTTHandler::MQTTHandler(MQTTController* mqttController, MessageBus* messageBus)
    : _mqttController(mqttController)
    , _messageBus(messageBus)
    , _isSubscribed(false)
{
}

MQTTHandler::~MQTTHandler()
{
    // Don't call pure virtual end() from destructor
    // Derived classes must call end() in their own destructors
}

bool MQTTHandler::extractIndexFromTopic(const char* topic, const char* prefix, uint8_t* index)
{
    if (topic == nullptr || prefix == nullptr || index == nullptr)
    {
        return false;
    }
    
    // Find prefix in topic
    const char* pos = strstr(topic, prefix);
    if (pos == nullptr)
    {
        return false;
    }
    
    // Move past prefix
    pos += strlen(prefix);
    
    // Skip any leading slashes
    while (*pos == '/')
    {
        pos++;
    }
    
    // Parse number
    char* endPtr;
    long value = strtol(pos, &endPtr, 10);
    
    if (endPtr == pos || value < 0 || value > 255)
    {
        return false;
    }
    
    *index = (uint8_t)value;
    return true;
}

#endif