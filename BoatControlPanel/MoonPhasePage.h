#pragma once


#include <SerialCommandManager.h>
#include <NextionControl.h>
#include <stdint.h>

#include "BaseBoatPage.h"
#include "NextionIds.h"



class MoonPhasePage : public BaseBoatPage
{
private:
    void updateMoonDisplay();
    unsigned long _pageEnterTime;
    unsigned long _lastRefresh;
protected:
    // Required overrides
    uint8_t getPageId() const override { return PageMoon; }
    void begin() override;
    void onEnterPage() override;
    void refresh(unsigned long now) override;

    //optional overrides
    void handleTouch(uint8_t compId, uint8_t eventType) override;

public:
    explicit MoonPhasePage(Stream* serialPort,
        WarningManager* warningMgr,
        SerialCommandManager* commandMgrLink = nullptr,
        SerialCommandManager* commandMgrComputer = nullptr);
};