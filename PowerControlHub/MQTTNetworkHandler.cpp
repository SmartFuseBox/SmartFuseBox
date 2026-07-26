/*
 * PowerControlHub
 * Copyright (C) 2025 Simon Carter (s1cart3r@gmail.com)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
#include "Local.h"
#include "MQTTNetworkHandler.h"
#include "ConfigManager.h"
#include "SystemDefinitions.h"
#include "SystemFunctions.h"

#if defined(MQTT_SUPPORT)
#include "MQTTController.h"
#endif

MQTTNetworkHandler::MQTTNetworkHandler(MQTTController* mqttController)
	: _mqttController(mqttController)
{
}

static const char* getParamValue(const StringKeyValue params[], uint8_t paramCount, const char* key)
{
	for (uint8_t i = 0; i < paramCount; ++i)
	{
		if (strcmp(params[i].key, key) == 0)
			return params[i].value;
	}

	return nullptr;
}

static void writeSuccess(char* buffer, size_t size, const char* command, const char* value)
{
	if (value)
	{
		snprintf(buffer, size, "\"success\":true,\"command\":\"%s\",\"v\":\"%s\"", command, value);
	}
	else
	{
		snprintf(buffer, size, "\"success\":true,\"command\":\"%s\"", command);
	}
}

static void writeError(char* buffer, size_t size, const char* message)
{
	snprintf(buffer, size, "\"success\":false,\"error\":\"%s\"", message);
}

void MQTTNetworkHandler::writeStatusJson(IWifiClient* client)
{
	Config* config = ConfigManager::getConfigPtr();

	if (!config)
	{
		client->print("\"mqtt\":{\"error\":\"Config not available\"}");
		return;
	}

	client->print("\"mqtt\":{");

	client->print("\"enabled\":");
	client->print(config->mqtt.enabled ? "true" : "false");
	client->print(",");

	client->print("\"broker\":\"");
	client->print(config->mqtt.broker);
	client->print("\",");

	client->print("\"port\":");
	client->print(config->mqtt.port);
	client->print(",");

	client->print("\"username\":\"");
	client->print(config->mqtt.username);
	client->print("\",");

	client->print("\"password\":\"***\",");

	client->print("\"deviceId\":\"");
	client->print(config->mqtt.deviceId);
	client->print("\",");

	client->print("\"useHomeAssistantDiscovery\":");
	client->print(config->mqtt.useHomeAssistantDiscovery ? "true" : "false");
	client->print(",");

	client->print("\"keepAliveInterval\":");
	client->print(config->mqtt.keepAliveInterval);
	client->print(",");

	client->print("\"discoveryPrefix\":\"");
	client->print(config->mqtt.discoveryPrefix);
	client->print("\"}");
}

void MQTTNetworkHandler::formatWifiStatusJson(IWifiClient* client)
{
	writeStatusJson(client);
}

CommandResult MQTTNetworkHandler::handleRequest(const char* method,
	const char* command,
	StringKeyValue* params,
	uint8_t paramCount,
	char* responseBuffer,
	size_t bufferSize)
{
	(void)method;

	Config* config = ConfigManager::getConfigPtr();

	if (config == nullptr)
	{
		writeError(responseBuffer, bufferSize, "Config not available");
		return CommandResult::ok();
	}

	const char* v = getParamValue(params, paramCount, "v");

#if defined(MQTT_SUPPORT)
	if (command[0] == '\0')
	{
		snprintf(responseBuffer, bufferSize,
			"\"success\":true,"
			"\"enabled\":%s,"
			"\"broker\":\"%s\","
			"\"port\":%u,"
			"\"username\":\"%s\","
			"\"password\":\"***\","
			"\"deviceId\":\"%s\","
			"\"useHomeAssistantDiscovery\":%s,"
			"\"keepAliveInterval\":%u,"
			"\"discoveryPrefix\":\"%s\"",
			config->mqtt.enabled ? "true" : "false",
			config->mqtt.broker,
			config->mqtt.port,
			config->mqtt.username,
			config->mqtt.deviceId,
			config->mqtt.useHomeAssistantDiscovery ? "true" : "false",
			config->mqtt.keepAliveInterval,
			config->mqtt.discoveryPrefix);
		return CommandResult::ok();
	}

	if (SystemFunctions::commandMatches(command, MqttConfigEnable))
	{
		if (v)
		{
			config->mqtt.enabled = (v[0] == '1');
			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			char buf[2];
			snprintf(buf, sizeof(buf), "%d", config->mqtt.enabled ? 1 : 0);
			writeSuccess(responseBuffer, bufferSize, command, buf);
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigBroker))
	{
		if (v)
		{
			strncpy(config->mqtt.broker, v, ConfigMqttBrokerLength - 1);
			config->mqtt.broker[ConfigMqttBrokerLength - 1] = '\0';
			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			writeSuccess(responseBuffer, bufferSize, command, config->mqtt.broker);
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigPort))
	{
		if (v)
		{
			uint16_t port = static_cast<uint16_t>(strtoul(v, nullptr, 10));

			if (port > 0)
				config->mqtt.port = port;

			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			char buf[8];
			snprintf(buf, sizeof(buf), "%u", config->mqtt.port);
			writeSuccess(responseBuffer, bufferSize, command, buf);
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigUsername))
	{
		if (v)
		{
			strncpy(config->mqtt.username, v, ConfigMqttUsernameLength - 1);
			config->mqtt.username[ConfigMqttUsernameLength - 1] = '\0';
			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			writeSuccess(responseBuffer, bufferSize, command, config->mqtt.username);
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigPassword))
	{
		if (v)
		{
			strncpy(config->mqtt.password, v, ConfigMqttPasswordLength - 1);
			config->mqtt.password[ConfigMqttPasswordLength - 1] = '\0';
			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			writeSuccess(responseBuffer, bufferSize, command, "***");
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigDeviceId))
	{
		if (v)
		{
			strncpy(config->mqtt.deviceId, v, ConfigMqttDeviceIdLength - 1);
			config->mqtt.deviceId[ConfigMqttDeviceIdLength - 1] = '\0';
			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			writeSuccess(responseBuffer, bufferSize, command, config->mqtt.deviceId);
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigHADiscovery))
	{
		if (v)
		{
			config->mqtt.useHomeAssistantDiscovery = (v[0] == '1');
			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			char buf[2];
			snprintf(buf, sizeof(buf), "%d", config->mqtt.useHomeAssistantDiscovery ? 1 : 0);
			writeSuccess(responseBuffer, bufferSize, command, buf);
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigKeepAlive))
	{
		if (v)
		{
			uint16_t keepAlive = static_cast<uint16_t>(strtoul(v, nullptr, 10));

			if (keepAlive > 0)
				config->mqtt.keepAliveInterval = keepAlive;

			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			char buf[8];
			snprintf(buf, sizeof(buf), "%u", config->mqtt.keepAliveInterval);
			writeSuccess(responseBuffer, bufferSize, command, buf);
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigState))
	{
		bool isConnected = false;

		if (_mqttController != nullptr)
			isConnected = _mqttController->isConnected();

		char buf[2];
		snprintf(buf, sizeof(buf), "%d", isConnected ? 1 : 0);
		writeSuccess(responseBuffer, bufferSize, command, buf);
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, MqttConfigDiscoveryPrefix))
	{
		if (v)
		{
			strncpy(config->mqtt.discoveryPrefix, v, ConfigMqttDiscoveryPrefixLength - 1);
			config->mqtt.discoveryPrefix[ConfigMqttDiscoveryPrefixLength - 1] = '\0';
			writeSuccess(responseBuffer, bufferSize, command, nullptr);
		}
		else
		{
			writeSuccess(responseBuffer, bufferSize, command, config->mqtt.discoveryPrefix);
		}

		return CommandResult::ok();
	}
#endif

	writeError(responseBuffer, bufferSize, "Invalid command");
	return CommandResult::ok();
}
