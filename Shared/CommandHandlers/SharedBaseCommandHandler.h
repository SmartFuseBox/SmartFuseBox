#pragma once

#include <Arduino.h>
#include "BaseCommandHandler.h"
#include "BroadcastManager.h"
#include "WarningManager.h"

/**
 * @brief Base class for command handlers that need broadcasting capabilities.
 * 
 * This class extends BaseCommandHandler with BroadcastManager dependency and helper methods
 * for sending commands, debug messages, and errors. It provides a minimal shared foundation
 * that can be used across different contexts (boat-specific, shared, etc.).
 * 
 * Handlers that only need broadcasting capabilities should inherit from this class.
 * For boat-specific handlers that need additional dependencies (NextionControl, WarningManager),
 * use BaseBoatCommandHandler instead.
 */
class SharedBaseCommandHandler : public BaseCommandHandler
{
private:
    WarningManager* _warningManager;

protected:
	BroadcastManager* _broadcaster;

    /**
     * @brief Constructor with broadcast dependency.
     * 
     * @param broadcaster Manager for broadcasting commands/debug/errors
     */
    SharedBaseCommandHandler(BroadcastManager* broadcaster, WarningManager* warningManager)
		: _warningManager(warningManager), _broadcaster(broadcaster)
    {
    };

    /**
     * @brief Get pointer to the WarningManager.
     * 
     * @return WarningManager* Pointer to the warning manager (can be nullptr)
	 */
    WarningManager* getWarningManager() const
    {
        return _warningManager;
	}

    /**
     * @brief Send a debug message to the computer serial (via BroadcastManager).
     * 
     * @param message Debug message content
     * @param identifier Command handler identifier
     */
    void sendDebugMessage(const String& message, const String& identifier)
    {
        if (_broadcaster)
        {
            _broadcaster->sendDebug(message, identifier);
        }
    }

    /**
     * @brief Send a debug message to the computer serial (via BroadcastManager).
     * 
     * @param message Debug message content (const char* for efficiency)
     * @param identifier Command handler identifier
     */
    void sendDebugMessage(const char* message, const char* identifier)
    {
        if (_broadcaster)
        {
            _broadcaster->sendDebug(message, identifier);
        }
    }

    /**
     * @brief Send an error message to the computer serial (via BroadcastManager).
     * 
     * @param message Error message content
     * @param identifier Command handler identifier
     */
    void sendErrorMessage(const String& message, const String& identifier)
    {
        if (_broadcaster)
        {
            _broadcaster->sendError(message, identifier);
        }
    }

    void sendErrorMessage(const char* message, const char* identifier)
    {
        if (_broadcaster)
        {
            _broadcaster->sendError(message, identifier);
        }
    }
    /**
     * @brief Parse a string value as a boolean.
     *
     * Accepts multiple formats:
     * - "1" or "0"
     * - "on" or "off" (case-insensitive)
     * - "true" or "false" (case-insensitive)
     *
     * @param value String to parse
     * @return true if the value represents a truthy value, false otherwise
     */
    bool parseBooleanValue(const String& value) const
    {
        return (value == F("1") ||
            value.equalsIgnoreCase(F("on")) ||
            value.equalsIgnoreCase(F("true")));
    }

    /**
     * @brief Check if a string contains only digits.
     *
     * @param s String to check
     * @return true if the string is non-empty and contains only digits (0-9)
     */
    bool isAllDigits(const String& s) const
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
};