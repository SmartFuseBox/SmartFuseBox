#pragma once

#include "INetworkCommandHandler.h"
#include "SharedConstants.h"
#include "WifiController.h"


class SystemNetworkHandler : public INetworkCommandHandler
{
private:
	WifiController* _wifiController;
	void formatStatusJson(char* buffer, size_t size);

public:
	explicit SystemNetworkHandler(WifiController* wifiController);
	const char* getRoute() const override { return "/api/system"; }
	CommandResult handleRequest(const String& method,
		const String& cmd,
		StringKeyValue* params,
		uint8_t paramCount,
		char* responseBuffer,
		size_t bufferSize) override;
};