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
#include "SensorConfigNetworkHandler.h"

#include "ConfigManager.h"
#include "ConfigController.h"
#include "SystemDefinitions.h"
#include "SystemFunctions.h"

CommandResult SensorConfigNetworkHandler::handleRequest(const char* method,
	const char* command,
	StringKeyValue* params,
	uint8_t paramCount,
	char* responseBuffer,
	size_t bufferSize)
{
	(void)method;

	Config* config = ConfigManager::getConfigPtr();
	if (!config)
	{
		formatJsonResponse(responseBuffer, bufferSize, false, "Config not available");
		return CommandResult::error(InvalidConfiguration);
	}

	ConfigResult result = ConfigResult::InvalidCommand;

	// GET /api/sensorconfig/  or  GET /api/sensorconfig/S0  — return all sensors
	if (SystemFunctions::commandMatches(command, SensorConfigGetAll) || command[0] == '\0')
	{
		int written = snprintf(responseBuffer, bufferSize,
			"\"success\":true,\"count\":%u,\"sensors\":[", config->sensors.count);

		for (uint8_t i = 0; i < config->sensors.count && i < ConfigMaxSensors; i++)
		{
			const SensorEntry& e = config->sensors.sensors[i];
			int n = snprintf(responseBuffer + written, bufferSize - written,
				"%s{\"i\":%u,\"t\":%u,\"n\":\"%s\","
				"\"p0\":%u,\"p1\":%u,"
				"\"u0\":%d,\"u1\":%d,"
				"\"o0\":%d,\"o1\":%d,"
				"\"en\":%u}",
				i > 0 ? "," : "",
				i,
				static_cast<uint8_t>(e.sensorType),
				e.name,
				e.pins[0],
				e.pins[1],
				e.options1[0],
				e.options1[1],
				e.options2[0],
				e.options2[1],
				e.enabled ? 1u : 0u);
			if (n < 0 || written + n >= static_cast<int>(bufferSize))
				break;
			written += n;
		}

		snprintf(responseBuffer + written, bufferSize - written, "]");
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, SensorConfigAddUpdate))
	{
		// S1:i=<idx>;t=<type>;o0=<opt0>;o1=<opt1>
		uint8_t idx, type;
		int8_t opt0, opt1;

		if (!getParamValueU8t(params, paramCount, "i", idx) ||
			!getParamValueU8t(params, paramCount, "t", type) ||
			!getParamValue8t(params, paramCount, "o0", opt0) ||
			!getParamValue8t(params, paramCount, "o1", opt1) ||
			idx >= ConfigMaxSensors)
		{
			result = ConfigResult::InvalidParameter;
		}
		else
		{
			SensorEntry& entry = config->sensors.sensors[idx];
			entry.enabled = true;
			entry.sensorType = static_cast<SensorIdList>(type);
			entry.options1[0] = opt0;
			entry.options1[1] = opt1;

			if (idx >= config->sensors.count)
				config->sensors.count = idx + 1;

			result = ConfigResult::Success;
		}
	}
	else if (SystemFunctions::commandMatches(command, SensorConfigRemove))
	{
		// S2:v=<idx>
		uint8_t idx;
		if (paramCount < 1 || !getParamValueU8t(params, paramCount, "v", idx))
		{
			result = ConfigResult::InvalidParameter;
		}
		else if (idx >= ConfigMaxSensors)
		{
			result = ConfigResult::InvalidParameter;
		}
		else if (idx >= config->sensors.count)
		{
			// Already absent — treat as success
			result = ConfigResult::Success;
		}
		else
		{
			for (uint8_t j = idx; j + 1 < config->sensors.count; j++)
				config->sensors.sensors[j] = config->sensors.sensors[j + 1];

			if (config->sensors.count > 0)
				config->sensors.count--;

			memset(&config->sensors.sensors[config->sensors.count], 0, sizeof(SensorEntry));

			result = ConfigResult::Success;
		}
	}
	else if (SystemFunctions::commandMatches(command, SensorConfigRename))
	{
		// S3:<idx>=<name>
		if (paramCount < 1)
		{
			result = ConfigResult::InvalidParameter;
		}
		else
		{
			uint8_t idx = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			if (idx >= ConfigMaxSensors || idx >= config->sensors.count || params[0].value[0] == '\0')
			{
				result = ConfigResult::InvalidParameter;
			}
			else
			{
				SensorEntry& entry = config->sensors.sensors[idx];
				strncpy(entry.name, params[0].value, sizeof(entry.name) - 1);
				entry.name[sizeof(entry.name) - 1] = '\0';
				result = ConfigResult::Success;
			}
		}
	}
	else if (SystemFunctions::commandMatches(command, SensorConfigSetPin))
	{
		// S4:i=<idx>;s=<slot>;v=<pin>
		uint8_t idx, slot, pin;
		if (!getParamValueU8t(params, paramCount, "i", idx) ||
			!getParamValueU8t(params, paramCount, "s", slot) ||
			!getParamValueU8t(params, paramCount, "v", pin) ||
			idx >= ConfigMaxSensors || idx >= config->sensors.count ||
			slot >= ConfigMaxSensorPins)
		{
			result = ConfigResult::InvalidParameter;
		}
		else
		{
			config->sensors.sensors[idx].pins[slot] = pin;
			result = ConfigResult::Success;
		}
	}
	else if (SystemFunctions::commandMatches(command, SensorConfigSetEnabled))
	{
		// S5:<idx>=<0|1>
		if (paramCount < 1)
		{
			result = ConfigResult::InvalidParameter;
		}
		else
		{
			uint8_t idx = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			bool enabled = SystemFunctions::parseBooleanValue(params[0].value);

			if (idx >= ConfigMaxSensors || idx >= config->sensors.count)
			{
				result = ConfigResult::InvalidParameter;
			}
			else
			{
				config->sensors.sensors[idx].enabled = enabled;
				result = ConfigResult::Success;
			}
		}
	}
	else if (SystemFunctions::commandMatches(command, SensorConfigSetOptions))
	{
		// S6:i=<idx>;s=<slot>;o=<group>;v=<value>
		uint8_t idx, slot, option;
		if (!getParamValueU8t(params, paramCount, "i", idx) ||
			!getParamValueU8t(params, paramCount, "s", slot) ||
			!getParamValueU8t(params, paramCount, "o", option) ||
			idx >= ConfigMaxSensors || idx >= config->sensors.count)
		{
			result = ConfigResult::InvalidParameter;
		}
		else
		{
			if (option > 1)
			{
				result = ConfigResult::InvalidParameter;
			}
			else
			{
				constexpr uint8_t options1Size = sizeof(SensorEntry::options1) / sizeof(SensorEntry::options1[0]);
				if (slot >= options1Size)
				{
					result = ConfigResult::InvalidParameter;
				}
				else if (option == 0)
				{
					int8_t val8;
					if (!getParamValue8t(params, paramCount, "v", val8))
					{
						result = ConfigResult::InvalidParameter;
					}
					else
					{
						config->sensors.sensors[idx].options1[slot] = val8;
						result = ConfigResult::Success;
					}
				}
				else
				{
					int16_t val16;
					if (!getParamValue16t(params, paramCount, "v", val16))
					{
						result = ConfigResult::InvalidParameter;
					}
					else
					{
						config->sensors.sensors[idx].options2[slot] = val16;
						result = ConfigResult::Success;
					}
				}
			}
		}
	}

	if (result == ConfigResult::Success)
	{
		formatJsonResponse(responseBuffer, bufferSize, true);
		return CommandResult::ok();
	}

	return CommandResult::error(static_cast<uint8_t>(result));
}

void SensorConfigNetworkHandler::formatStatusJson(IWifiClient* client)
{
	Config* config = ConfigManager::getConfigPtr();
	if (!config)
		return;

	client->print("\"localSensors\":{");
	client->print("\"count\":");
	client->print(config->sensors.count);
	client->print(",\"sensors\":[");

	for (uint8_t i = 0; i < config->sensors.count && i < ConfigMaxSensors; i++)
	{
		const SensorEntry& e = config->sensors.sensors[i];

		if (i > 0)
			client->print(",");

		client->print("{\"i\":");
		client->print(i);
		client->print(",\"t\":");
		client->print(static_cast<uint8_t>(e.sensorType));
		client->print(",\"n\":\"");
		client->print(e.name);
		client->print("\",\"p0\":");
		client->print(e.pins[0]);
		client->print(",\"p1\":");
		client->print(e.pins[1]);
		client->print(",\"u0\":");
		client->print(e.options1[0]);
		client->print(",\"u1\":");
		client->print(e.options1[1]);
		client->print(",\"o0\":");
		client->print(e.options2[0]);
		client->print(",\"o1\":");
		client->print(e.options2[1]);
		client->print(",\"en\":");
		client->print(e.enabled ? "true" : "false");
		client->print("}");
	}

	client->print("]}");
}

void SensorConfigNetworkHandler::formatWifiStatusJson(IWifiClient* client)
{
	formatStatusJson(client);
}
