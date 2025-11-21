#include "SystemCommandHandler.h"

#ifdef __arm__
// should use uinstd.h to define sbrk but Due causes a conflict
extern "C" char* sbrk(int incr);
#else  // __ARM__
extern char* __brkval;
#endif  // __arm__

SystemCommandHandler::SystemCommandHandler(BroadcastManager* broadcaster)
    : _broadcaster(broadcaster)
{
}

SystemCommandHandler::~SystemCommandHandler()
{
}

const String* SystemCommandHandler::supportedCommands(size_t& count) const
{
    static const String cmds[] = { SystemHeartbeatCommand, SystemInitialized, SystemFreeMemory };
    count = sizeof(cmds) / sizeof(cmds[0]);
    return cmds;
}

bool SystemCommandHandler::handleCommand(SerialCommandManager* sender, const String command, const StringKeyValue params[], int paramCount)
{
    (void)params;
    (void)paramCount;

    String cmd = command;
    cmd.trim();

    if (cmd == SystemHeartbeatCommand)
    {
        sendAckOk(sender, cmd);
    }
    else if (cmd == SystemInitialized)
    {
        sendAckOk(sender, cmd);
    }
    else if (cmd == SystemFreeMemory)
    {
        StringKeyValue param = { ValueParamName, String(freeRam()) };
        sendAckOk(sender, cmd, &param);
    }
    else
    {
        sendAckErr(sender, cmd, F("Unknown system command"));
    }

    return true;
}

uint16_t SystemCommandHandler::freeRam() {
#if defined(ARDUINO_MEGA2560)
    extern int __heap_start, * __brkval;
    int v;
    return (int)&v - (__brkval == 0 ? (int)&__heap_start : (int)__brkval);
#elif defined(ARDUINO_UNO_R4)
    char top;
#ifdef __arm__
    return &top - reinterpret_cast<char*>(sbrk(0));
#elif defined(CORE_TEENSY) || (ARDUINO > 103 && ARDUINO != 151)
    return &top - __brkval;
#else  // __arm__
    return __brkval ? &top - __brkval : &top - __malloc_heap_start;
#endif  // __arm__
#endif
}
