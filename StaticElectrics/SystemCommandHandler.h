#pragma once


#include <Arduino.h>
#include "Config.h"
#include "ConfigManager.h"
#include "BaseCommandHandler.h"
#include "StaticElectricConstants.h"

class SystemCommandHandler : public BaseCommandHandler
{
private:
	SerialCommandManager* _commandMgrComputer;
public:
	explicit SystemCommandHandler(SerialCommandManager* commandMgrComputer);

	bool handleCommand(SerialCommandManager* sender, const String command, const StringKeyValue params[], int paramCount) override;
	const String* supportedCommands(size_t& count) const override;
};