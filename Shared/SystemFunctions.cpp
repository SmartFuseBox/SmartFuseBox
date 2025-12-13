#include "SystemFunctions.h"
#include "SystemDefinitions.h"
#include "Local.h"

#if defined(ARDUINO_UNO_R4)
#include <WiFiS3.h>
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

uint8_t SystemFunctions::GenerateDefaultPassword(char* buffer, size_t bufferSize)
{
    if (bufferSize < 15)
        return BufferInvalid;

#if defined(ARDUINO_UNO_R4)
    uint8_t mac[6];
    WiFi.macAddress(mac);

    snprintf(buffer, bufferSize, "sfb-%02X%02X%02X", mac[3], mac[4], mac[5]);
#else
    // Use analog noise as seed
    randomSeed(analogRead(A0) + analogRead(A1) + millis());
    uint32_t storedID = random(0x10000000, 0xFFFFFFFF);

    // Format: SFB12A1B2C3
    snprintf(buffer, bufferSize, "sfb-%08X", storedID);

#endif

    buffer[bufferSize - 1] = '\0';

    return BufferSuccess;
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

bool SystemFunctions::parseBooleanValue(const char* value)
{
    return (strcmp(value, "1") == 0 ||
        strcmp(value, "on") == 0 ||
        strcmp(value, "true") == 0);
}

bool SystemFunctions::isAllDigits(const char* s)
{
    if (!s || s[0] == '\0')
        return false;

    for (size_t i = 0; s[i] != '\0'; ++i) 
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

bool SystemFunctions::isProgmem(const char* ptr)
{
#if defined(ARDUINO_MEGA2560)
    // On AVR (Arduino Mega 2560), PROGMEM starts after RAM
    // RAMEND is defined by Arduino (typically 0x21FF for Mega 2560)
    return (uintptr_t)ptr >= RAMEND;
#elif defined(ARDUINO_UNO_R4)
    // On Arduino UNO R4 (Renesas RA4M1), PROGMEM is not used the same way
    // Flash memory starts at 0x00000000, SRAM starts at 0x20000000
    return (uintptr_t)ptr < 0x20000000;
#else
#error "You must define 'ARDUINO_MEGA2560' or 'ARDUINO_UNO_R4'"
#endif
}

size_t SystemFunctions::copyString(char* dest, const char* src, size_t maxLen)
{
    if (!dest || !src || maxLen == 0)
        return 0;

    size_t copied = 0;

    if (isProgmem(src))
    {
        // Copy from PROGMEM
        while (copied < maxLen - 1)
        {
            char c = pgm_read_byte(src++);
            if (c == '\0')
                break;
            dest[copied++] = c;
        }
        dest[copied] = '\0';
    }
    else
    {
        // Copy from RAM
        while (copied < maxLen - 1 && src[copied] != '\0')
        {
            dest[copied] = src[copied];
            copied++;
        }
        dest[copied] = '\0';
    }

    return copied;
}

// Implementation
size_t SystemFunctions::appendString(char* dest, size_t destSize, size_t offset, const char* src) {
    if (!dest || !src || offset >= destSize - 1) return 0;
    
    size_t available = destSize - offset - 1; // Reserve space for null terminator
    size_t written = 0;
    
    if (isProgmem(src)) {
        while (available > 0) {
            char c = pgm_read_byte(src++);
            if (c == '\0') break;
            dest[offset + written++] = c;
            available--;
        }
    } else {
        while (available > 0 && *src != '\0') {
            dest[offset + written++] = *src++;
            available--;
        }
    }
    
    dest[offset + written] = '\0';
    return written;
}

size_t SystemFunctions::calculateLength(const char* str) {
    if (!str) return 0;
    
    if (isProgmem(str)) {
        return strlen_P(str);
    } else {
        return strlen(str);
    }
}