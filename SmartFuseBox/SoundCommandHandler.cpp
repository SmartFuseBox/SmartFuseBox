// 
// 
// 

#include "SoundCommandHandler.h"



SoundCommandHandler::SoundCommandHandler(SerialCommandManager* commandMgrComputer, SerialCommandManager* commandMgrLink, 
    SoundController* soundController)
	: _commandMgrComputer(commandMgrComputer), _commandMgrLink(commandMgrLink), _soundController(soundController)
{

}

const char* const* SoundCommandHandler::supportedCommands(size_t& count) const
{
    static const char* cmds[] = { SoundSignalCancel, SoundSignalActive, SoundSignalSoS,
        SoundSignalFog, SoundSignalMoveStarboard, SoundSignalMovePort, SoundSignalMoveAstern, 
        SoundSignalMoveDanger, SoundSignalOvertakeStarboard, SoundSignalOvertakePort, 
        SoundSignalOvertakeConsent, SoundSignalOvertakeDanger, SoundSignalTest };
    count = sizeof(cmds) / sizeof(cmds[0]);
    return cmds;
}

bool SoundCommandHandler::handleCommand(SerialCommandManager* sender, const char* command, const StringKeyValue params[], uint8_t paramCount)
{
	(void)params;

    if (_soundController == nullptr)
    {
        sendAckErr(sender, command, F("Sound manager not initialized"));
		return true;
    }

    // none of the sound commands should receive any parameters
    if (paramCount > 0)
    {
        sendAckErr(sender, command, F("Invalid Parameters"));
        return true;
    }

    if (strcmp(command, SoundSignalCancel) == 0)
    {
        _soundController->playSound(SoundType::None);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalActive) == 0)
    {
        StringKeyValue param = makeParam(static_cast<uint8_t>(_soundController->getCurrentSoundType()), static_cast<uint8_t>(_soundController->getCurrentSoundState()));
        sendAckOk(sender, command, &param);
    }
    else if (strcmp(command, SoundSignalSoS) == 0)
    {
        _soundController->playSound(SoundType::Sos);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalFog) == 0)
    {
        _soundController->playSound(SoundType::Fog);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalMoveAstern) == 0)
    {
        _soundController->playSound(SoundType::MoveAstern);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalMovePort) == 0)
    {
        _soundController->playSound(SoundType::MovePort);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalMoveStarboard) == 0)
    {
        _soundController->playSound(SoundType::MoveStarboard);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalMoveDanger) == 0)
    {
        _soundController->playSound(SoundType::MoveDanger);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalOvertakeConsent) == 0)
    {
        _soundController->playSound(SoundType::OvertakeConsent);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalOvertakeDanger) == 0)
    {
        _soundController->playSound(SoundType::OvertakeDanger);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalOvertakePort) == 0)
    {
        _soundController->playSound(SoundType::OvertakePort);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalOvertakeStarboard) == 0)
    {
        _soundController->playSound(SoundType::OvertakeStarboard);
        sendAckOk(sender, command);
    }
    else if (strcmp(command, SoundSignalTest) == 0)
    {
        _soundController->playSound(SoundType::Test);
        sendAckOk(sender, command);
    }
    else
    {
        sendAckErr(sender, command, F("Unknown system command"));
    }

    broadcast(command);

    return true;
}

void SoundCommandHandler::broadcast(const char* cmd, const StringKeyValue* param)
{
    if (_commandMgrLink != nullptr)
    {
        sendAckOk(_commandMgrLink, cmd, param);
    }

    if (_commandMgrComputer != nullptr)
    {
        sendAckOk(_commandMgrComputer, cmd, param);
    }
}
