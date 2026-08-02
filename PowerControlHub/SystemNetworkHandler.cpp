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
#include "SystemNetworkHandler.h"
#include "SystemCpuMonitor.h"
#include "ConfigManager.h"
#include "DateTimeManager.h"
#include "SystemFunctions.h"
#include "FirmwareVersion.h"
#include "PinGuard.h"

#if defined(OTA_AUTO_UPDATE) && defined(ESP32) && defined(WIFI_SUPPORT)
#include "OtaManager.h"
#endif

#if defined(SD_CARD_SUPPORT)
#include "MicroSdDriver.h"
#endif

constexpr uint8_t UptimeBufferLength = 15;

SystemNetworkHandler::SystemNetworkHandler(WifiController* wifiController)
	: _wifiController(wifiController)
#if defined(SD_CARD_SUPPORT)
	, _sdCardLogger(nullptr)
#endif
{
}

CommandResult SystemNetworkHandler::handleRequest(const char* method,
	const char* command,
	StringKeyValue* params,
	uint8_t paramCount,
	char* responseBuffer,
	size_t bufferSize)
{
	(void)method;
	(void)params;
	(void)paramCount;

	if (SystemFunctions::commandMatches(command, SystemHeartbeatCommand))
	{
		formatStatusJson(responseBuffer, bufferSize);
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, SystemPinUsage))
	{
		uint8_t pins[64];
		uint8_t count = SystemFunctions::getUsedPins(pins, sizeof(pins));

		size_t pos = 0;
		int written = snprintf(responseBuffer + pos, bufferSize - pos,
			"\"success\":true,\"command\":\"%s\",\"pins\":[", command);

		if (written > 0)
			pos += (size_t)written;

		for (uint8_t i = 0; i < count && pos < bufferSize; ++i)
		{
			written = snprintf(responseBuffer + pos, bufferSize - pos,
				"%s%u", (i > 0) ? "," : "", (unsigned)pins[i]);

			if (written > 0)
				pos += (size_t)written;
			else
				break;
		}

		if (pos < bufferSize)
		{
			responseBuffer[pos++] = ']';
			responseBuffer[pos] = '\0';
		}

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, SystemPinRestrictions))
	{
		uint8_t tableSize = PinGuard::getPinTableSize();
		uint8_t hardPins[64];
		uint8_t advisoryPins[64];
		uint8_t hardCount = 0;
		uint8_t advisoryCount = 0;

		for (uint8_t i = 0; i < tableSize; ++i)
		{
			uint8_t pin;
			PinCategory category;
			PinGuard::getPinTableEntry(i, pin, category);

			if (category == PinCategory::Hard)
				hardPins[hardCount++] = pin;
			else if (category == PinCategory::Advisory)
				advisoryPins[advisoryCount++] = pin;
		}

		size_t pos = 0;

		int written = snprintf(responseBuffer + pos, bufferSize - pos,
			"\"success\":true,\"command\":\"%s\"", command);

		if (written > 0)
			pos += (size_t)written;

		// Hard pins array
		if (hardCount > 0 && pos < bufferSize)
		{
			written = snprintf(responseBuffer + pos, bufferSize - pos, ",\"hard\":[");

			if (written > 0)
				pos += (size_t)written;

			for (uint8_t i = 0; i < hardCount && pos < bufferSize; ++i)
			{
				written = snprintf(responseBuffer + pos, bufferSize - pos,
					"%s%u", (i > 0) ? "," : "", (unsigned)hardPins[i]);

				if (written > 0)
					pos += (size_t)written;
				else
					break;
			}

			if (pos < bufferSize)
				responseBuffer[pos++] = ']';
		}

		// Advisory pins array
		if (advisoryCount > 0 && pos < bufferSize)
		{
			written = snprintf(responseBuffer + pos, bufferSize - pos, ",\"advisory\":[");

			if (written > 0)
				pos += (size_t)written;

			for (uint8_t i = 0; i < advisoryCount && pos < bufferSize; ++i)
			{
				written = snprintf(responseBuffer + pos, bufferSize - pos,
					"%s%u", (i > 0) ? "," : "", (unsigned)advisoryPins[i]);

				if (written > 0)
					pos += (size_t)written;
				else
					break;
			}

			if (pos < bufferSize)
				responseBuffer[pos++] = ']';
		}

		if (pos < bufferSize)
			responseBuffer[pos] = '\0';

		return CommandResult::ok();
	}
	#if defined(OTA_AUTO_UPDATE) && defined(ESP32) && defined(WIFI_SUPPORT)
	else if (SystemFunctions::commandMatches(command, SystemCheckForUpdate))
	{
		if (!_otaManager)
		{
			snprintf(responseBuffer, bufferSize, "\"success\":false,\"error\":\"OTA not available\"");
			return CommandResult::ok();
		}

		const char* applyStr = nullptr;

		for (uint8_t i = 0; i < paramCount; ++i)
		{
			if (strcmp(params[i].key, "apply") == 0)
			{
				applyStr = params[i].value;
				break;
			}
		}

		bool applyNow = applyStr && (applyStr[0] == '1');
		_otaManager->triggerCheck(applyNow);

		const char* stateStr = "idle";

		switch (_otaManager->getState())
		{
			case OtaState::Idle:
				stateStr = "triggered";
				break;

			case OtaState::Checking:
				stateStr = "checking";
				break;

			case OtaState::UpdateAvailable:
				stateStr = "available";
				break;

			case OtaState::Downloading:
				stateStr = "downloading";
				break;

			case OtaState::Rebooting:
				stateStr = "rebooting";
				break;

			case OtaState::Failed:
				stateStr = "failed";
				break;

			case OtaState::UpToDate:
				stateStr = "uptodate";
				break;
		}

		snprintf(responseBuffer, bufferSize,
			"\"success\":true,\"command\":\"%s\",\"v\":\"v%u.%u.%u.%u\",\"av\":\"%s\",\"s\":\"%s\"",
			command,
			FirmwareMajor, FirmwareMinor, FirmwarePatch, FirmwareBuild,
			_otaManager->getAvailableVersion(),
			stateStr);

		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, SystemOtaStatus))
	{
		const char* stateStr = "disabled";
		const char* avVersion = "";
		char autoApply = '0';

		if (_otaManager)
		{
			switch (_otaManager->getState())
			{
				case OtaState::Idle:
					stateStr = "idle";
					break;

				case OtaState::Checking:
					stateStr = "checking";
					break;

				case OtaState::UpdateAvailable:
					stateStr = "available";
					break;

				case OtaState::Downloading:
					stateStr = "downloading";
					break;

				case OtaState::Rebooting:
					stateStr = "rebooting";
					break;

				case OtaState::Failed:
					stateStr = "failed";
					break;

				case OtaState::UpToDate:
					stateStr = "uptodate";
					break;
			}

			avVersion = _otaManager->getAvailableVersion();

			SystemHeader* hdr = ConfigManager::getHeaderPtr();
			if (hdr && (hdr->reserved[0] & OtaFlagAutoApply))
				autoApply = '1';
		}

		snprintf(responseBuffer, bufferSize,
			"\"success\":true,\"command\":\"%s\",\"v\":\"v%u.%u.%u.%u\",\"av\":\"%s\",\"s\":\"%s\",\"auto\":\"%c\"",
			command,
			FirmwareMajor, FirmwareMinor, FirmwarePatch, FirmwareBuild,
			avVersion,
			stateStr,
			autoApply);

		return CommandResult::ok();
	}
#endif // OTA_AUTO_UPDATE
	else if (SystemFunctions::commandMatches(command, SystemGetDateTime))
	{
		char dateTimeStr[DateTimeBufferLength];
		if (DateTimeManager::formatDateTime(dateTimeStr, sizeof(dateTimeStr)))
		{
			snprintf(responseBuffer, bufferSize,
				"\"success\":true,\"command\":\"%s\",\"v\":\"%s\"",
				command, dateTimeStr);
		}
		else
		{
			snprintf(responseBuffer, bufferSize,
				"\"success\":false,\"error\":\"Date/time not set\"");
		}
		return CommandResult::ok();
	}
	else if (SystemFunctions::commandMatches(command, SystemSetDateTime))
	{
		const char* tsStr = nullptr;
		for (uint8_t i = 0; i < paramCount; ++i)
		{
			if (strcmp(params[i].key, ValueParamName) == 0)
			{
				tsStr = params[i].value;
				break;
			}
		}

		if (tsStr)
		{
			uint64_t timestamp = static_cast<uint64_t>(strtoull(tsStr, nullptr, 0));
			if (timestamp > 0)
			{
				DateTimeManager::setDateTime(timestamp);
				char dateTimeStr[DateTimeBufferLength];
				DateTimeManager::formatDateTime(dateTimeStr, sizeof(dateTimeStr));
				snprintf(responseBuffer, bufferSize,
					"\"success\":true,\"command\":\"%s\",\"v\":\"%s\"",
					command, dateTimeStr);
			}
			else
			{
				snprintf(responseBuffer, bufferSize,
					"\"success\":false,\"error\":\"Invalid timestamp\"");
			}
		}
		else
		{
			snprintf(responseBuffer, bufferSize,
				"\"success\":false,\"error\":\"Missing v parameter\"");
		}
		return CommandResult::ok();
	}
	else
	{
		return CommandResult::error(InvalidCommandParameters);
	}
}

void SystemNetworkHandler::formatStatusJson(char* buffer, size_t size)
{
	Config* config = ConfigManager::getConfigPtr();

	bool bluetoothEnabled = false;
	bool wifiEnabled = false;
	int rssi = 0;

	if (config)
	{
		bluetoothEnabled = config->network.bluetoothEnabled;
		wifiEnabled = config->network.wifiEnabled;
	}

	if (_wifiController && wifiEnabled && _wifiController->isEnabled())
	{
		rssi = _wifiController->getServer()->getSignalStrength();
	}

	char dateTimeStr[DateTimeBufferLength];
	DateTimeManager::formatDateTime(dateTimeStr, sizeof(dateTimeStr));

	bool sdPresent = false;
	uint32_t logSize = 0;

#if defined(SD_CARD_SUPPORT)
	if (_sdCardLogger)
	{
		MicroSdDriver& sdDriver = MicroSdDriver::getInstance();
		sdPresent = sdDriver.isCardPresent();
		logSize = _sdCardLogger->getCurrentLogFileSize();
	}
#endif

	char uptime[UptimeBufferLength];
	TimeParts timeParts = SystemFunctions::msToTimeParts(SystemFunctions::millis64());
	SystemFunctions::formatTimeParts(uptime, UptimeBufferLength, timeParts);

	char fw[16];
	snprintf(fw, sizeof(fw), "v%u.%u.%u.%u",
		FirmwareMajor, FirmwareMinor, FirmwarePatch, FirmwareBuild);

	snprintf(buffer, size,
		"\"system\":{\"mem\":%d,\"cpu\":%d,\"bluetooth\":%d,\"wifi\":%d,\"rssi\":%d,\"time\":\"%s\","
		"\"sd\":{\"present\":%d,\"log\":%lu},\"Uptime\":\"%s\",\"fw\":\"%s\"}",
		SystemFunctions::freeMemory(),
		SystemCpuMonitor::getCpuUsage(),
		bluetoothEnabled,
		wifiEnabled,
		rssi,
		dateTimeStr,
		sdPresent,
		(unsigned long)logSize,
		uptime,
		fw);
}

void SystemNetworkHandler::formatWifiStatusJson(IWifiClient* client)
{
	char buffer[MaximumJsonResponseBufferSize];
	buffer[0] = '\0';

	formatStatusJson(buffer, sizeof(buffer));
	client->print(buffer);
}
