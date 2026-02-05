# Config Synchronization Feature for SmartFuseBox

## Overview
This feature enables the SmartFuseBox (Fuse Box Controller) to periodically retrieve and synchronize configuration settings from the Boat Control Panel, ensuring both devices stay in sync.

## Architecture

### Components

1. **ConfigSyncManager** (ConfigSyncManager.h/cpp)
   - Manages periodic config synchronization
   - Sends C1 (ConfigGetSettings) requests at configurable intervals
   - Snapshots config before sync to detect changes
   - Uses timeout-based completion detection (5 second timeout)
   - Only saves to EEPROM if config actually changed
   - Implements smart retry strategy (10s when failing, 5min when successful)
   - Raises/clears SyncFailed warning based on sync status

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
1. ConfigSyncManager (timer expires or requestSync())
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
   ├─> If NO response received:
   │   └─> Raises SyncFailed warning
   ├─> If changed:
   │   ├─> Saves to EEPROM (ConfigController->save())
   │   ├─> Success: Clears SyncFailed warning
   │   └─> Failure: Raises SyncFailed warning
   └─> If unchanged: Clears SyncFailed warning (already synced)
```

### Smart Retry Strategy
```
Normal Operation (everything works):
└─> Sync every 5 minutes

Sync Failing (SyncFailed warning active):
└─> Retry every 10 seconds (fast recovery)

After Successful Sync:
└─> Clear SyncFailed warning
└─> Return to 5 minute interval
```

## Configuration

### Sync Intervals
```cpp
// ConfigSyncManager.h
constexpr unsigned long DefaultConfigSyncIntervalMs = 300000; // 5 minutes (normal)
constexpr unsigned long FastConfigSyncRetryMs = 10000;        // 10 seconds (retry)
```

### Sync Timeout
```cpp
// ConfigSyncManager.h
static constexpr unsigned long SYNC_TIMEOUT_MS = 5000; // 5 seconds
```

### Enable/Disable Sync
```cpp
configSyncManager.setEnabled(false); // Disable auto-sync
configSyncManager.setEnabled(true);  // Re-enable
```

### Manual Sync Request
```cpp
configSyncManager.requestSync(); // Immediate sync (called in setup())
```

## Debug Messages
Monitor serial output for:
- `"Config sync started"` - ACK:C1=ok received
- `"Config synced and saved"` - Changes detected and saved
- `"Config sync failed to save"` - Save to EEPROM failed
- `"Config sync failed - no response"` - Control Panel didn't respond

## Warning Management

### SyncFailed Warning (WarningType::SyncFailed)
**Raised when:**
- Control Panel doesn't respond to C1 request (timeout with no config received)
- EEPROM save fails after config change

**Cleared when:**
- Config successfully synced and saved
- Sync completes with no changes (already in sync)

**Effect:**
- When active: Triggers fast retry (10 seconds)
- When cleared: Returns to normal interval (5 minutes)

## Commands Reference

### C1 - Get Settings (ConfigGetSettings)
- **Sender**: SmartFuseBox → Control Panel
- **Purpose**: Request all config settings
- **Response**: Control Panel sends ACK:C1=ok followed by C3-C27 commands

### C3-C27 - Config Data
Individual config commands sent by Control Panel:
- C3: Boat name
- C4: Relay names (8 commands, one per relay)
- C5: Home button mappings (4 commands)
- C6: Button colors (8 commands)
- C7: Vessel type
- C8: Sound relay ID
- C9: Sound start delay
- C10-C17: WiFi/Bluetooth settings (R4 only)
- C18: Default relay states (8 commands)
- C19: Linked relays (variable count)

## Integration Points

### SmartFuseBox.ino
```cpp
// 1. Include header
#include "ConfigSyncManager.h"

// 2. Instantiate manager
ConfigSyncManager configSyncManager(&commandMgrComputer, &commandMgrLink, 
                                   &configController, &warningManager);

// 3. Link to handlers (in setup())
ackHandler.setConfigSyncManager(&configSyncManager, &configController);
configHandler.setConfigSyncManager(&configSyncManager);

// 4. Request initial sync (in setup())
configSyncManager.requestSync();

// 5. Update in loop
void loop() {
    // ... other updates ...
    configSyncManager.update(now);
}
```

## Memory Considerations
- ConfigSyncManager stores a Config snapshot (~100-200 bytes depending on Config struct size)
- Uses memcmp for efficient change detection
- Only saves to EEPROM when changes are detected (reduces wear)

## Example Scenarios

### Scenario 1: Successful Initial Sync
```
0:00  - Boot
0:00  - requestSync() called in setup()
0:01  - Send C1 to Control Panel
0:02  - Receive ACK:C1=ok, then C3-C27
0:07  - Timeout (5s after last config), compare & save
0:07  - SyncFailed cleared (if was set)
5:07  - Next auto-sync (5 minutes later)
```

### Scenario 2: Control Panel Offline
```
0:00  - Boot
0:00  - requestSync() called
0:01  - Send C1 (no response)
0:06  - Timeout, no config received
0:06  - Raise SyncFailed warning
0:16  - Retry (10 seconds later, fast retry)
0:26  - Retry again
0:36  - Control Panel comes online
0:37  - Receive C3-C27, save, clear warning
5:37  - Back to 5 minute intervals
```

### Scenario 3: No Config Changes
```
0:00  - Sync triggered
0:01  - Send C1, receive C3-C27
0:06  - Timeout, compare configs
0:06  - No changes detected
0:06  - Clear SyncFailed (silent, no save needed)
```

## Future Enhancements
- Add retry count limit before giving up
- Implement exponential backoff for retries
- Add sync status LED indicator
- Log sync statistics (success/failure count, last sync time)
- Add selective config sync (only specific settings)
- Add config version numbers to detect staleness
