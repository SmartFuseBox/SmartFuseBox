#pragma once
#include <Arduino.h>
#include "BaseCommandHandler.h"
#include "StaticElectricConstants.h"

class AckCommandHandler : public BaseCommandHandler
{
public:
    // Constructor: pass the NextionControl pointer so we can notify the current page
    explicit AckCommandHandler(SerialCommandManager* commandMgrComputer);

    bool handleCommand(SerialCommandManager* sender, const String command, const StringKeyValue params[], int paramCount) override;
    const String* supportedCommands(size_t& count) const override;

private:
    SerialCommandManager* _commandMgrComputer;
};