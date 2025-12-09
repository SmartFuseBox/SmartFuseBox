#include "SystemFunctions.h"
#include "SystemDefinitions.h"
#include "Local.h"

#if defined(ARDUINO_UNO_R4)
extern "C" char* sbrk(int incr);
#endif

uint16_t SystemFunctions::stackAvailable()
{
    extern int __heap_start, * __brkval;
    unsigned int v;
    return (unsigned int)&v - (__brkval == 0 ? (unsigned int)&__heap_start : (unsigned int)__brkval);
}

uint16_t SystemFunctions::freeMemory()
{
#if defined(ARDUINO_MEGA2560)
    extern int __heap_start, * __brkval;
    int v;
    return (int)&v - (__brkval == 0 ? (int)&__heap_start : (int)__brkval);
#elif defined(ARDUINO_UNO_R4)
    char top;
    return &top - reinterpret_cast<char*>(sbrk(0));
#else
#error "You must define 'ARDUINO_MEGA2560' or 'ARDUINO_UNO_R4'"
#endif
}

void SystemFunctions::initializeSerial(HardwareSerial& serialPort, unsigned long baudRate, bool waitForConnection)
{
	serialPort.begin(baudRate);

	if (waitForConnection)
	{
		unsigned long leave = millis() + SerialInitTimeoutMs;

		while (!serialPort && millis() < leave)
			delay(10);

		if (serialPort)
			delay(100);
	}
}

static uint8_t SystemFunctions::GenerateDefaultPassword(char* buffer, size_t bufferSize)
{
    if (bufferSize < 17)
        return BufferInvalid;

#if defined(ARDUINO_UNO_R4)
    uint8_t mac[6];
    WiFi.macAddress(mac);

    char password[16];
    // Create password like: SFB-A1B2C3
    sprintf(buffer, "sfb-%02X%02X%02X", mac[3], mac[4], mac[5]);
#else
    uint32_t storedID = 0;

    // Use analog noise as seed
    randomSeed(analogRead(A0) + analogRead(A1) + millis());
    storedID = random(0x10000000, 0xFFFFFFFF);

    // Format: SFB12A1B2C3
    snprintf(buffer, bufferSize, "sfb%08X", storedID);

#endif

    return BufferSuccess;
}

bool SystemFunctions::parseBooleanValue(const String& value)
{
    return (value == F("1") ||
        value.equalsIgnoreCase(F("on")) ||
        value.equalsIgnoreCase(F("true")));
}

bool SystemFunctions::isAllDigits(const String& s)
{
    if (s.length() == 0)
        return false;

    for (size_t i = 0; i < s.length(); ++i)
    {
        if (!isDigit(s[i]))
            return false;
    }

    return true;
}

unsigned long SystemFunctions::elapsedMillis(unsigned long now, unsigned long previous)
{
    return now - previous;
}

bool SystemFunctions::hasElapsed(unsigned long now, unsigned long previous, unsigned long interval)
{
    return (now - previous) >= interval;
}