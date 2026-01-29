#include "ConfigCommandHandler.h"
#include "ConfigSyncManager.h"

#if defined(ARDUINO_UNO_R4)
#include "BluetoothController.h"
#include <Arduino.h>
#endif


ConfigCommandHandler::ConfigCommandHandler(WifiController* wifiController, ConfigController* configController)
	: _wifiController(wifiController),
	  _configController(configController),
	  _configSyncManager(nullptr)
{
}

void ConfigCommandHandler::setConfigSyncManager(ConfigSyncManager* syncManager)
{
	_configSyncManager = syncManager;
}

bool ConfigCommandHandler::handleCommand(SerialCommandManager* sender, const char* command, const StringKeyValue params[], uint8_t paramCount)
{
	// Notify sync manager that a config command was received (if syncing)
	if (_configSyncManager)
	{
		_configSyncManager->notifyConfigReceived();
	}

	// Access the in-memory config
	ConfigResult result;

	if (strcmp(command, ConfigSaveSettings) == 0)
    {
		result = _configController->save();
    }
    else if (strcmp(command, ConfigGetSettings) == 0)
    {
        char buffer[128]{};
        buffer[0] = '\0';

		Config* config = _configController->getConfigPtr();

        if (!config)
        {
            sendAckErr(sender, command, F("Config not available"));
            return true;
		}

        // return summary of config back to caller in multiple commands
        // C3:<name>
        sender->sendCommand(ConfigRename, config->name);

        // C4 entries - send both short and long names in format: <idx>=<shortName|longName>
        for (uint8_t i = 0; i < ConfigRelayCount; ++i)
        {
            snprintf(buffer, sizeof(buffer), "%u=%s|%s", i, config->relayShortNames[i], config->relayLongNames[i]);
            sender->sendCommand(ConfigRenameRelay, buffer);
        }

        // C5 entries
        for (uint8_t s = 0; s < ConfigHomeButtons; ++s)
        {
            snprintf(buffer, sizeof(buffer), "%u=%u", s, config->homePageMapping[s]);
            sender->sendCommand(ConfigMapHomeButton, buffer);
        }

        // C6 Send home page button color mappings
        for (uint8_t i = 0; i < ConfigRelayCount; i++)
        {
            snprintf(buffer, sizeof(buffer), "%u=%u", i, config->buttonImage[i]);
            sender->sendCommand(ConfigSetButtonColor, buffer);
        }

        // C7 Boat type
        snprintf(buffer, sizeof(buffer), "v=%d", static_cast<uint8_t>(config->vesselType));
        sender->sendCommand(ConfigBoatType, buffer);

        // C8 Sound relay ID
        snprintf(buffer, sizeof(buffer), "v=%d", static_cast<uint8_t>(config->hornRelayIndex));
        sender->sendCommand(ConfigSoundRelayId, buffer);

        // C9 Sound start delay
        snprintf(buffer, sizeof(buffer), "v=%d", static_cast<uint8_t>(config->soundStartDelayMs));
        sender->sendCommand(ConfigSoundStartDelay, buffer);

#if defined(ARDUINO_UNO_R4)
		// C10 Bluetooth enable
        snprintf(buffer, sizeof(buffer), "v=%s", (config->bluetoothEnabled ? "1" : "0"));
        sender->sendCommand(ConfigBluetoothEnable, buffer);

		// C11 WiFi enable
        snprintf(buffer, sizeof(buffer), "v=%s", (config->wifiEnabled ? "1" : "0"));
        sender->sendCommand(ConfigWifiEnable, buffer);

		// C12 WiFi mode
        snprintf(buffer, sizeof(buffer), "v=%d", config->accessMode);
        sender->sendCommand(ConfigWifiMode, buffer);

		// C13 WiFi SSID
        snprintf(buffer, sizeof(buffer), "v=%s", config->apSSID);
        sender->sendCommand(ConfigWifiSSID, buffer);

		// C14 WiFi Password
        snprintf(buffer, sizeof(buffer), "v=%s", config->apPassword);
        sender->sendCommand(ConfigWifiPassword, buffer);

		// C15 WiFi Port
        snprintf(buffer, sizeof(buffer), "v=%d", config->wifiPort);
        sender->sendCommand(ConfigWifiPort, buffer);

		// C16 WiFi State
        if (_wifiController && _wifiController->getServer())
        {
            snprintf(buffer, sizeof(buffer), "v=%d", static_cast<int>(_wifiController->getServer()->getConnectionState()));
            sender->sendCommand(ConfigWifiState, buffer);
        }

		// C17 WiFi AP IP Address
        if (_wifiController && _wifiController->getServer())
        {
			char ipBuffer[MaxIpAddressLength];
            _wifiController->getServer()->getIpAddress(ipBuffer, sizeof(ipBuffer));
            snprintf(buffer, sizeof(buffer), "v=%s", ipBuffer);
            sender->sendCommand(ConfigWifiApIpAddress, buffer);
		}
#endif

		// C18 Default relay states
        for (uint8_t i = 0; i < ConfigRelayCount; ++i)
        {
            snprintf(buffer, sizeof(buffer), "%d=%s", i, (config->defaulRelayState[i] ? "1" : "0"));
            sender->sendCommand(ConfigDefaultRelayState, buffer);
		}

		// C19 Linked relays
        for (uint8_t i = 0; i < ConfigMaxLinkedRelays; ++i)
        {
            snprintf(buffer, sizeof(buffer), "%d=%d", config->linkedRelays[i][0], config->linkedRelays[i][1]);
            sender->sendCommand(ConfigLinkRelays, buffer);
		}

        result = ConfigResult::Success;
    }
    else if (strcmp(command, ConfigResetSettings) == 0)
    {
        // Reset to defaults
        result = _configController->reset();
    }
    else if (strcmp(command, ConfigRename) == 0)
    {
        if (paramCount >= 1)
        {
            result = _configController->rename(params[0].value);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigRenameRelay) == 0)
    {
        // Expect "C4:<idx>=<shortName>" or "C4:<idx>=<shortName|longName>" where idx 0..7
        if (paramCount >= 1)
        {
            uint8_t idx = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
			result = _configController->renameRelay(idx, params[0].value);
        }
        else
        {
			result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigMapHomeButton) == 0)
    {
        // Expect "MAP <button>=<relay>" where button 0..3, relay 0..7 (or 255 to unmap)
        if (paramCount >= 1)
        {
            uint8_t button = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
            uint8_t relay = static_cast<uint8_t>(strtoul(params[0].value, nullptr, 0));

            result = _configController->mapHomeButton(button, relay);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigSetButtonColor) == 0)
    {
        // Expect "MAP <button>=<color>" where button 0..3, image 0..5 (or 255 to unmap)
        if (paramCount >= 1)
        {
            uint8_t button = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
            uint8_t buttonColor = static_cast<uint8_t>(strtoul(params[0].value, nullptr, 0));

            result = _configController->mapHomeButtonColor(button, buttonColor);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
        }
    else if (strcmp(command, ConfigBoatType) == 0)
    {
        // Expect "C7:type=<value>" where value is 0..3
        if (paramCount >= 1)
        {
            uint8_t type = atoi(params[0].value);
            result = _configController->setVesselType(type);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigSoundRelayId) == 0)
    {
        // Expect "MAP <value>=<relay>" where relay 0..7 (or 255 to unmap)
        if (paramCount == 1 && strlen(params[0].value) > 0)
        {
            uint8_t relay = atoi(params[0].value);
            result = _configController->setSoundRelayButton(relay);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigSoundStartDelay) == 0)
    {
        if (paramCount == 1)
        {
            uint16_t soundStartDelay = atoi(params[0].value);
			result = _configController->setsoundDelayStart(soundStartDelay);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
#if defined(ARDUINO_UNO_R4)
	else if (strcmp(command, ConfigBluetoothEnable) == 0)
    {
        // Expect "C10:v=<0|1>"
        if (paramCount >= 1)
        {
            bool enable = SystemFunctions::parseBooleanValue(params[0].value);
			result = _configController->setBluetoothEnabled(enable);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigWifiEnable) == 0)
    {
        // Expect "C11:v=<0|1>"
        if (paramCount >= 1)
        {
            bool enable = SystemFunctions::parseBooleanValue(params[0].value);
			result = _configController->setWifiEnabled(enable);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigWifiMode) == 0)
    {
        // Expect "C12:v=<0|1>"
        if (paramCount >= 1)
        {
            uint8_t mode = static_cast<uint8_t>(atoi(params[0].value));
			result = _configController->setWifiAccessMode(mode);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
	}
	else if (strcmp(command, ConfigWifiSSID) == 0)
    {
        // Expect "C13:v=<value>"
        if (paramCount >= 1)
        {
			result = _configController->setWifiSsid(params[0].value);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigWifiPassword) == 0)
    {
        // Expect "C14:v=<value>"
        if (paramCount >= 1)
        {
			result = _configController->setWifiPassword(params[0].value);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigWifiPort) == 0)
    {
        // Expect "C15:v=<value>"
        if (paramCount >= 1)
        {
            uint16_t port = static_cast<uint16_t>(atoi(params[0].value));
			result = _configController->setWifiPort(port);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
	}
    else if (strcmp(command, ConfigWifiState) == 0)
    {
        // C16 WiFi State
        uint8_t state = static_cast<uint8_t>(WifiConnectionState::Disconnected);

        if (_wifiController)
        {
            state = static_cast<uint8_t>(_wifiController->getServer()->getConnectionState());
        }

        char cmd[10];
		snprintf(cmd, sizeof(cmd), "v=%d", state);
        sender->sendCommand(ConfigWifiState, cmd);
       result = ConfigResult::Success;
	}
    else if (strcmp(command, ConfigWifiApIpAddress) == 0)
    {
        // Expect "C17:v=<value>"
        if (paramCount >= 1)
        {
			result = _configController->setWifiIpAddress(params[0].value);
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
	}
#endif
    else if (strcmp(command, ConfigDefaultRelayState) == 0)
    {
        if (paramCount == 1)
        {
			uint8_t relayIndex = static_cast<uint8_t>(atoi(params[0].key));
			result = _configController->setRelayDefaultState(relayIndex, SystemFunctions::parseBooleanValue(params[0].value));
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else if (strcmp(command, ConfigLinkRelays) == 0)
    {
        // Expect "C19:<relay1>=<relay2>" to link relay1 to relay2
        // or "C19:<relay1>=0xFF" to unlink relay1
        if (paramCount >= 1)
        {
            uint8_t relay1 = static_cast<uint8_t>(strtoul(params[0].key, nullptr, 0));
            uint8_t relay2 = static_cast<uint8_t>(strtoul(params[0].value, nullptr, 0));

            if (relay2 < MaxUint8Value)
            {

                result = _configController->linkRelays(relay1, relay2);
            }
            else
            {
                result = _configController->unlinkRelay(relay1);
            }
        }
        else
        {
            result = ConfigResult::InvalidParameter;
        }
    }
    else
    {
        result = ConfigResult::InvalidCommand;
    }

    switch (result)
    {
    case ConfigResult::Success:
        break;
    case ConfigResult::InvalidRelay:
        sendAckErr(sender, command, F("Index out of range"), &params[0]);
        return true;
    case ConfigResult::TooLong:
        sendAckErr(sender, command, F("Name too long"), &params[0]);
        return true;
    case ConfigResult::MissingName:
        sendAckErr(sender, command, F("Missing name"), &params[0]);
        return true;
    case ConfigResult::InvalidConfig:
        sendAckErr(sender, command, F("Config not available"), &params[0]);
        return true;
	case ConfigResult::InvalidCommand:
        sendAckErr(sender, command, F("Invalid command"), &params[0]);
		return true;
    case ConfigResult::InvalidParameter:
        sendAckErr(sender, command, F("Invalid parameter"), &params[0]);
		return true;
    case ConfigResult::BluetoothInitFailed:
        sendAckErr(sender, command, F("Bluetooth init failed"), &params[0]);
		return true;
    case ConfigResult::WifiInitFailed:
	    sendAckErr(sender, command, F("WiFi init failed"), &params[0]);
	    return true;
    default:
        sendAckErr(sender, command, F("Unknown error"), &params[0]);
        return true;
    }

    sendAckOk(sender, command, &params[0]);

    return true;
}

const char* const* ConfigCommandHandler::supportedCommands(size_t& count) const
{
    static const char* cmds[] = { ConfigSaveSettings, ConfigGetSettings, 
        ConfigResetSettings, ConfigBoatType, ConfigSoundRelayId, ConfigSoundStartDelay,
        ConfigBluetoothEnable, ConfigWifiEnable, ConfigWifiMode, ConfigWifiSSID, 
        ConfigWifiPassword, ConfigWifiPort, ConfigWifiState, ConfigWifiApIpAddress,
        ConfigDefaultRelayState, ConfigLinkRelays };
    count = sizeof(cmds) / sizeof(cmds[0]);
    return cmds;
}
