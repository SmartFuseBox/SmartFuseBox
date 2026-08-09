#pragma once
#include "Config.h"

enum class LocationTypeSubDescriptor : uint8_t
{
	Boat = 0x00,
	Other = 0x01
};

struct LocationTypeDescriptor
{
	uint8_t id;
	LocationTypeSubDescriptor subType;
	const char* description;
};

// Indexed by LocationType enum value
constexpr LocationTypeDescriptor LocationTypeDescriptors[] = {
	[static_cast<size_t>(LocationType::Power)] = { static_cast<uint8_t>(LocationType::Power), LocationTypeSubDescriptor::Boat,  "Power boat" },
	[static_cast<size_t>(LocationType::Sail)] = { static_cast<uint8_t>(LocationType::Sail), LocationTypeSubDescriptor::Boat,  "Sailing boat" },
	[static_cast<size_t>(LocationType::Fishing)] = { static_cast<uint8_t>(LocationType::Fishing), LocationTypeSubDescriptor::Boat,  "Fishing boat" },
	[static_cast<size_t>(LocationType::Yacht)] = { static_cast<uint8_t>(LocationType::Yacht), LocationTypeSubDescriptor::Boat,  "Yacht" },
	[static_cast<size_t>(LocationType::Shed)] = { static_cast<uint8_t>(LocationType::Shed), LocationTypeSubDescriptor::Other, "Shed" },
	[static_cast<size_t>(LocationType::Basement)] = { static_cast<uint8_t>(LocationType::Basement), LocationTypeSubDescriptor::Other, "Basement" },
	[static_cast<size_t>(LocationType::Workshop)] = { static_cast<uint8_t>(LocationType::Workshop), LocationTypeSubDescriptor::Other, "Workshop" },
	[static_cast<size_t>(LocationType::Garage)] = { static_cast<uint8_t>(LocationType::Garage), LocationTypeSubDescriptor::Other, "Garage" },
	[static_cast<size_t>(LocationType::Bedroom)] = { static_cast<uint8_t>(LocationType::Bedroom), LocationTypeSubDescriptor::Other, "Bedroom" },
	[static_cast<size_t>(LocationType::Office)] = { static_cast<uint8_t>(LocationType::Office), LocationTypeSubDescriptor::Other, "Office" },
	// Note: LocationType::Other has value 0xFF and is not included in the indexed array
};

static_assert(std::size(LocationTypeDescriptors) == static_cast<size_t>(LocationType::Office) + 1,
	"LocationTypeDescriptors must cover all LocationType enum values up to Office. Update descriptors when enum changes.");
