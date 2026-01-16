// 
// 
// 

#include "MoonPhasePage.h"


#include "Astronomy.h"
#include "DateTimeManager.h"


// Nextion Names/Ids on current Page
constexpr uint8_t ButtonPrevious = 2;
constexpr char ControlMoonPhase[] = "p2";
constexpr char ControlMoonPhaseName[] = "t1";
constexpr char ControlMoonPhaseSeaDescription[] = "t2";
constexpr char ControlMoonPhaseDescription[] = "t3";

constexpr unsigned long RefreshIntervalMs = 10000;
constexpr unsigned long AutoReturnMs = 120000;

MoonPhasePage::MoonPhasePage(Stream* serialPort,
    WarningManager* warningMgr,
    SerialCommandManager* commandMgrLink,
    SerialCommandManager* commandMgrComputer)
    : BaseBoatPage(serialPort, warningMgr, commandMgrLink, commandMgrComputer),
	_pageEnterTime(0), _lastRefresh(0)
{
}

void MoonPhasePage::onEnterPage()
{
    // Called when page becomes active
    _pageEnterTime = millis();
    _lastRefresh = millis();
    updateMoonDisplay();
}

void MoonPhasePage::begin()
{
    updateMoonDisplay();
}

void MoonPhasePage::refresh(unsigned long now)
{
    if (now - _lastRefresh >= RefreshIntervalMs)
    {
        _lastRefresh = now;
        updateMoonDisplay();
    }

    // Auto-return to home after configured timeout
    if (_pageEnterTime != 0 && (now - _pageEnterTime) >= AutoReturnMs)
    {
        setPage(PageHome);
    }
}

void MoonPhasePage::updateMoonDisplay()
{
    MoonPhase phase = Astronomy::getMoonPhaseFromUnix(DateTimeManager::getCurrentTime());
    setPicture(ControlMoonPhase, MoonImages[static_cast<uint8_t>(phase)]);

	char buffer[BufferSizeMoonPhaseDescription];
    Astronomy::getMoonPhaseName(phase, buffer, sizeof(buffer));
    sendText(ControlMoonPhaseName, buffer);
    Astronomy::getMoonPhaseSeaDescription(phase, buffer, sizeof(buffer));
    sendText(ControlMoonPhaseSeaDescription, buffer);
    Astronomy::getMoonPhaseDescription(phase, buffer, sizeof(buffer));
	sendText(ControlMoonPhaseDescription, buffer);
}

// Handle touch events for buttons
void MoonPhasePage::handleTouch(uint8_t compId, uint8_t eventType)
{
    (void)eventType;

    switch (compId)
    {
    case ButtonPrevious:
        setPage(PageHome);
        return;
    }
}
