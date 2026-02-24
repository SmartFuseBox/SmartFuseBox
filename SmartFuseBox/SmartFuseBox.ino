#include <Arduino.h>
#include <SerialCommandManager.h>

#include "Local.h"
#include "SmartFuseBoxApp.h"
#include "SystemFunctions.h"

#include "WaterSensorHandler.h"
#include "Dht11SensorHandler.h"
#include "LightSensorHandler.h"
#include "SystemSensorHandler.h"

#define COMPUTER_SERIAL Serial
#define LINK_SERIAL Serial1

// forward declares
// Consumer note:
// - `commandMgrComputer` and `commandMgrLink` are local to your .ino so you can
//   select the hardware Serial instances (Serial, Serial1, etc.) and baud rates
//   appropriate for your board. Keep the callbacks `onComputerCommandReceived` and
//   `onLinkCommandReceived` in this file so they can access these serial managers.
// - Construct `SmartFuseBoxApp` with pointers to these serial managers and your
//   relay pin mapping. Then construct any board-specific sensors using the
//   accessors from `app` (e.g. `app.messageBus()`, `app.broadcastManager()`,
//   `app.sensorCommandHandler()`). Finally call `app.setup(...)` with your
//   sensor arrays from `setup()` and call `app.loop()` from `loop()`.
void onComputerCommandReceived(SerialCommandManager* mgr);
void onLinkCommandReceived(SerialCommandManager* mgr);


SerialCommandManager commandMgrComputer(&COMPUTER_SERIAL, onComputerCommandReceived, '\n', ':', ';', '=', 500, 64);
SerialCommandManager commandMgrLink(&LINK_SERIAL, onLinkCommandReceived, '\n', ':', ';', '=', 500, 64);

SmartFuseBoxApp app(&commandMgrComputer, &commandMgrLink, Relays, ConfigRelayCount);

// Project-specific sensors
WaterSensorHandler waterSensorHandler(app.messageBus(), app.broadcastManager(), app.sensorCommandHandler(), WaterSensorPin, WaterSensorActivePin);
Dht11SensorHandler dht11SensorHandler(app.messageBus(), app.broadcastManager(), app.sensorCommandHandler(), app.warningManager(), Dht11SensorPin);
LightSensorHandler lightSensorHandler(app.messageBus(), app.broadcastManager(), app.sensorCommandHandler(), app.warningManager(), LightSensorPin, LightSensorAnalogPin);
SystemSensorHandler systemSensorHandler(app.messageBus(), app.wifiController(), app.bluetoothController(), app.warningManager());

// sensor manager
BaseSensorHandler* sensorHandlers[] = {
	&waterSensorHandler, &dht11SensorHandler, &lightSensorHandler, &systemSensorHandler
};
constexpr uint8_t sensorHandlerCount = sizeof(sensorHandlers) / sizeof(sensorHandlers[0]);

// middleware
BaseSensor* baseSensors[] = {
	&waterSensorHandler, &dht11SensorHandler, &lightSensorHandler, &systemSensorHandler
};
constexpr uint8_t baseSensorCount = sizeof(baseSensors) / sizeof(baseSensors[0]);

void setup()
{
	// Serial initialization is performed first to ensure that any logging or error messages
	// from DateTimeManager or ConfigManager during initialization are properly output.
	SystemFunctions::initializeSerial(COMPUTER_SERIAL, 115200, true);
	SystemFunctions::initializeSerial(LINK_SERIAL, 19200, true);

	// Wire late-binding sensor dependencies
	systemSensorHandler.setSdCardLogger(app.sdCardLogger());

	app.setup(sensorHandlers, sensorHandlerCount, baseSensors, baseSensorCount);
}

void loop()
{
	app.loop();
}

void onComputerCommandReceived(SerialCommandManager* mgr)
{
	commandMgrComputer.sendError(mgr->getRawMessage(), F("STATCMD"));
	SystemFunctions::resetSerial(COMPUTER_SERIAL);
}

void onLinkCommandReceived(SerialCommandManager* mgr)
{
	commandMgrComputer.sendError(mgr->getRawMessage(), F("STATLNK"));
	SystemFunctions::resetSerial(LINK_SERIAL);
}

