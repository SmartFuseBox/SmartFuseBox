#include "SystemDefinitions.h"

struct SensorFieldDescriptor {
	const char label[20];       // e.g. "Analogue Pin", "Threshold"
	const char type[20];        // "gpio", "int8", "int16", "none"
	int16_t minVal;
	int16_t maxVal;
	int16_t defaultValue;
};

struct SensorTypeDescriptor {
	const char* name;
	SensorFieldDescriptor pins[ConfigMaxSensorPins];
	SensorFieldDescriptor options1[2];
	SensorFieldDescriptor options2[2];
};

// Indexed by SensorIdList value
constexpr SensorTypeDescriptor SensorDescriptors[] = {
	[static_cast<size_t>(SensorIdList::WaterSensor)] = {
		.name = "Water Sensor",
		.pins = {
			{ "Data Pin", "gpio", 0, 39, 255 },
			{ "Power Pin", "gpio", 0, 39, 255 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options1 = { { "Unused", "none", 0, 0, 0 }, { "Unused", "none", 0, 0, 0 } },
		.options2 = { { "Unused", "none", 0, 0, 0 }, { "Unused", "none", 0, 0, 0 } },
	},
	[static_cast<size_t>(SensorIdList::Dht11Sensor)] = {
		.name = "DHT11",
		.pins = {
			{ "Data Pin", "gpio", 0, 39, 255 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options1 = { { "Unused", "none", 0, 0, 0 }, { "Unused", "none", 0, 0, 0 } },
		.options2 = { { "Unused", "none", 0, 0, 0 }, { "Unused", "none", 0, 0, 0 } },
	},
	[static_cast<size_t>(SensorIdList::LightSensor)] = {
		.name = "Light Sensor",
		.pins = {
			{ "Analogue Pin", "gpio", 0, 39, 255 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options1 = {
			{ "Mode", "int8", 0, 1, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options2 = {
			{ "Threshold", "int16", 0, 4095, 512 },
			{ "Unused", "none", 0, 0, 0 },
		},
	},
	[static_cast<size_t>(SensorIdList::GpsSensor)] = {
		.name = "GPS",
		.pins = {
			{ "RX Pin", "gpio", 0, 39, 255 },
			{ "TX Pin", "gpio", 0, 39, 255 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options1 = {
			{ "UART Num", "int8", 1, 2, 2 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options2 = { { "Unused", "none", 0, 0, 0 }, { "Unused", "none", 0, 0, 0 } },
	},
	[static_cast<size_t>(SensorIdList::SystemSensor)] = {
		.name = "System Sensor",
		.pins = {
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options1 = { { "Unused", "none", 0, 0, 0 }, { "Unused", "none", 0, 0, 0 } },
		.options2 = { { "Unused", "none", 0, 0, 0 }, { "Unused", "none", 0, 0, 0 } },
	},
	[static_cast<size_t>(SensorIdList::BinaryPresenceSensor)] = {
		.name = "Binary Presence",
		.pins = {
			{ "Sensor Pin", "gpio", 0, 39, 255 },
			{ "onDetected", "int8", 0, 255, 0 },
			{ "onClear", "int8", 0, 255, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options1 = {
			{ "Active State", "int8", 0, 1, 1 },
			{ "Detect Action", "int8", 0, 255, 0 },
		},
		.options2 = {
			{ "Unused", "none", 0, 0, 0 },
			{ "Clear Action", "int16", 0, 255, 0 },
		},
	},
	[static_cast<size_t>(SensorIdList::VoltageSensor)] = {
		.name = "Voltage Sensor",
		.pins = {
			{ "Analogue Pin", "gpio", 0, 39, 255 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
			{ "Unused", "none", 0, 0, 0 },
		},
		.options1 = {
			{ "ADC Vref", "int8", 0, 50, 50 },
			{ "R2 (tenths)", "int8", 0, 255, 75 },
		},
		.options2 = {
			{ "R1 (kOhm)", "int16", 0, 1000, 30 },
			{ "Low Warn (Vx10)", "int16", 0, 500, 0 },
		},
	},
};

static_assert(
	std::size(SensorDescriptors) == static_cast<size_t>(SensorIdList::Count),
	"SensorDescriptors missing an entry for a SensorIdList value!");