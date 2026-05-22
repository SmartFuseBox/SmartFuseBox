/*
 * PowerControlHub
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

/*
 * PinGuard.h - framework-owned compile-time pin validation (do not edit)
 *
 * Selects the correct pin table automatically using the IDF-provided macros
 * CONFIG_IDF_TARGET_ESP32S3 and CONFIG_IDF_TARGET_ESP32, which are emitted by
 * the Arduino-ESP32 toolchain (via sdkconfig.h) based on the board chosen in
 * the IDE — no manual defines in Local.h are required.
 *
 * Table tiers:
 *   PinCategory::Hard     — always rejected (flash-reserved GPIO, input-only
 *                           used as output)
 *   PinCategory::Advisory — risky but sometimes legitimate (strapping pins,
 *                           UART0 TX/RX); behaviour controlled by PinGuardMode
 *   PinCategory::Safe     — no restriction
 *
 * PinGuard mode is a persistent system setting stored in SystemHeader::pinGuardFlags.
 * Call PinGuard::setMode() after loading the system header to apply it at runtime.
 * Mode is configured via the F14 command (a=advisory, b=bypass).
 *
 * Usage:
 *   PinGuardResult r = PinGuard::validate(pin, PinUse::Output);
 *   if (PinGuard::isBlocked(r)) { ... }
 *
 * To add a new board: add a new CONFIG_IDF_TARGET_* block below.
 * PinGuard.h is framework-owned — do not edit Local.h for table entries.
 */

#include <stdint.h>
#include "SystemDefinitions.h"

// ─── Pin Use ─────────────────────────────────────────────────────────────────
// Describes the intended use of a pin.  Input-only GPIO are fine for Input,
// Sensor reads etc. but are hard-blocked for anything that requires OUTPUT.
enum class PinUse : uint8_t
{
	Output,     // generic digital output (e.g. LED, buzzer)
	Input,      // generic digital / analog input
	SpiSck,     // SPI clock — must be output-capable
	SpiMosi,    // SPI MOSI — must be output-capable
	SpiMiso,    // SPI MISO — input only is fine
	SpiCs,      // SPI chip-select — must be output-capable
	Relay,      // relay drive pin — must be output-capable
	Sensor,     // sensor signal pin — input only is fine
};

// ─── Pin Category ─────────────────────────────────────────────────────────────
enum class PinCategory : uint8_t
{
	Safe     = 0,   // unrestricted
	Advisory = 1,   // risky; behaviour governed by PinGuardMode
	Hard     = 2,   // always rejected for the given use
};

// ─── Validation Result ────────────────────────────────────────────────────────
enum class PinGuardResult : uint8_t
{
	Safe            = 0,    // pin is fine for the requested use
	AdvisoryBlocked = 1,    // advisory pin blocked (mode does not permit advisory)
	HardBlocked     = 2,    // hard-blocked — must not be used for this purpose
	Disabled        = 3,    // pin == PinDisabled (0xFF) — treated as not fitted
};

// ─── PinGuard Mode ────────────────────────────────────────────────────────────
// Persistent system setting stored in SystemHeader::pinGuardFlags.
// Configured via the F14 command.
namespace PinGuardMode
{
	constexpr uint8_t None         = 0x00;  // strict — advisory pins are blocked
	constexpr uint8_t AllowAdvisory = 0x01; // advisory pins are permitted
	constexpr uint8_t Bypass        = 0x02; // skip all checks — always returns Safe
}

// ─── Internal helpers — not for direct use ───────────────────────────────────
namespace PinGuardInternal
{
	// Returns true when the requested PinUse requires an output-capable pin.
	inline bool requiresOutput(PinUse use)
	{
		return use == PinUse::Output
			|| use == PinUse::SpiSck
			|| use == PinUse::SpiMosi
			|| use == PinUse::SpiCs
			|| use == PinUse::Relay;
	}
}

// ─── Per-variant pin tables ───────────────────────────────────────────────────
//
// Each entry is { gpio, category }.
// Hard entries only need to be listed for the use-cases they block.
// The validate() function applies "Hard if requiresOutput and pin is input-only"
// automatically — entries here are for additional hard blocks (flash, etc.)
// and for advisory classifications.

struct PinTableEntry
{
	uint8_t      gpio;
	PinCategory  category;
};

#if defined(CONFIG_IDF_TARGET_ESP32S3)

// ── ESP32-S3 Dev Module ───────────────────────────────────────────────────────
// Hard blocks
//   GPIO 26–32 : connected to internal Octal flash/PSRAM — hard for ALL uses
//   GPIO 45–46 : input-only on S3 silicon (promoted to Hard for output uses)
// Advisory
//   GPIO 0,3,19,20,45,46 : strapping / USB / UART0
static const PinTableEntry _pinTable[] PROGMEM = {
	// Flash/PSRAM-reserved — hard for ALL uses
	{ 26, PinCategory::Hard },
	{ 27, PinCategory::Hard },
	{ 28, PinCategory::Hard },
	{ 29, PinCategory::Hard },
	{ 30, PinCategory::Hard },
	{ 31, PinCategory::Hard },
	{ 32, PinCategory::Hard },
	// Input-only on S3 — Advisory here; validate() promotes to Hard for output
	{ 45, PinCategory::Advisory },
	{ 46, PinCategory::Advisory },
	// Strapping / USB / UART0 — advisory for all uses
	{  0, PinCategory::Advisory },
	{  3, PinCategory::Advisory },   // UART0 RX
	{ 19, PinCategory::Advisory },   // USB D-
	{ 20, PinCategory::Advisory },   // USB D+
};
static constexpr uint8_t _pinTableSize = sizeof(_pinTable) / sizeof(_pinTable[0]);

// Input-only GPIOs on S3: GPIO 45 and 46 form a contiguous range
static constexpr uint8_t _inputOnlyMin = 45;
static constexpr uint8_t _inputOnlyMax = 46;
#elif defined(CONFIG_IDF_TARGET_ESP32)

// ── Classic ESP32 (NodeMCU-32S, DevKitC, and all other ESP32 boards) ─────────
// Hard blocks
//   GPIO 6–11  : connected to internal SPI flash — touching causes immediate crash
//   GPIO 34–39 : input-only silicon (promoted to Hard for output uses)
// Advisory
//   GPIO 0,1,2,3,5,12,15 : strapping / UART0
static const PinTableEntry _pinTable[] PROGMEM = {
	// Flash-reserved — hard for ALL uses
	{  6, PinCategory::Hard },
	{  7, PinCategory::Hard },
	{  8, PinCategory::Hard },
	{  9, PinCategory::Hard },
	{ 10, PinCategory::Hard },
	{ 11, PinCategory::Hard },
	// Input-only — Advisory here; validate() promotes to Hard for output uses
	{ 34, PinCategory::Advisory },
	{ 35, PinCategory::Advisory },
	{ 36, PinCategory::Advisory },
	{ 37, PinCategory::Advisory },
	{ 38, PinCategory::Advisory },
	{ 39, PinCategory::Advisory },
	// Strapping / UART0 — advisory for all uses
	{  0, PinCategory::Advisory },
	{  1, PinCategory::Advisory },   // UART0 TX
	{  2, PinCategory::Advisory },
	{  3, PinCategory::Advisory },   // UART0 RX
	{  5, PinCategory::Advisory },
	{ 12, PinCategory::Advisory },
	{ 15, PinCategory::Advisory },
};
static constexpr uint8_t _pinTableSize = sizeof(_pinTable) / sizeof(_pinTable[0]);

// Input-only GPIO range for classic ESP32
static constexpr uint8_t _inputOnlyMin = 34;
static constexpr uint8_t _inputOnlyMax = 39;

#endif // CONFIG_IDF_TARGET_*

// ─── PinGuard ────────────────────────────────────────────────────────────────

class PinGuard
{
public:
	/**
	 * @brief Apply a PinGuardMode bitmask loaded from SystemHeader::pinGuardFlags.
	 *        Call this once after the system header is loaded, before any hardware init.
	 */
	static void setMode(uint8_t mode) { _mode = mode; }

	/**
	 * @brief Return the current PinGuardMode bitmask.
	 */
	static uint8_t getMode() { return _mode; }

	/**
	 * @brief Convenience helper — returns true when the result means the pin is unusable.
	 */
	static bool isBlocked(PinGuardResult result)
	{
		return result == PinGuardResult::HardBlocked || result == PinGuardResult::AdvisoryBlocked;
	}

	/**
	 * @brief Validate a pin for a given use.
	 *
	 * @param pin GPIO number to validate (255/PinDisabled returns Disabled).
	 * @param use Intended use of the pin.
	 * @return PinGuardResult indicating whether the pin is usable.
	 *
	 * Mode is read from the static _mode field set via setMode():
	 *   PinGuardMode::Bypass       — always returns Safe (no checks at all)
	 *   PinGuardMode::AllowAdvisory — advisory pins are allowed
	 *   If both bits are set, Bypass takes precedence and returns Safe immediately.
	 */
	static PinGuardResult validate(uint8_t pin, PinUse use)
	{
		if (pin == PinDisabled)
			return PinGuardResult::Disabled;

		// Bypass: skip all checks
		if (_mode & PinGuardMode::Bypass)
			return PinGuardResult::Safe;

#if defined(CONFIG_IDF_TARGET_ESP32S3) || defined(CONFIG_IDF_TARGET_ESP32)
		const bool needsOutput = PinGuardInternal::requiresOutput(use);
		const bool allowAdvisory = (_mode & PinGuardMode::AllowAdvisory) != 0;

		// Walk the pin table
		for (uint8_t i = 0; i < _pinTableSize; ++i)
		{
			uint8_t tablePin = pgm_read_byte(&_pinTable[i].gpio);
			uint8_t tableCategory = pgm_read_byte(&_pinTable[i].category);

			if (tablePin != pin)
				continue;

			PinCategory cat = static_cast<PinCategory>(tableCategory);

			if (cat == PinCategory::Hard)
				return PinGuardResult::HardBlocked;

			if (cat == PinCategory::Advisory)
			{
				// Input-only pins: hard-block for output, transparent for input
				const bool isInputOnly = (pin >= _inputOnlyMin && pin <= _inputOnlyMax);

				if (isInputOnly && needsOutput)
					return PinGuardResult::HardBlocked;

				if (isInputOnly && !needsOutput)
					return PinGuardResult::Safe;   // input-only is fine here

				// Ordinary advisory pin (strapping / UART0)
				if (!allowAdvisory)
					return PinGuardResult::AdvisoryBlocked;
			}

			return PinGuardResult::Safe;
		}

		// Not found in table — safe
		return PinGuardResult::Safe;

#else
		// Non-ESP32 boards — no pin restrictions modelled yet
		(void)use;
		return PinGuardResult::Safe;
#endif
	}

	/**
	 * @brief Return a short human-readable reason string for a blocked result.
	 *        String is in PROGMEM — use with F()-style printing or strcpy_P.
	 */
	static const __FlashStringHelper* reasonString(PinGuardResult result)
	{
		switch (result)
		{
			case PinGuardResult::HardBlocked:
				return F("hard-blocked (flash/input-only)");
			case PinGuardResult::AdvisoryBlocked:
				return F("advisory (strapping/UART pin)");
			case PinGuardResult::Disabled:
				return F("disabled (255)");
			default:
				return F("safe");
		}
	}

	/**
	 * @brief Return a short human-readable string for a PinUse value.
	 */
	static const __FlashStringHelper* useString(PinUse use)
	{
		switch (use)
		{
			case PinUse::Output:
				return F("Output");
			case PinUse::Input:
				return F("Input");
			case PinUse::SpiSck:
				return F("SPI-SCK");
			case PinUse::SpiMosi:
				return F("SPI-MOSI");
			case PinUse::SpiMiso:
				return F("SPI-MISO");
			case PinUse::SpiCs:
				return F("SPI-CS");
			case PinUse::Relay:
				return F("Relay");
			case PinUse::Sensor:
				return F("Sensor");
			default:
				return F("Unknown");
		}
	}

private:
	inline static uint8_t _mode = PinGuardMode::None;
};
