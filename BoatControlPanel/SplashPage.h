/*
 * SmartFuseBox
 * Copyright (C) 2025 Simon Carter (s1cart3r@gmail.com)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
#pragma once

#include "NextionIds.h"
#include <NextionControl.h>
#include "ConfigManager.h"

class SplashPage : public BaseDisplayPage
{
public:
    SplashPage(Stream* serialPort)
		: BaseDisplayPage(serialPort)
    {}

    uint8_t getPageId() const override { return PageSplash; }

    void onEnterPage() override
    {
		// No special actions needed on entering splash page
    }

    void begin() override
    {
        Config* config = ConfigManager::getConfigPtr();

        if (!config)
        {
            return;
        }

        // initialize global variables
        sendText(F("pageSplash.vaBoatName"), config->vessel.name);
        sendText(F("pageSplash.vaHomeB1"), config->relay.shortNames[0]);
        sendText(F("pageSplash.vaHomeB2"), config->relay.shortNames[1]);
        sendText(F("pageSplash.vaHomeB3"), config->relay.shortNames[2]);
        sendText(F("pageSplash.vaHomeB4"), config->relay.shortNames[3]);

        // Relay page global variables
        sendText(F("pageSplash.vaRelayName1"), config->relay.longNames[0]);
        sendText(F("pageSplash.vaRelayName2"), config->relay.longNames[1]);
        sendText(F("pageSplash.vaRelayName3"), config->relay.longNames[2]);
        sendText(F("pageSplash.vaRelayName4"), config->relay.longNames[3]);
        sendText(F("pageSplash.vaRelayName5"), config->relay.longNames[4]);
        sendText(F("pageSplash.vaRelayName6"), config->relay.longNames[5]);
        sendText(F("pageSplash.vaRelayName7"), config->relay.longNames[6]);
        sendText(F("pageSplash.vaRelayName8"), config->relay.longNames[7]);
    }

    void refresh(unsigned long now) override
    {
		(void)now;
        // No periodic refresh needed for splash page
    }
};