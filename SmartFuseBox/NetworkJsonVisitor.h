#pragma once

#include "IWifiClient.h"

class NetworkJsonVisitor
{
public:
	virtual void formatWifiStatusJson(IWifiClient* client) = 0;
	virtual ~NetworkJsonVisitor() = default;
};