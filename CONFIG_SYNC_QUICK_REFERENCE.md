# Config Sync - Quick Reference

## Summary
Automatic config synchronization between SmartFuseBox and Boat Control Panel with smart retry and warning system.

## Files Modified/Created

### New Files
1. **ConfigSyncManager.h** - Sync manager header
2. **ConfigSyncManager.cpp** - Sync manager implementation
3. **CONFIG_SYNC_README.md** - Full documentation
4. **SYNC_WARNING_IMPLEMENTATION.md** - Warning system details

### Modified Files
1. **AckCommandHandler.h** - Added FUSE_BOX_CONTROLLER support
2. **AckCommandHandler.cpp** - Process config ACKs
3. **ConfigCommandHandler.h** - Added sync manager integration
4. **ConfigCommandHandler.cpp** - Notify sync manager on config receive
5. **SmartFuseBox.ino** - Instantiate and integrate sync manager

## Quick Start

### 1. How It Works
```
SmartFuseBox ──C1──> Control Panel  (Request config)
              <─ACK── (Acknowledged)
              <─C3─── (Boat name)
              <─C4─── (Relay names x8)
              <─C5─── (Button mappings x4)
              ... more config commands ...
              
After 5s timeout:
  - Compare snapshot vs current
  - Save if changed
  - Clear/Raise SyncFailed warning
```

### 2. Sync Intervals
| State | Interval | Why |
|-------|----------|-----|
| **Normal** | 5 minutes | Reduce traffic |
| **Failing** | 10 seconds | Fast recovery |
| **First boot** | Immediate | requestSync() in setup() |

### 3. Warning States
| Warning | Meaning | Action |
|---------|---------|--------|
| **SyncFailed Active** | No response or save failed | Retry every 10s |
| **SyncFailed Cleared** | Sync successful | Normal 5min interval |

## Code Integration

### SmartFuseBox.ino Setup
```cpp
// Already added:
#include "ConfigSyncManager.h"

// Instantiate with warning manager
ConfigSyncManager configSyncManager(&commandMgrComputer, &commandMgrLink, 
                                   &configController, &warningManager);

// In setup():
ackHandler.setConfigSyncManager(&configSyncManager, &configController);
configHandler.setConfigSyncManager(&configSyncManager);
configSyncManager.requestSync(); // Immediate first sync

// In loop():
configSyncManager.update(now);
```

## Debug Messages

| Message | Meaning |
|---------|---------|
| `"Config sync started"` | Received ACK:C1=ok |
| `"Config synced and saved"` | Success, changes saved |
| `"Config sync failed - no response"` | Control Panel offline |
| `"Config sync failed to save"` | EEPROM write failed |

## Configuration

### Change Sync Interval
```cpp
// ConfigSyncManager.h
constexpr unsigned long DefaultConfigSyncIntervalMs = 300000; // Change this
constexpr unsigned long FastConfigSyncRetryMs = 10000;        // And this
```

### Manual Control
```cpp
configSyncManager.requestSync();        // Force immediate sync
configSyncManager.setEnabled(false);    // Disable auto-sync
configSyncManager.setEnabled(true);     // Re-enable
configSyncManager.setSyncInterval(600000); // Set to 10 minutes
```

## Testing Scenarios

### Test 1: Normal Operation
```
1. Both devices online
2. Modify config on Control Panel
3. Wait up to 5 minutes
4. Check: Config synced to SmartFuseBox
```

### Test 2: Control Panel Offline
```
1. Disconnect Control Panel
2. Observe retries every 10 seconds
3. Reconnect Control Panel
4. Check: Syncs within 10 seconds
```

### Test 3: Boot Sync
```
1. Modify config on Control Panel
2. Reboot SmartFuseBox
3. Check: Syncs immediately on boot
```

## Key Constants

```cpp
// Sync intervals
DefaultConfigSyncIntervalMs = 300000;  // 5 minutes
FastConfigSyncRetryMs = 10000;         // 10 seconds

// Timeout
SYNC_TIMEOUT_MS = 5000;                // 5 seconds

// Warning
WarningType::SyncFailed = 0x00000200   // Bit 9
```

## Commands

| Command | Direction | Purpose |
|---------|-----------|---------|
| **C1** | SFB → BCP | Request all config |
| **ACK:C1=ok** | BCP → SFB | Config request acknowledged |
| **C3-C27** | BCP → SFB | Individual config settings |

## Troubleshooting

### Sync Not Happening
- Check: `configSyncManager.isEnabled()` should return true
- Check: Warning manager is passed to constructor
- Check: `update(now)` is called in loop

### Constant Retries
- Check: Control Panel is online and responding
- Check: Serial link between devices is working
- Monitor: Debug messages for specific error

### Config Not Saving
- Check: EEPROM is not full
- Check: Config actually changed (memcmp)
- Monitor: `"Config sync failed to save"` message

## Architecture Summary

```
┌─────────────────────────────────────────────────────────┐
│                    ConfigSyncManager                     │
│                                                          │
│  - Timer management (5min normal, 10s retry)            │
│  - Config snapshot & comparison                         │
│  - Warning management (SyncFailed)                      │
│  - EEPROM save coordination                             │
└─────────────────────────────────────────────────────────┘
                     │                    │
         ┌───────────┴───────────┐       │
         ▼                       ▼       ▼
┌──────────────────┐   ┌──────────────────┐
│ AckCommandHandler│   │ConfigCommandHandler│
│                  │   │                    │
│ - Process ACK:C1 │   │ - Receive C3-C27   │
│ - Log sync start │   │ - Update Config    │
└──────────────────┘   │ - Notify sync mgr  │
                       └──────────────────┘
```

## Benefits

✅ **Automatic synchronization** - No manual intervention  
✅ **Smart retry** - Fast recovery when failing  
✅ **Change detection** - Only saves when needed  
✅ **Warning integration** - Visual feedback of sync status  
✅ **EEPROM wear reduction** - Memcmp before save  
✅ **Configurable intervals** - Tune for your needs  

## Next Steps After Crash Recovery

1. ✅ Files recreated
2. ✅ Documentation restored
3. **Build the project** - Verify compilation
4. **Test sync** - Check normal operation
5. **Test failure** - Disconnect and verify retry
6. **Commit changes** - Don't lose work again! 😊

```bash
# Commit your work!
git add .
git commit -m "feat: Add config synchronization with smart retry and warning system"
git push origin feature/BoatSensorHousing
```
