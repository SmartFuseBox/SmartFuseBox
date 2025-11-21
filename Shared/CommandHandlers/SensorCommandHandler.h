#pragma once

#include <Arduino.h>
#include "ConfigManager.h"
#include "SerialCommandManager.h"
#include "Local.h"

#ifdef FUSE_BOX_CONTROLLER
#include "SharedBaseCommandHandler.h"
#endif

#ifdef BOAT_CONTROL_PANEL
#include "HomePage.h"
#include "BaseBoatCommandHandler.h"
#include "BaseBoatPage.h"
#endif

// Define the base class type based on the build configuration
#if defined(BOAT_CONTROL_PANEL)
    #define SENSOR_BASE_CLASS BaseBoatCommandHandler
#elif defined(FUSE_BOX_CONTROLLER)
    #define SENSOR_BASE_CLASS SharedBaseCommandHandler
#else
    #error "Either BOAT_CONTROL_PANEL or FUSE_BOX_CONTROLLER must be defined"
#endif

class SensorCommandHandler : public SENSOR_BASE_CLASS
{
public:
#if defined(BOAT_CONTROL_PANEL)
    explicit SensorCommandHandler(BroadcastManager* broadcastManager, NextionControl* nextionControl, WarningManager* warningManager);
#elif defined(FUSE_BOX_CONTROLLER)
	explicit SensorCommandHandler(BroadcastManager* broadcastManager, WarningManager* warningManager);
#endif

    bool handleCommand(SerialCommandManager* sender, const String command, const StringKeyValue params[], int paramCount) override;
    const String* supportedCommands(size_t& count) const override;
};

#undef SENSOR_BASE_CLASS