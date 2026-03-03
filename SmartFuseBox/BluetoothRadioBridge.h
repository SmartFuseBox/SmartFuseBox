#pragma once

#include "Local.h"

#if defined(BLUETOOTH_SUPPORT)
    #include "BluetoothController.h"
    using PlatformBluetoothRadio = BluetoothController;
#else
    #include "NullBluetoothRadio.h"
    using PlatformBluetoothRadio = NullBluetoothRadio;
#endif
