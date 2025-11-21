#include "BaseBoatCommandHandler.h"

BaseBoatCommandHandler::BaseBoatCommandHandler(
    BroadcastManager* broadcaster,
    NextionControl* nextionControl,
    WarningManager* warningManager
)
	: SharedBaseCommandHandler(broadcaster, warningManager)
    , _nextionControl(nextionControl)
{
}

void BaseBoatCommandHandler::notifyCurrentPage(uint8_t updateType, const void* data)
{
    if (!_nextionControl)
        return;

    BaseDisplayPage* p = _nextionControl->getCurrentPage();
    
    if (!p)
        return;

    p->handleExternalUpdate(updateType, data);
}
