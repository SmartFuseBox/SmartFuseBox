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

#include "Local.h"
#include "Queue.h"
#include "PowerControlHubConstants.h"
#include "WarningManager.h"
#include "WarningType.h"
#include "LoggingSupport.h"
#include "JsonVisitor.h"
#include "BaseSensor.h"

/// Poll the sensor every 5 seconds.
constexpr uint64_t VoltageSensorCheckIntervalMs = 5000;

/// Rolling average depth (10 readings).
constexpr uint8_t VoltageSensorQueueSize = 10;

constexpr uint16_t VoltageSensorAdcFullScale = ADC_FULL_SCALE;

#if defined(ESP32)
/// ESP32 ADC reference is ~3.3V with default attenuation (DB_11)
/// Note: Actual voltage depends on attenuation setting
constexpr int8_t VoltageSensorDefaultVrefTenths = 33;
#else
/// Arduino boards typically use 5.0V reference
constexpr int8_t VoltageSensorDefaultVrefTenths = 50;
#endif

/// Default R1 in kΩ — matches the 30k/7.5k module variant.
constexpr int16_t VoltageSensorDefaultR1kOhm = 30;

/// Default R2 in tenths of kΩ (75 = 7.5 kΩ) — matches the 30k/7.5k module variant.
constexpr int8_t VoltageSensorDefaultR2TenthskOhm = 75;

/**
 * @brief Sensor handler for the DC 0–25 V voltage detection module.
 *
 * The module uses a resistor voltage divider to scale the measured voltage
 * down to a range readable by the MCU's ADC.  The measured voltage is
 * recovered with:
 *
 *   Vout = (adcValue / AdcFullScale) * Vref
 *   Vin  = Vout * (R1 + R2) / R2
 *
 * A 10-reading rolling average is maintained to smooth noise.
 *
 * Configuration (SensorEntry fields):
 *   pins[0]    – analog input pin
 *   options1[0] – ADC reference voltage in tenths of a volt (e.g. 50 = 5.0 V,
 *                 33 = 3.3 V).  0 defaults to 50 (5.0 V).
 *   options1[1] – R2 in tenths of kΩ (e.g. 75 = 7.5 kΩ).
 *                 0 defaults to 75 (7.5 kΩ).
 *   options2[0] – R1 in whole kΩ (e.g. 30 = 30 kΩ).
 *                 0 defaults to 30 (30 kΩ).
 *   options2[1] – Low-voltage warning threshold in tenths of a volt
 *                 (e.g. 114 = 11.4 V).  0 disables the warning.
 */
class VoltageSensorHandler : public BaseSensor, public BroadcastLoggerSupport
{
private:
	WarningManager* _warningManager;
	const uint8_t _sensorPin;
	bool _initialized;

	/// Pre-computed divisor: R2 / (R1 + R2), avoids float division every tick.
	float _voltageDivisor;

	/// ADC reference voltage in volts.
	float _vref;

	/// Low-voltage warning threshold in volts.  0.0 = disabled.
	float _warnThresholdV;

	Queue<uint16_t> _voltageQueue;

	/// Latest raw ADC reading.
	uint16_t _latestRaw;

	/// Whether the low-voltage warning is currently active.
	bool _warningActive;

#if defined(MQTT_SUPPORT)
	char _slugVoltage[32];
	char _slugAvgVoltage[40];
	char _nameVoltage[48];
	char _nameAvgVoltage[48];
#endif

	/**
	 * @brief Convert a raw ADC count to the actual input voltage.
	 * @param raw  10-bit ADC count (0–1023).
	 * @return Input voltage in volts.
	 */
	float rawToVolts(uint16_t raw) const
	{
		float vout = (static_cast<float>(raw) / static_cast<float>(VoltageSensorAdcFullScale)) * _vref;
		return (_voltageDivisor > 0.0f) ? (vout / _voltageDivisor) : 0.0f;
	}

protected:
	void initialize() override
	{
		if (_sensorPin == PinDisabled)
		{
			// invalid pin configuration; raise a warning and disable this sensor
			if (_warningManager != nullptr)
				_warningManager->raiseWarning(WarningType::VoltageSensorFailure);

			_initialized = false; 
			return;
		}

		pinMode(_sensorPin, INPUT);
		_initialized = true;
	}

	uint64_t update() override
	{
		if (!_initialized)
			return VoltageSensorCheckIntervalMs;

		_latestRaw = static_cast<uint16_t>(analogRead(_sensorPin));

		if (_voltageQueue.isFull())
			_voltageQueue.dequeue();

		_voltageQueue.enqueue(_latestRaw);

		float instantV = rawToVolts(_latestRaw);
		float avgV     = rawToVolts(_voltageQueue.average());

		// Raise or clear low-voltage warning based on the smoothed average.
		if (_warningManager != nullptr && _warnThresholdV > 0.0f)
		{
			if (!_warningActive && avgV < _warnThresholdV)
			{
				_warningManager->raiseWarning(WarningType::LowVoltage);
				_warningActive = true;
			}
			else if (_warningActive && avgV >= _warnThresholdV)
			{
				_warningManager->clearWarning(WarningType::LowVoltage);
				_warningActive = false;
			}
		}

		// Encode voltages as millivolts for the integer key-value transport.
		StringKeyValue params[2];
		strncpy(params[0].key, "v", sizeof(params[0].key));
		snprintf(params[0].value, sizeof(params[0].value), "%u",
			static_cast<unsigned int>(instantV * 1000.0f + 0.5f));
		strncpy(params[1].key, "avg", sizeof(params[1].key));
		snprintf(params[1].value, sizeof(params[1].value), "%u",
			static_cast<unsigned int>(avgV * 1000.0f + 0.5f));

		sendCommand(SensorVoltage, params, 2);

		char dbuf[56];
		snprintf(dbuf, sizeof(dbuf), "raw=%u inst=%.2fV avg=%.2fV", _latestRaw, instantV, avgV);
		sendDebug(dbuf, _name);

		return VoltageSensorCheckIntervalMs;
	}

public:
	/**
	 * @param broadcastManager     Shared broadcast/logging bus.
	 * @param warningManager       Warning manager; may be nullptr.
	 * @param sensorPin            Analog pin wired to the module's signal output.
	 * @param vrefTenths           ADC reference voltage in tenths of a volt
	 *                             (e.g. 50 = 5.0 V).  Pass 0 to use the default.
	 * @param r1kOhm               R1 resistor value in whole kΩ.  0 → default (30).
	 * @param r2TenthskOhm         R2 resistor value in tenths of kΩ.  0 → default (75 = 7.5 kΩ).
	 * @param warnThresholdTenths  Low-voltage threshold in tenths of a volt (e.g. 114 = 11.4 V).
	 *                             0 disables the warning.
	 * @param name                 Friendly sensor name.
	 */
	VoltageSensorHandler(BroadcastManager* broadcastManager,
						 WarningManager*   warningManager,
						 uint8_t           sensorPin,
						 int8_t            vrefTenths,
						 int16_t           r1kOhm,
						 int8_t            r2TenthskOhm,
						 int16_t           warnThresholdTenths,
						 const char*       name = "Voltage")
		: BaseSensor(name),
		  BroadcastLoggerSupport(broadcastManager),
		  _warningManager(warningManager),
		  _sensorPin(sensorPin),
		  _voltageQueue(VoltageSensorQueueSize, 0),
		  _latestRaw(0),
		  _warningActive(false)
	{
		// Resolve ADC reference voltage.
		int8_t effectiveVref = (vrefTenths != 0) ? vrefTenths : VoltageSensorDefaultVrefTenths;
		_vref = static_cast<float>(effectiveVref) / 10.0f;

		// Resolve resistor values (convert to Ω for the divisor).
		float r1 = static_cast<float>((r1kOhm != 0) ? r1kOhm : VoltageSensorDefaultR1kOhm) * 1000.0f;
		float r2 = static_cast<float>((r2TenthskOhm != 0) ? r2TenthskOhm : VoltageSensorDefaultR2TenthskOhm) / 10.0f * 1000.0f;
		_voltageDivisor = (r1 + r2 > 0.0f) ? (r2 / (r1 + r2)) : 0.0f;

		// Resolve warning threshold.
		_warnThresholdV = (warnThresholdTenths != 0)
			? (static_cast<float>(warnThresholdTenths) / 10.0f)
			: 0.0f;

#if defined(MQTT_SUPPORT)
		snprintf(_slugVoltage, sizeof(_slugVoltage), "%s_voltage", _safeSlug);
		snprintf(_slugAvgVoltage, sizeof(_slugAvgVoltage), "%s_avg_voltage", _safeSlug);
		snprintf(_nameVoltage, sizeof(_nameVoltage), "%s Voltage", _name);
		snprintf(_nameAvgVoltage, sizeof(_nameAvgVoltage), "%s Avg Voltage", _name);
#endif
	}

	void formatStatusJson(char* buffer, size_t size) override
	{
		float instantV = rawToVolts(_latestRaw);
		float avgV = rawToVolts(_voltageQueue.average());
		snprintf(buffer, size, "\"voltage\":%.2f,\"avg\":%.2f", instantV, avgV);
	}

	SensorIdList getSensorIdType() const override
	{
		return SensorIdList::VoltageSensor;
	}

	SensorType getSensorType() const override
	{
		return SensorType::Local;
	}

	const char* getSensorCommandId() const override
	{
		return SensorVoltage;
	}

#if defined(MQTT_SUPPORT)

	uint8_t getMqttChannelCount() const override
	{
		if (!_initialized)
			return 0;

		return 2;
	}

	MqttSensorChannel getMqttChannel(uint8_t channelIndex) const override
	{
		switch (channelIndex)
		{
			case 0:
				return { _nameVoltage, _slugVoltage, "voltage", "voltage", "V", false };
			default:
				return { _nameAvgVoltage, _slugAvgVoltage, "voltage", "voltage", "V", false };
		}
	}

	void getMqttValue(uint8_t channelIndex, char* buffer, size_t size) const override
	{
		float v = (channelIndex == 0)
			? rawToVolts(_latestRaw)
			: rawToVolts(_voltageQueue.average());
		snprintf(buffer, size, "%.2f", v);
	}

#endif
};
