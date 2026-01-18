#pragma once

#include <SerialCommandManager.h>
#include <NextionControl.h>
#include <stdint.h>

#include "BaseBoatPage.h"
#include "NextionIds.h"



class VhfDistressPage : public BaseBoatPage
{
private:
    double _lastLongitude = NAN;
    double _lastLatitude = NAN;
    void formatGpsPosition(double latitude, double longitude, char* outBuf, size_t outBufSize);
    void updateDisplay();
protected:
    // Required overrides
    uint8_t getPageId() const override { return PageVhfDistress; }
    void begin() override;
    void onEnterPage() override;
    void refresh(unsigned long now) override;

    //optional overrides
    void handleTouch(uint8_t compId, uint8_t eventType) override;
    void handleExternalUpdate(uint8_t updateType, const void* data) override;

public:
    explicit VhfDistressPage(Stream* serialPort,
        WarningManager* warningMgr,
        SerialCommandManager* commandMgrLink = nullptr,
        SerialCommandManager* commandMgrComputer = nullptr);
};