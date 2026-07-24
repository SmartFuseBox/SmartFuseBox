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
// 
// 
// 

#include "Local.h"
#include "WarningNetworkHandler.h"
#include "SystemFunctions.h"


WarningNetworkHandler::WarningNetworkHandler(WarningManager* warningManager)
	: _warningManager(warningManager)
{
}

CommandResult WarningNetworkHandler::handleRequest(const char* method,
	const char* command,
	StringKeyValue* params,
	uint8_t paramCount,
	char* responseBuffer,
	size_t bufferSize)
{
	(void)method;
	(void)params;

	if (_warningManager == nullptr)
	{
		formatJsonResponse(responseBuffer, bufferSize, false, "warning controller not initialized");
		return CommandResult::error(WarningControllerNotInitialised);
	}

	// none of the warning commands should receive any parameters
	if (paramCount > 0)
	{
		formatJsonResponse(responseBuffer, bufferSize, false, "Invalid parameters");
		return CommandResult::error(InvalidCommandParameters);
	}

	if (SystemFunctions::commandMatches(command, WarningsList))
	{
		formatStatusJson(responseBuffer, bufferSize);
		return CommandResult::ok();
	}

	if (SystemFunctions::commandMatches(command, WarningsListText))
	{
		formatWarningsJsonList(responseBuffer, bufferSize);
		return CommandResult::ok();
	}

	return CommandResult::error(InvalidCommandParameters);
}

void WarningNetworkHandler::formatStatusJson(char* buffer, size_t size)
{
	snprintf(buffer, size, "\"warning\":{\"active\": \"0x%X\"}",
		static_cast<unsigned int>(_warningManager->getActiveWarningsMask()));
}

void WarningNetworkHandler::formatWifiStatusJson(IWifiClient* client)
{
	char buffer[MaximumJsonResponseBufferSize];
	buffer[0] = '\0';

	formatStatusJson(buffer, sizeof(buffer));
	client->print(buffer);
}

void WarningNetworkHandler::formatWarningsJsonList(char* buffer, size_t size)
{
	uint32_t activeWarnings = _warningManager->getActiveWarningsMask();

	size_t pos = 0;
	int written = snprintf(buffer + pos, (pos < size ? size - pos : 0), "\"success\":true,\"warnings\":[");
	if (written < 0) return;
	pos += (size_t)written;
	bool first = true;

	for (uint8_t bit = 0; bit < 32; bit++)
	{
		if (activeWarnings & (1UL << bit))
		{
			const char* warningStr = getWarningString(bit);

			if (warningStr != nullptr && warningStr[0] != '\0')
			{
				if (!first)
				{
					if (pos < size - 1) buffer[pos++] = ',';
				}

				first = false;

				// append quoted string (warningStr may contain spaces)
				written = snprintf(buffer + pos, (pos < size ? size - pos : 0), "\"%s\"", warningStr);

				if (written < 0)
					break;

				pos += (size_t)written;

				if (pos >= size)
					break;
			}
		}
	}

	if (pos < size - 2)
	{
		buffer[pos++] = ']';
		buffer[pos] = '\0';
	}
}
