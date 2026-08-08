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
#include "RelayNetworkHandler.h"
#include "SystemFunctions.h"

RelayNetworkHandler::RelayNetworkHandler(RelayController* relayController)
	: _relayController(relayController)
{
}

CommandResult RelayNetworkHandler::handleRequest(const char* method,
	const char* command, StringKeyValue* params, uint8_t paramCount,
	char* responseBuffer, size_t bufferSize)
{
	(void)method;

	if (!_relayController)
	{
		formatJsonResponse(responseBuffer, bufferSize, false, "Controller not initialized");
		return CommandResult::error(RelayControllerNotInitialised);
	}

	if (SystemFunctions::commandMatches(command, RelayTurnAllOff))
	{
		_relayController->turnAllRelaysOff();
		formatStatusJson(responseBuffer, bufferSize);
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, RelayTurnAllOn))
	{
		_relayController->turnAllRelaysOn();
		formatStatusJson(responseBuffer, bufferSize);
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, RelayRetrieveStates))
	{
		formatStatusJson(responseBuffer, bufferSize);
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, RelaySetState))
	{
		if (paramCount == 1)
		{
			uint8_t relayIndex = atoi(params[0].key);
			uint8_t state = atoi(params[0].value);

			if (relayIndex >= _relayController->getRelayCount())
			{
				return CommandResult::error(InvalidCommandParameters);
			}

			CommandResult result = _relayController->setRelayState(relayIndex, state > 0);


			if (!result.success)
			{
				return CommandResult::error(InvalidCommandParameters);
			}

			formatStatusJson(responseBuffer, bufferSize);
			return CommandResult::ok();
		}
		else
		{
			return CommandResult::error(InvalidCommandParameters);
		}
	}
	else if (SystemFunctions::commandMatches(command, RelayStatusGet))
	{
		if (paramCount == 1)
		{
			uint8_t relayIndex = atoi(params[0].key);

			if (relayIndex >= _relayController->getRelayCount())
			{
				return CommandResult::error(InvalidCommandParameters);
			}

			formatStatusJson(responseBuffer, bufferSize);
			return CommandResult::ok();
		}
		else
		{
			return CommandResult::error(InvalidCommandParameters);
		}
	}
	else if (SystemFunctions::commandMatches(command, RelayGetAllConfig))
	{
		formatStatusJson(responseBuffer, bufferSize);
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, RelayRename))
	{
		if (paramCount >= 1)
		{
			uint8_t idx = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));

			if (idx >= _relayController->getRelayCount())
			{
				formatJsonResponse(responseBuffer, bufferSize, false, "Invalid relay index");
				return CommandResult::error(InvalidCommandParameters);
			}

			int pipeIdx = SystemFunctions::indexOf(params[0].value, '|', 0);
			char shortName[ConfigShortRelayNameLength] = "";
			char longName[ConfigLongRelayNameLength] = "";

			if (pipeIdx >= 0)
			{
				SystemFunctions::substr(shortName, sizeof(shortName), params[0].value, 0, pipeIdx);
				SystemFunctions::substr(longName, sizeof(longName), params[0].value, pipeIdx + 1);
			}
			else
			{
				strncpy(shortName, params[0].value, sizeof(shortName) - 1);
			}

			_relayController->renameRelay(idx, shortName, longName);
			formatJsonResponse(responseBuffer, bufferSize, true);
			return CommandResult::ok();
		}
		formatJsonResponse(responseBuffer, bufferSize, false, "Invalid parameters");
		return CommandResult::error(InvalidCommandParameters);
	}
	else if (SystemFunctions::commandMatches(command, RelaySetButtonColor))
	{
		if (paramCount >= 1)
		{
			uint8_t relayIndex = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			uint8_t color = static_cast<uint8_t>(strtoul(params[0].value, nullptr, 0));
			if (relayIndex >= _relayController->getRelayCount())
			{
				formatJsonResponse(responseBuffer, bufferSize, false, "Invalid relay index");
				return CommandResult::error(InvalidCommandParameters);
			}
			_relayController->setButtonColor(relayIndex, color);
			formatJsonResponse(responseBuffer, bufferSize, true);
			return CommandResult::ok();
		}
		formatJsonResponse(responseBuffer, bufferSize, false, "Invalid parameters");
		return CommandResult::error(InvalidCommandParameters);
	}
	else if (SystemFunctions::commandMatches(command, RelaySetDefaultState))
	{
		if (paramCount >= 1)
		{
			uint8_t relayIndex = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			bool state = atoi(params[0].value) > 0;
			if (relayIndex >= _relayController->getRelayCount())
			{
				formatJsonResponse(responseBuffer, bufferSize, false, "Invalid relay index");
				return CommandResult::error(InvalidCommandParameters);
			}
			_relayController->setRelayDefaultState(relayIndex, state);
			formatJsonResponse(responseBuffer, bufferSize, true);
			return CommandResult::ok();
		}

		formatJsonResponse(responseBuffer, bufferSize, false, "Invalid parameters");
		return CommandResult::error(InvalidCommandParameters);
	}
	else if (SystemFunctions::commandMatches(command, RelayLink))
	{
		if (paramCount >= 1)
		{
			uint8_t relayIndex = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			uint8_t linkedIndex = static_cast<uint8_t>(strtoul(params[0].value, nullptr, 0));

			if (relayIndex >= _relayController->getRelayCount())
			{
				formatJsonResponse(responseBuffer, bufferSize, false, "Invalid relay index");
				return CommandResult::error(InvalidCommandParameters);
			}

			if (linkedIndex == DefaultValue)
			{
				_relayController->unlinkRelay(relayIndex);
			}
			else
			{
				RelayResult linkResult = _relayController->linkRelays(relayIndex, linkedIndex);
				if (linkResult == RelayResult::Failed)
				{
					formatJsonResponse(responseBuffer, bufferSize, false, "No available link slots");
					return CommandResult::error(InvalidCommandParameters);
				}
			}

			formatJsonResponse(responseBuffer, bufferSize, true);
			return CommandResult::ok();
		}

		formatJsonResponse(responseBuffer, bufferSize, false, "Invalid parameters");
		return CommandResult::error(InvalidCommandParameters);
	}
	else if (SystemFunctions::commandMatches(command, RelaySetActionType))
	{
		if (paramCount >= 1)
		{
			uint8_t relayIndex = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			uint8_t actionType = static_cast<uint8_t>(strtoul(params[0].value, nullptr, 0));

			if (relayIndex >= _relayController->getRelayCount())
			{
				formatJsonResponse(responseBuffer, bufferSize, false, "Invalid relay index");
				return CommandResult::error(InvalidCommandParameters);
			}

			_relayController->setRelayActionType(relayIndex, static_cast<RelayActionType>(actionType));
			formatJsonResponse(responseBuffer, bufferSize, true);
			return CommandResult::ok();
		}

		formatJsonResponse(responseBuffer, bufferSize, false, "Invalid parameters");
		return CommandResult::error(InvalidCommandParameters);
	}
	else if (SystemFunctions::commandMatches(command, RelaySetPin))
	{
		if (paramCount >= 1)
		{
			uint8_t relayIndex = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			uint8_t pin = static_cast<uint8_t>(strtoul(params[0].value, nullptr, 0));

			if (relayIndex >= _relayController->getRelayCount())
			{
				formatJsonResponse(responseBuffer, bufferSize, false, "Invalid relay index");
				return CommandResult::error(InvalidCommandParameters);
			}
			RelayResult pinResult = _relayController->setRelayPin(relayIndex, pin);
			if (pinResult == RelayResult::InvalidPin)
			{
				formatJsonResponse(responseBuffer, bufferSize, false, "Invalid pin");
				return CommandResult::error(InvalidCommandParameters);
			}

			formatJsonResponse(responseBuffer, bufferSize, true);
			return CommandResult::ok();
		}

		formatJsonResponse(responseBuffer, bufferSize, false, "Invalid parameters");
		return CommandResult::error(InvalidCommandParameters);
	}

	return CommandResult::error(InvalidCommandParameters);
}

void RelayNetworkHandler::formatStatusJson(char* buffer, size_t size)
{
	if (!buffer || size == 0)
		return;

	Config* config = ConfigManager::getConfigPtr();

	int written = snprintf(buffer, size, "\"relays\":[");

	if (written < 0 || written >= static_cast<int>(size))
	{
		buffer[size - 1] = '\0';
		return;
	}

	for (uint8_t i = 0; i < _relayController->getRelayCount(); i++)
	{
		CommandResult result = _relayController->getRelayStatus(i);
		int n;

		if (config != nullptr && i < ConfigRelayCount)
		{
			const RelayEntry& relay = config->relay.relays[i];
			n = snprintf(buffer + written, size - written,
				"%s{\"shortName\":\"%s\",\"longName\":\"%s\",\"pin\":%u,\"img\":%u,\"defaultState\":%u,\"actionType\":%u,\"state\":%u}",
				(i > 0) ? "," : "",
				relay.shortName,
				relay.longName,
				relay.pin,
				relay.buttonImage,
				relay.defaultState ? 1u : 0u,
				static_cast<uint8_t>(relay.actionType),
				result.status);
		}
		else
		{
			n = snprintf(buffer + written, size - written,
				"%s%u",
				(i > 0) ? "," : "",
				result.status);
		}

		if (n < 0 || written + n >= static_cast<int>(size))
			break;

		written += n;
	}

	int n = snprintf(buffer + written, size - written, "]");
	if (n < 0 || written + n >= static_cast<int>(size))
	{
		buffer[size - 1] = '\0';
		return;
	}
	written += n;

	if (config != nullptr)
	{
		n = snprintf(buffer + written, size - written,
			",\"hm\":[%u,%u,%u,%u]",
			config->relay.homePageMapping[0],
			config->relay.homePageMapping[1],
			config->relay.homePageMapping[2],
			config->relay.homePageMapping[3]);

		if (n < 0 || written + n >= static_cast<int>(size))
		{
			buffer[size - 1] = '\0';
			return;
		}
		written += n;

		n = snprintf(buffer + written, size - written, ",\"lk\":[");
		if (n < 0 || written + n >= static_cast<int>(size))
		{
			buffer[size - 1] = '\0';
			return;
		}
		written += n;

		for (uint8_t i = 0; i < ConfigMaxLinkedRelays; i++)
		{
			n = snprintf(buffer + written, size - written,
				"%s[%u,%u]",
				(i > 0) ? "," : "",
				config->relay.linkedRelays[i][0],
				config->relay.linkedRelays[i][1]);

			if (n < 0 || written + n >= static_cast<int>(size))
			{
				buffer[size - 1] = '\0';
				return;
			}
			written += n;
		}

		n = snprintf(buffer + written, size - written, "]");
		if (n < 0 || written + n >= static_cast<int>(size))
		{
			buffer[size - 1] = '\0';
		}
	}
}

void RelayNetworkHandler::formatWifiStatusJson(IWifiClient* client)
{
	if (!_relayController)
	{
		client->print("\"relays\":[]");
		return;
	}

	Config* config = ConfigManager::getConfigPtr();
	uint8_t relayCount = _relayController->getRelayCount();

	client->print("\"relays\":[");

	for (uint8_t i = 0; i < relayCount; i++)
	{
		CommandResult result = _relayController->getRelayStatus(i);

		if (i > 0)
			client->print(",");

		if (config != nullptr && i < ConfigRelayCount)
		{
			const RelayEntry& relay = config->relay.relays[i];
			client->print("{\"shortName\":\"");
			client->print(relay.shortName);
			client->print("\",\"longName\":\"");
			client->print(relay.longName);
			client->print("\",\"pin\":");
			client->print(relay.pin);
			client->print(",\"img\":");
			client->print(relay.buttonImage);
			client->print(",\"defaultState\":");
			client->print(relay.defaultState ? 1 : 0);
			client->print(",\"actionType\":");
			client->print(static_cast<uint8_t>(relay.actionType));
			client->print(",\"state\":");
			client->print(result.status);
			client->print("}");
		}
		else
		{
			client->print(result.status);
		}
	}

	client->print("]");

	if (config != nullptr)
	{
		client->print(",\"homeMap\":[");

		for (uint8_t i = 0; i < ConfigHomeButtons; i++)
		{
			if (i > 0)
				client->print(",");
			client->print(config->relay.homePageMapping[i]);
		}

		client->print("]");

		client->print(",\"linked\":[");

		for (uint8_t i = 0; i < ConfigMaxLinkedRelays; i++)
		{
			if (i > 0)
				client->print(",");

			client->print("[");
			client->print(config->relay.linkedRelays[i][0]);
			client->print(",");
			client->print(config->relay.linkedRelays[i][1]);
			client->print("]");
		}
		client->print("]");
	}
}
