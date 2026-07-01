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
#pragma once

#include "INetworkCommandHandler.h"
#include "BaseConfigCommandHandler.h"

/**
 * @brief Network handler for local sensor configuration (S-series) via /api/sensorconfig.
 *
 * Exposes the same S0–S6 operations as SensorConfigCommandHandler but over
 * the WiFi REST interface. The GET route returns a JSON array of all configured
 * local sensors; mutation commands (S1–S6) modify the in-memory config directly.
 */
class SensorConfigNetworkHandler : public INetworkCommandHandler, public BaseConfigCommandHandler
{
public:
	SensorConfigNetworkHandler() = default;

	const char* getRoute() const override { return "/api/sensorconfig"; }

	void formatWifiStatusJson(IWifiClient* client) override;

	void formatStatusJson(IWifiClient* client);

	CommandResult handleRequest(const char* method,
		const char* command,
		StringKeyValue* params,
		uint8_t paramCount,
		char* responseBuffer,
		size_t bufferSize) override;
};
