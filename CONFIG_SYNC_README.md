# Config Synchronization Feature for SmartFuseBox

## Overview
This feature enables the SmartFuseBox (Fuse Box Controller) to periodically retrieve and synchronize configuration settings from the Boat Control Panel, ensuring both devices stay in sync.

## Architecture

### Components

1. **ConfigSyncManager** (SmartFuseBox/ConfigSyncManager.h/cpp)
   - Manages periodic config synchronization
   - Sends C1 (ConfigGetSettings) requests at configurable intervals (default: 5 minutes)
   - Snapshots config before sync to detect changes
   - Uses timeout-based completion detection (5 second timeout)
   - Only saves to EEPROM if config actually changed
   - Provides debug messages for monitoring sync status

2. **ConfigCommandHandler** (Updated)
   - Receives C3-C27 commands from Control Panel
   - Updates in-memory Config object
   - Notifies ConfigSyncManager when config commands are received during sync

3. **AckCommandHandler** (Updated for FUSE_BOX_CONTROLLER)
   - Handles ACK:C1=ok to confirm sync started
   - Provides config sync manager reference

## How It Works

### Sync Flow
```
1. ConfigSyncManager (timer expires)
   ├─> Snapshots current config (memcpy)
   └─> Sends C1 (ConfigGetSettings) to Control Panel

2. Control Panel receives C1
   ├─> Responds with ACK:C1=ok
   └─> Sends config data: C3, C4, C5, ..., C27

3. SmartFuseBox receives responses
   ├─> AckCommandHandler: Logs "Config sync started" (ACK:C1=ok)
   └─> ConfigCommandHandler: 
       ├─> Updates in-memory Config for each C3-C27
       └─> Notifies ConfigSyncManager (resets timeout)

4. ConfigSyncManager (after 5s timeout)
   ├─> Compares snapshot vs current config (memcmp)
   ├─> If changed:
   │   ├─> Saves to EEPROM (ConfigController->save())
   │   └─> Logs "Config synced and saved"
   └─> If unchanged: Silent (no save needed)
```

### Timeout Mechanism
- After sending C1, ConfigSyncManager waits for config commands
- Each C3-C27 command received resets the timeout timer
- After 5 seconds of no config commands, sync is considered complete
- This handles variable number of responses (not all configs may be sent)

## Configuration

### Sync Interval
Default: 300000ms (5 minutes)
```cpp
ConfigSyncManager configSyncManager(&commandMgrLink, &configController, 300000);
```

### Sync Timeout
Defined in ConfigSyncManager.h:
```cpp
static constexpr unsigned long SYNC_TIMEOUT_MS = 5000; // 5 seconds
```

### Enable/Disable Sync
```cpp
configSyncManager.setEnabled(false); // Disable auto-sync
configSyncManager.setEnabled(true);  // Re-enable
```

### Manual Sync Request
```cpp
configSyncManager.requestSync(); // Immediate sync (ignores timer)
```

## Debug Messages
Monitor serial output for:
- `"Config sync started"` - ACK:C1=ok received
- `"Config synced and saved"` - Changes detected and saved
- `"Config sync failed to save"` - Save to EEPROM failed

## Commands Reference

### C1 - Get Settings (ConfigGetSettings)
- **Sender**: SmartFuseBox → Control Panel
- **Purpose**: Request all config settings
- **Response**: Control Panel sends ACK:C1=ok followed by C3-C27 commands

### C3-C27 - Config Data
Individual config commands sent by Control Panel:
- C3: Boat name
- C4: Relay names (8 commands)
- C5: Home button mappings (4 commands)
- C6: Button colors (8 commands)
- C7: Vessel type
- C8: Sound relay ID
- C9: Sound start delay
- C10-C17: WiFi/Bluetooth settings (R4 only)
- C18: Default relay states (8 commands)
- C19: Linked relays (variable)

## Integration Points

### SmartFuseBox.ino
```cpp
// 1. Include header
#include "ConfigSyncManager.h"

// 2. Instantiate manager
ConfigSyncManager configSyncManager(&commandMgrLink, &configController, 300000);

// 3. Link to handlers (in setup())
ackHandler.setConfigSyncManager(&configSyncManager, &configController);
configHandler.setConfigSyncManager(&configSyncManager);

// 4. Update in loop
void loop() {
    // ... other updates ...
    configSyncManager.update(now);
}
```

## Memory Considerations
- ConfigSyncManager stores a Config snapshot (~100-200 bytes depending on Config struct size)
- Uses memcmp for efficient change detection
- Only saves to EEPROM when changes are detected (reduces wear)

## Future Enhancements
- Add retry logic for failed syncs
- Implement exponential backoff if Control Panel doesn't respond
- Add sync status LED indicator
- Log sync statistics (success/failure count, last sync time)
- Add selective config sync (only specific settings)
