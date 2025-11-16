// 
// 
// 

#include "SystemCommandHandler.h"

constexpr char SystemHeartbeat[] = "F0";
constexpr char SystemInitialized[] = "F1";
constexpr char FreeMemory[] = "F2";

SystemCommandHandler::SystemCommandHandler(SerialCommandManager* commandMgrComputer)
    : _commandMgrComputer(commandMgrComputer)
{
}

bool SystemCommandHandler::handleCommand(SerialCommandManager* sender, const String command, const StringKeyValue params[], int paramCount)
{
    // Access the in-memory config
    Config* config = ConfigManager::getConfigPtr();

    if (!config)
    {
        sendAckErr(sender, command, F("Config not available"));
        return true;
    }

    // Normalize command
    String cmd = command;
    cmd.trim();

    if (cmd == SystemHeartbeat)
    {
        sendAckOk(sender, cmd, &params[0]);
    }
    else if (cmd == SystemInitialized)
    {
        sendAckOk(sender, cmd, &params[0]);
    }
    else if (cmd == FreeMemory)
    {
        sendAckOk(sender, cmd, &params[0]);
    }
    else
    {
        sendAckErr(sender, cmd, F("Unknown config command"));
    }

    return true;
}

const String* SystemCommandHandler::supportedCommands(size_t& count) const
{
    static const String cmds[] = { SystemHeartbeat, SystemInitialized, FreeMemory };
    count = sizeof(cmds) / sizeof(cmds[0]);
    return cmds;
}
