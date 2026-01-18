#pragma once

#include <SerialCommandManager.h>
#include <NextionControl.h>
#include <stdint.h>

#include "BaseBoatPage.h"
#include "NextionIds.h"



class VhfRadioPage : public BaseBoatPage
{
protected:
    // Required overrides
    uint8_t getPageId() const override { return PageVhfRadio; }
    void begin() override;
    void onEnterPage() override;
    void refresh(unsigned long now) override;

    //optional overrides
    void handleTouch(uint8_t compId, uint8_t eventType) override;

public:
    explicit VhfRadioPage(Stream* serialPort,
        WarningManager* warningMgr,
        SerialCommandManager* commandMgrLink = nullptr,
        SerialCommandManager* commandMgrComputer = nullptr);
};