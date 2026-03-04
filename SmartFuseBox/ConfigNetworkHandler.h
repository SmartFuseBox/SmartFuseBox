#pragma once

#include "INetworkCommandHandler.h"
#include "ConfigController.h"
#include "WifiController.h"

class ConfigNetworkHandler : public INetworkCommandHandler
{
private:
	ConfigController* _configController;
	WifiController* _wifiController;
public:
	explicit ConfigNetworkHandler(ConfigController* configController, WifiController* wifiController);

	const char* getRoute() const override { return "/api/config"; }

	void formatWifiStatusJson(IWifiClient* client) override;

	void formatStatusJson(IWifiClient* client);

	CommandResult handleRequest(const char* method,
		const char* cmd,
		StringKeyValue* params,
		uint8_t paramCount,
		char* responseBuffer,
		size_t bufferSize) override;
};