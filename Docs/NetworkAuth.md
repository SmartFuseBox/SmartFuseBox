# NetworkAuth — WiFi API Authentication

NetworkAuth is a dual-mode authentication layer that protects the device's HTTP API from unauthorised access over WiFi. It is disabled by default and must be explicitly enabled via the serial console, Bluetooth, or the MAUI application after initial configuration.

---

## Why It Exists

The ESP32 HTTP server exposes every control surface of the device — relay toggling, sensor data, pin configuration, MQTT credentials, and firmware updates — as REST endpoints at well-known paths. Without authentication:

- Any device on the same WiFi network can toggle relays, change pin assignments, or trigger OTA updates.
- The `/api/index` response includes the API key and HMAC key in cleartext, allowing an attacker to harvest credentials and impersonate the authorised MAUI client.
- Browser-based discovery (e.g. Chrome typing `192.168.4.1/api/index`) immediately leaks all device state and secrets.

NetworkAuth gates every HTTP request (including `/api/index`) behind credential verification. Requests lacking valid credentials receive `401 Unauthorized`.

---

## How It Works

### Server-Side Enforcement

Auth is enforced inside `WifiServer::processClientRequest()` in `WifiServer.cpp`. The check runs **before** any route handler is dispatched:

```cpp
if (_authConfig != nullptr && _authConfig->enabled)
{
    // parse X-API-Key, X-Auth-Timestamp, X-Auth-Signature headers
    // validate API key match OR HMAC signature
    if (!authorized)
    {
        sendResponse(401, …);
        cleanupClient(index);
        return;
    }
}
```

If `_authConfig` is `nullptr` or `enabled == false`, the check is skipped and all requests are serviced without credentials.

The `_authConfig` pointer is set once at startup from the global `Config` struct (stored in EEPROM) via `WifiServer::setAuthConfig()`. It points to the live in-memory config, so changes take effect immediately after a save (no reboot required).

### Credential Storage

Auth credentials live in `Config.h` as part of the persistent `Config` struct:

```cpp
struct NetworkAuthConfig {
    bool    enabled;                     // master switch
    uint8_t version;                    // reserved
    char    apiKey[32];                 // "ak-XXXX…" — 31 chars + null
    char    hmacKey[32];                // "hk-XXXX…" — 31 chars + null
    uint8_t reserved[4];
} __attribute__((packed));
```

Both keys are 31 characters max, null-terminated. The `version` and `reserved` fields are reserved for future use.

---

## Authentication Mechanisms

Two independent mechanisms are supported. Either one grants access.

### 1. API Key (Simple)

**Header:** `X-API-Key: ak-pch-1712A0`

The server performs an exact string comparison against `_authConfig->apiKey`. If they match, the request is authorised.

This is the simplest method. It requires no timestamp or body hashing and works even when the device RTC has not been set. It is less secure than HMAC because the key is transmitted verbatim on every request and could be captured from HTTP traffic (the connection is plain HTTP, not HTTPS).

### 2. HMAC-SHA256 (Strong)

**Headers:**

| Header | Value |
|---|---|
| `X-Auth-Timestamp` | Unix timestamp (seconds) of the signing moment |
| `X-Auth-Signature` | Lowercase hex-encoded HMAC-SHA256 signature |

**Signing input format** (canonical, one `\n`-separated line):

```
{timestamp}\n{METHOD}\n{path}\n{body}
```

Example signing input for `GET /api/index` with no body at timestamp `1746000000`:

```
1746000000
GET
/api/index

```

The signature is computed as `HMAC-SHA256(hmacKey, signInput)` and encoded as lowercase hex.

**Timestamp window:** ±300 seconds (5 minutes). Requests outside this window are rejected. This requires the device RTC to be set. If `DateTimeManager::isTimeSet()` returns false, HMAC verification is skipped and only API key auth can succeed.

**Device time synchronisation:** The ESP32 has no battery-backed RTC and loses time on power cycle. The device clock must be set before HMAC authentication will function. Command `F6` sets the system date/time:

```
F6;2026-07-15T14:30:00
```

The MAUI app sends `F6` automatically on first connection via the `TimeSyncService`, so HMAC works out of the box after pairing with the app. If connecting from a custom client or script, send `F6` with the current UTC time in ISO 8601 format before issuing HMAC-signed requests.

**HMAC key format:** `hk-XXXX…` — 31 characters. The key is the HMAC secret shared between client and server.

### Header Parsing

Headers are extracted from the raw HTTP request buffer by case-insensitive substring search. Both `X-API-Key:` and `x-api-key:` are recognised. Header values extend to the next `\r\n` or `\n`.

---

## Guarded Endpoints

When auth is enabled, **all** HTTP endpoints are guarded. This includes:

| Path | Method | Description |
|---|---|---|
| `/api/index` | GET | Full device state JSON (relays, sensors, config, auth keys) |
| `/api/relay/*` | GET/POST | Relay control and configuration |
| `/api/config/*` | GET/POST | Device configuration (pins, network, auth itself) |
| `/api/sensor/*` | GET/POST | Sensor configuration |
| `/api/sound/*` | GET/POST | Sound/signal control |
| `/api/system/*` | GET/POST | System commands, OTA, time |
| `/api/warning/*` | GET/POST | Warning manager |
| `/api/mqtt/*` | GET/POST | MQTT broker settings |
| `/api/scheduler/*` | GET/POST | Schedule management |
| `/api/externalsensor/*` | GET/POST | External sensor registration |

The built-in web UI (`/index` — HTML page) is **not** guarded by auth. However, its embedded JavaScript calls `fetch('/api/index')` every 5 seconds to refresh data. When auth is enabled, these fetch calls will receive `401` and the web UI will show stale data. This is by design — the web UI is a convenience dashboard for trusted local networks. For secure operation, use the MAUI application which sends proper auth headers.

---

## Configuration Interface

### Command `C19` — Network Authentication

Auth is configured via command `C19` on all transports (serial, Bluetooth, WiFi).

**Serial / Bluetooth format:**

```
C19                  — read current state
C19;e=1              — enable auth
C19;e=0              — disable auth
C19;k=ak-newkey      — set API key
C19;h=hk-newkey      — set HMAC key
C19;g=1              — auto-generate both keys
```

**WiFi HTTP format:**

```
GET  /api/config/C19                              — read current state
POST /api/config/C19?e=1                          — enable
POST /api/config/C19?k=ak-newkey                  — set API key
POST /api/config/C19?h=hk-newkey                  — set HMAC key
POST /api/config/C19?g=1                          — auto-generate keys
POST /api/config/C19?e=1&k=ak-X&h=hk-X            — combined
```

**Read response** (serial):

```
ACK:C19:e=1;k=ak-pch-1712A0;h=hk-sfb-1712A0
```

**Read response** (WiFi JSON):

```json
{"e":true,"k":"ak-pch-1712A0","h":"hk-sfb-1712A0"}
```

### Parameter Reference

| Param | Type | Description |
|---|---|---|
| `e` | bool (`0`/`1` or `true`/`false`) | Enable/disable authentication |
| `k` | string (≤31 chars) | API key value |
| `h` | string (≤31 chars) | HMAC secret key value |
| `g` | bool (`0`/`1` or `true`/`false`) | Generate new device-unique keys |

### Key Generation (`g=1`)

When `g=1` is sent, the device calls `ConfigController::generateAuthKeys()` which:

1. Generates a device-unique deterministic password via `SystemFunctions::GenerateDefaultPassword()`
2. Prefixes it with `ak-` for the API key and `hk-` for the HMAC key
3. Writes both into `config.auth.apiKey` and `config.auth.hmacKey`

The device ID used for generation is derived from hardware characteristics, making keys unique to each ESP32. Generated keys follow the pattern `ak-XXXX` / `hk-XXXX` where `XXXX` is the device-specific component.

### Validation Rules

When setting keys via `k=` or `h=`:

- **Null key:** Setting a key to an empty or null value **disables auth** (`enabled = false`). This is a safety measure — a blank key with auth enabled would lock out all clients.
- **Too long:** Keys exceeding 31 characters return `ConfigResult::TooLong` and auth is disabled.
- **Enable without keys:** Calling `e=1` when either key is empty returns `ConfigResult::InvalidParameter` — auth cannot be enabled until both keys are set.

---

## /api/index Auth Exposure

The `/api/index` response always includes the current auth configuration in its `config` section:

```json
{
  "config": {
    …
    "auth": {
      "enabled": true,
      "apiKey": "ak-pch-1712A0",
      "hmacKey": "hk-sfb-1712A0"
    }
  }
}
```

This is how the MAUI client discovers the device's auth keys on first connection. The flow:

1. User enters IP/port in the MAUI app (no auth keys known yet)
2. App calls `GET /api/index` — if auth is disabled, the response includes the keys
3. App reads the keys and stores them in local preferences
4. App calls `ConfigureAuth(apiKey, hmacKey)` on both `DashboardConnection` and `ConfigConnection`
5. Future requests carry auth headers

When auth is already enabled and the MAUI app has stored keys from a previous session, the app loads them from preferences at startup and configures the auth handler before the first request.

---

## Client-Side Implementation (.NET MAUI)

### DeviceAuthHandler

`DeviceAuthHandler` is a `DelegatingHandler` that sits in the `HttpClient` pipeline for both `DashboardConnection` and `ConfigConnection`. It intercepts every outbound request in `SendAsync()` and injects auth headers.

```csharp
// In SendAsync():
if (apiKey.Length > 0)
    request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);

if (hmacKey.Length > 0)
{
    string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    string signInput = $"{timestamp}\n{method}\n{path}\n{body}";
    byte[] hash = HMACSHA256(hmacKeyBytes, signInputBytes);
    string signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    request.Headers.TryAddWithoutValidation("X-Auth-Timestamp", timestamp);
    request.Headers.TryAddWithoutValidation("X-Auth-Signature", signature);
}
```

Both mechanisms are applied simultaneously when both keys are configured. The server checks API key first (fast path), then falls back to HMAC.

Key design decisions in the handler:

- **Thread-safe state:** API key and HMAC key are stored behind a `lock` and can be updated at runtime via `Configure()` without disrupting in-flight requests.
- **Body buffering:** For POST requests, the handler reads and buffers the request body to compute the HMAC signature, then rewinds it with a `ByteArrayContent` so the inner handler can still read it. This is safe for the small payloads (typically <256 bytes) used by the MAUI app.
- **Streaming fallback:** If body buffering fails, the signature is computed with an empty body — the server will reject the HMAC, but API key auth may still succeed.

### Auth Sync Flow

When the MAUI app changes auth settings (enable/disable, set keys, generate keys), the `ConfigConnection` automatically re-syncs the handler:

```
SetAuthEnabledAsync() ──┐
SetAuthApiKeyAsync()  ──┼──► POST /api/config/C19
SetAuthHmacKeyAsync() ──┤          │
GenerateAuthKeysAsync()──┘          ▼
                         SyncAuthHandlerFromDeviceAsync()
                                    │
                         GET /api/config/C19
                                    │
                         _authHandler.Configure(apiKey, hmacKey)
                                    │
                         _messageBus.Publish(AuthConfigChanged)
                                    │
                         PowerHubService.OnAuthConfigChanged()
                                    │
                         dc.ConfigureAuth() + cc.ConfigureAuth()
```

This ensures that after any auth change, both the config connection (which made the change) and the dashboard connection (which polls `/api/index`) have the updated credentials. Without this sync, the config change would succeed but subsequent requests would fail with `401` because the handler still had the old keys.

### AuthConfigChanged Message

A fire-and-forget message published whenever the device's auth configuration changes:

```csharp
public record AuthConfigChanged(AuthConfigModel Config);
```

`PowerHubService` subscribes to this message and propagates the new credentials to both `DashboardConnection` and `ConfigConnection`. This decouples the config-layer auth change from the dashboard-layer credential update — the `ConfigPoller` doesn't need to know about auth internals.

---

## MAUI UI — NetworkSecurityPage

The `NetworkSecurityPage` (accessible from Settings) provides a UI for managing auth:

- **Enable/Disable toggle** — switch controlling `config.auth.enabled`
- **API Key entry** — text field for the API key
- **HMAC Key entry** — text field for the HMAC key
- **Generate New Keys** button — sends `g=1` to the device, then refreshes the displayed keys
- **Save All Settings** button — writes all three fields (enabled, API key, HMAC key) and saves to EEPROM

The page is backed by `NetworkSecurityViewModel` which calls `PowerHubService` methods:

| UI Action | Service Method | HTTP Call |
|---|---|---|
| Toggle enable | `SetAuthEnabledAsync(bool)` | `POST /api/config/C19?e=0\|1` |
| Save API key | `SetAuthApiKeyAsync(string)` | `POST /api/config/C19?k=…` |
| Save HMAC key | `SetAuthHmacKeyAsync(string)` | `POST /api/config/C19?h=…` |
| Generate keys | `GenerateAuthKeysAsync()` | `POST /api/config/C19?g=1` |
| Refresh | `GetAuthConfigAsync()` | `GET /api/config/C19` |

---

## Connection Lifecycle

### First Connection (auth disabled)

```
MAUI App                    ESP32 WiFi Server
   │                              │
   │── GET /api/index ───────────►│  auth disabled, no check
   │◄─ {config:{auth:{…}}} ──────│  leaks apiKey, hmacKey
   │                              │
   │  App stores keys in prefs    │
   │  ConfigureAuth(apiKey,hmacKey)
   │                              │
   │── GET /api/index ───────────►│  auth disabled, but headers sent anyway
   │  X-API-Key: ak-…             │
```

### Auth Enabled After Configuration

```
MAUI App                    ESP32 WiFi Server
   │                              │
   │── POST /api/config/C19?e=1 ─►│  enable auth
   │◄─ {"success":true} ─────────│
   │                              │
   │  SyncAuthHandlerFromDevice() │
   │  (re-reads keys, updates handler)
   │                              │
   │── GET /api/index ───────────►│  auth check: X-API-Key matches ✓
   │  X-API-Key: ak-…             │
   │◄─ 200 OK ───────────────────│
```

### Unauthorised Browser (auth enabled)

```
Browser                     ESP32 WiFi Server
   │                              │
   │── GET /api/index ───────────►│  auth check: no headers → not authorized
   │◄─ 401 {"message":"Unauthorized"}
   │                              │
```

---

## Security Considerations

| Concern | Mitigation |
|---|---|
| Plain-text HTTP | API key is sent in cleartext. Use HMAC mode for sensitive deployments. The connection is local WiFi only — not exposed to the internet unless the user explicitly port-forwards. |
| Replay attacks | HMAC timestamp window (±300 s) limits replay. Timestamp monotonicity is not enforced, so a captured signature can be replayed within the window. |
| Key exposure in `/api/index` | Keys are always present in the index JSON. When auth is enabled, reading `/api/index` requires a valid key — solving the chicken-and-egg problem for initial setup. Users should disable auth, read keys once, then enable auth. |
| Web UI breakage | The built-in HTML dashboard (`/index`) stops updating when auth is enabled because its JavaScript cannot send auth headers. This is by design. |
| EEPROM wear | Auth keys are stored in EEPROM as part of the `Config` struct. Frequent key regeneration should be avoided — keys are intended to be set once and rarely changed. |

---

## Migration from Pre-Auth Firmware

1. **Flash updated firmware** — auth defaults to disabled (`enabled == false`).
2. **Open NetworkSecurityPage** in the MAUI app and tap **Generate New Keys**.
3. Note the generated keys (they appear in the text fields).
4. Toggle **Enable Authentication** on.
5. Tap **Save All Settings**.
6. The MAUI app automatically syncs the new keys into its auth handler pipeline.
7. Verify by opening `http://<device-ip>/api/index` in a browser — you should receive `401 Unauthorized`.
