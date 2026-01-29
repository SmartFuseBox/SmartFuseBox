#pragma once

#include <Arduino.h>
#include <SerialCommandManager.h>
#include "ConfigController.h"
#include "Config.h"
#include "WarningManager.h"

constexpr unsigned long DefaultConfigSyncIntervalMs = 300000; // 5 minutes (normal)
constexpr unsigned long FastConfigSyncRetryMs = 10000;        // 10 seconds (retry on failure)

// ConfigSyncManager handles periodic synchronization of config settings
// from the control panel to the fuse box via C1 (ConfigGetSettings) requests
class ConfigSyncManager
{
private:
	static constexpr unsigned long SYNC_TIMEOUT_MS = 5000;
	SerialCommandManager* _computerSerial;
	SerialCommandManager* _linkSerial;
	ConfigController* _configController;
	WarningManager* _warningManager;
	unsigned long _syncInterval;
	unsigned long _retryInterval;
	unsigned long _lastSyncRequest;
	unsigned long _lastConfigReceived;
	bool _enabled;
	bool _syncing;
	Config _configSnapshot;

	void sendSyncRequest();
	void completeSyncIfTimeout(unsigned long now);
	bool hasConfigChanged();
	void saveConfigIfChanged();
	unsigned long getCurrentSyncInterval() const; // Returns appropriate interval based on sync state

public:
	ConfigSyncManager(SerialCommandManager* commandMgrComputer, 
		SerialCommandManager* linkSerial, ConfigController* configController,
		WarningManager* warningManager,
		unsigned long syncInterval = DefaultConfigSyncIntervalMs,
		unsigned long retryInterval = FastConfigSyncRetryMs);

    void update(unsigned long now);

    void requestSync();

    void setEnabled(bool enabled);
    bool isEnabled() const { return _enabled; }

    void setSyncInterval(unsigned long interval);
    unsigned long getSyncInterval() const { return _syncInterval; }

    bool isSyncing() const { return _syncing; }

    void notifyConfigReceived();

};
