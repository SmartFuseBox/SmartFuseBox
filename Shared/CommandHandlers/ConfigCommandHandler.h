#pragma once

#include <Arduino.h>
#include "BaseCommandHandler.h"
#include "ConfigController.h"
#include "WifiController.h"

// Forward declarations
class BluetoothController;
class ConfigSyncManager;

class ConfigCommandHandler : public BaseCommandHandler
{
private:
	WifiController* _wifiController;
	ConfigController* _configController;
	ConfigSyncManager* _configSyncManager;

public:
	explicit ConfigCommandHandler(WifiController* wifiController, ConfigController* configController);

	void setConfigSyncManager(ConfigSyncManager* syncManager);

	bool handleCommand(SerialCommandManager* sender, const char* command, const StringKeyValue params[], uint8_t paramCount) override;
	const char* const* supportedCommands(size_t& count) const override;
};