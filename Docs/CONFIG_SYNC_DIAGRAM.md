# Config Sync - Visual Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CONFIG SYNC ARCHITECTURE                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐                              ┌──────────────────────┐
│   SMARTFUSEBOX       │                              │  BOAT CONTROL PANEL  │
│  (Fuse Box)          │                              │  (Control Panel)     │
└──────────────────────┘                              └──────────────────────┘

╔══════════════════════╗                              
║ ConfigSyncManager    ║                              
║                      ║                              
║ Timer: Every 5 min   ║                              
╚══════════════════════╝                              
         │                                             
         │ (1) Timer expires                          
         ▼                                             
  ┌─────────────────┐                                 
  │ Snapshot Config │ (memcpy current config)         
  └─────────────────┘                                 
         │                                             
         ▼                                             
  ═══════════════════════════════════════════════════>
         │          C1 (ConfigGetSettings)            
         │                                             │
         │                              ╔══════════════════════╗
         │                              ║ ConfigCommandHandler ║
         │                              ║                      ║
         │                              ║ Processes C1         ║
         │                              ╚══════════════════════╝
         │                                             │
         │                                             ▼
  <═══════════════════════════════════════════════════
         │              ACK:C1=ok                     
         │                                             
         ▼                                             
╔══════════════════════╗                              
║ AckCommandHandler    ║                              
║                      ║                              
║ Logs "Sync started"  ║                              
╚══════════════════════╝                              
         │                                             
  <═══════════════════════════════════════════════════
         │              C3:Sea Wolf                    
         │                                             
  <═══════════════════════════════════════════════════
         │              C4:0=Nav|Navigation            
         │                                             
  <═══════════════════════════════════════════════════
         │              C4:1=Lights|Deck Lights        
         │                                             
  <═══════════════════════════════════════════════════
         │              ... (more C4-C27 commands)     
         │                                             
         ▼                                             
╔══════════════════════╗                              
║ ConfigCommandHandler ║                              
║                      ║                              
║ 1. Update Config     ║  (for each C3-C27)          
║ 2. Notify SyncMgr    ║  (reset timeout)            
╚══════════════════════╝                              
         │                                             
         │                                             
         │ (2) No commands for 5 seconds (timeout)    
         ▼                                             
╔══════════════════════╗                              
║ ConfigSyncManager    ║                              
║                      ║                              
║ Sync Complete:       ║                              
║ 1. Compare snapshot  ║  (memcmp)                   
║ 2. If changed:       ║                              
║    └─> Save EEPROM   ║  (ConfigController->save())  
║ 3. Log result        ║                              
╚══════════════════════╝                              


═══════════════════════════════════════════════════════════════════════════════

                            TIMING DIAGRAM

Time    SmartFuseBox                        ControlPanel
────────────────────────────────────────────────────────────────────────────
0:00    [5 min timer expires]
        Snapshot config
        ───────────────> C1
                                            Receive C1
                                            Process request
0:01    <─────────────── ACK:C1=ok
        [Sync started]
        [Timeout: 5s]
                                            Send config data...
0:02    <─────────────── C3:Sea Wolf
        [Update config]
        [Reset timeout]
0:03    <─────────────── C4:0=Nav|Navigation
        [Update config]
        [Reset timeout]
0:04    <─────────────── C4:1=Lights|Deck Lights
        [Update config]
        [Reset timeout]
...     ... (more commands)
0:08    <─────────────── C19:3=4
        [Update config]
        [Reset timeout]
0:13    [5s timeout - no more commands]
        Compare snapshot vs current
        Config changed? Yes
        Save to EEPROM ✓
        [Sync complete]
        
5:13    [5 min timer expires again]
        ... repeat cycle ...
```
