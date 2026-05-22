# PinGuard — Compile-Time Pin Validation

PinGuard is a framework-owned compile-time pin validation layer that prevents invalid GPIO assignments from crashing or silently corrupting hardware initialisation on ESP32 targets.

---

## Why It Exists

Several ESP32 GPIOs are permanently unusable for certain purposes:

- **Flash-reserved GPIOs** — wired internally to the SPI flash chip. Writing to them causes an immediate watchdog reset or flash corruption.
- **Input-only GPIOs** — silicon-level limitation; using them as outputs silently fails or hangs the peripheral driver (e.g. SPI).
- **Strapping / UART0 GPIOs** — held at specific logic levels during boot; driving them during initialisation can prevent the board from booting at all.

Without a guard these errors are invisible: the config command returns `ok`, the value is saved to EEPROM, and the board enters a watchdog reset loop on the next boot with no actionable serial output.

PinGuard catches these problems at the point the pin is *set*, not when the board crashes.

---

## How It Works

PinGuard is included automatically via `BoardConfig.h` — no include is needed in consumer files. The correct pin table is selected at compile time using the IDF-provided macros `CONFIG_IDF_TARGET_ESP32S3` and `CONFIG_IDF_TARGET_ESP32`, which are emitted by the Arduino-ESP32 toolchain based on the board selected in the IDE. No manual defines are required in `Local.h`.

When a guarded pin-setting command is received, the setter calls:

```cpp
PinGuardResult result = PinGuard::validate(pin, PinUse::SpiSck);
```

If the result is `HardBlocked` or `AdvisoryBlocked` the setter returns `ConfigResult::InvalidPin` / `RelayResult::InvalidPin`, the command handler sends back `ACK:<cmd>=Invalid pin:<params>`, and a serial debug message names the blocked pin and the reason.

Not all pin-setting commands call PinGuard — see [Guarded Commands](#guarded-commands) below for the exact coverage.

---

## Guarded Commands

PinGuard checks are applied at the setter level for the following commands only.

| Command | Description | Pins validated | `PinUse` |
|---|---|---|---|
| `C4` | SPI bus pins | SCK, MOSI | `SpiSck`, `SpiMosi` |
| `C4` | SPI bus pins | MISO | `SpiMiso` |
| `C6` | XpdzTone buzzer pin | pin | `Output` |
| `C8` | HW-479 RGB LED pins | R, G, B | `Output` |
| `C18` | RTC DS1302 pins | data, clock, reset | `Output` |
| `C32` | SD card chip-select pin | CS | `SpiCs` |
| `N3` | Nextion display RX pin | RX | `Input` |
| `N4` | Nextion display TX pin | TX | `Output` |
| `R11` | Relay output pin | pin | `Relay` |

### Unguarded pin setters

| Command | Description | Status |
|---|---|---|
| `S4` | Sensor pin slot assignment | No PinGuard check — any GPIO value is accepted |

---

## Validation Tiers

| Tier | Result | Meaning | Overrideable? |
|---|---|---|---|
| **Hard** | `HardBlocked` | Flash-reserved or input-only used as output — will crash or silently fail | No |
| **Advisory** | `AdvisoryBlocked` | Strapping or UART0 pin — risky but sometimes legitimate | Yes — via `F14` AllowAdvisory mode |
| **Safe** | `Safe` | No restriction | — |
| **Disabled** | `Disabled` | Pin == `0xFF` (`PinDisabled`) — not fitted, no check performed | — |

The advisory and bypass behaviour is controlled by the persistent `pinGuardFlags` system setting — see [PinGuard System Mode](#pinGuard-system-mode) below.

---

## Input-Only Pin Handling

Input-only GPIOs (e.g. ESP32 GPIO 34–39, ESP32-S3 GPIO 45–46) are handled as follows:

- When the requested `PinUse` **requires output** (see table below): the pin is **hard-blocked** regardless of PinGuard mode — this is a silicon-level limitation.
- When the requested `PinUse` **does not require output**: the advisory gate is bypassed and `validate()` returns `Safe`.

Input-only GPIOs are stored as `PinCategory::Advisory` in the pin table. The promotion to `HardBlocked` for output uses is applied inside `validate()` via the `_inputOnlyMin`/`_inputOnlyMax` range check. For non-output uses the pin is returned directly as `Safe` without consulting the advisory mode flag.

---

## Pin Use Types

| `PinUse` value | Requires output-capable GPIO? | Typical caller |
|---|---|---|
| `Output` | Yes | Generic digital output, buzzer |
| `Input` | No | Generic digital / analogue input |
| `SpiSck` | Yes | `C4` SPI clock |
| `SpiMosi` | Yes | `C4` SPI MOSI |
| `SpiMiso` | No | `C4` SPI MISO |
| `SpiCs` | Yes | `C32` SD card chip-select |
| `Relay` | Yes | `R11` relay pin |
| `Sensor` | No | Sensor signal pin |

---

## Board Pin Tables

### Classic ESP32 (NodeMCU-32S, DevKitC, and all other ESP32 boards)

Selected when `CONFIG_IDF_TARGET_ESP32` is defined by the toolchain.

#### Hard-blocked (all uses)

| GPIO | Reason |
|---|---|
| 6, 7, 8, 9, 10, 11 | Connected to internal SPI flash — any access causes immediate crash |

#### Hard-blocked for output uses only (safe for input)

| GPIO | Reason |
|---|---|
| 34, 35, 36, 37, 38, 39 | Input-only silicon — cannot drive output |

#### Advisory (all uses)

| GPIO | Reason |
|---|---|
| 0 | Strapping pin — held low during flash download mode |
| 1 | UART0 TX — used by serial debug output |
| 2 | Strapping pin — must be low at boot for flash download |
| 3 | UART0 RX — used by serial debug input |
| 5 | Strapping pin — controls SDIO slave timing |
| 12 | Strapping pin — selects flash voltage; held low at boot |
| 15 | Strapping pin — controls UART0 log output during boot |

#### Recommended safe output pins

14, 16, 17, 21, 22, 25, 26, 27, 32, 33

#### Recommended SPI assignment (NodeMCU-32S)

| Signal | GPIO |
|---|---|
| SCK | 18 |
| MOSI | 23 |
| MISO | 19 |
| CS | 5 *(advisory — prefer 15 or 27)* |

---

### ESP32-S3 Dev Module

Selected when `CONFIG_IDF_TARGET_ESP32S3` is defined by the toolchain.

#### Hard-blocked (all uses)

| GPIO | Reason |
|---|---|
| 26, 27, 28, 29, 30, 31, 32 | Connected to internal Octal flash/PSRAM — any access causes immediate crash |

#### Hard-blocked for output uses only (safe for input)

| GPIO | Reason |
|---|---|
| 45, 46 | Input-only silicon on S3 |

#### Advisory (all uses)

| GPIO | Reason |
|---|---|
| 0 | Strapping pin |
| 3 | UART0 RX |
| 19 | USB D− |
| 20 | USB D+ |
| 45 | Boot-mode strapping |
| 46 | Boot-mode strapping |

---

## Crash Loop Guard

PinGuard prevents *new* bad pins from being saved, but a board that already has invalid pins in EEPROM will enter a watchdog reset loop before any serial handler is registered.

The **crash loop guard** in `PowerControlHubApp::setup()` detects and breaks this cycle automatically:

1. `ConfigManager::incrementCrashCounter()` is called **immediately after `ConfigManager::load()`**, before any hardware initialisation. The counter is persisted to EEPROM so a watchdog reset cannot undo it.
2. If `crashCounter >= CrashCounterThreshold` (default **3**), the firmware:
   - Sends `ERR:Crash loop detected — resetting config to defaults` on the serial port.
   - Calls `ConfigManager::resetToDefaults()` and saves to EEPROM.
   - Raises the `DefaultConfigurationFuseBox` warning.
   - Continues booting with safe default config.
3. At the very end of `setup()`, after `SystemInitialized` is sent, `ConfigManager::resetCrashCounter()` is called. This only executes if the full initialisation sequence completed without a watchdog reset.

### Timeline example

| Boot | crashCounter after increment | Outcome |
|---|---|---|
| 1 | 1 | WDT reset during SPI init — counter never cleared |
| 2 | 2 | WDT reset again — counter never cleared |
| 3 | 3 ≥ threshold | Config reset to defaults, boot completes, counter cleared to 0 |

The threshold is defined in `ConfigManager.h`:

```cpp
constexpr uint8_t CrashCounterThreshold = 3;
```

---

## PinGuard System Mode

The PinGuard mode is a persistent bitmask stored in `SystemHeader::pinGuardFlags` and controlled via the `F14` serial command. It takes effect immediately at runtime without requiring a reboot and is also applied on every boot before any hardware initialisation.

### Mode flags

| Flag | Bit | `F14` param | Effect |
|---|---|---|---|
| `None` | `0x00` | — | Default strict mode — advisory pins are blocked |
| `AllowAdvisory` | `0x01` | `a=true` | Advisory (strapping/UART) pins are permitted |
| `Bypass` | `0x02` | `b=true` | All PinGuard checks are skipped — `validate()` always returns `Safe` |

> **Note:** If both `AllowAdvisory` and `Bypass` are set, `Bypass` takes precedence and `validate()` returns `Safe` immediately. Hard-blocked pins (flash-reserved) are never automatically safe — `Bypass` is an explicit opt-out and should only be used when the board design intentionally uses risky GPIOs.

### Command examples

```
F14                      # read current mode — returns a=0;b=0
F14:a=true               # allow advisory pins, bypass off
F14:a=false;b=false      # reset to strict mode (same as default)
F14:b=true               # bypass all checks
```

### Recovery from invalid saved pins

If a user saves invalid pins to EEPROM and the board enters a watchdog reset loop, the crash loop guard (see above) will reset the entire config — including `pinGuardFlags` — to safe defaults after three consecutive crashes. There is no need to use `Bypass` mode for recovery; the crash guard handles it automatically.

The user can also send `C2` (Config Reset) over serial to reset to defaults at any time.

---

## Adding a New Board Variant

1. Open `PowerControlHub/PinGuard.h`.
2. Add a new `#elif defined(CONFIG_IDF_TARGET_<variant>)` block before the existing `#elif defined(CONFIG_IDF_TARGET_ESP32)` fallback.
3. Populate `_pinTable[]`, `_pinTableSize`, `_inputOnlyMin`, and `_inputOnlyMax` for the new target.
4. No changes to `Local.h` or any consumer file are needed — the toolchain macro selects the table automatically.

---

## Related

- [`PinGuard.h`](../PowerControlHub/PinGuard.h) — implementation
- [`ConfigManager.h`](../PowerControlHub/ConfigManager.h) — `CrashCounterThreshold`, `incrementCrashCounter`, `resetCrashCounter`
- [`Docs/Commands.md`](Commands.md) — `C4`, `C6`, `C8`, `C18`, `C32`, `N3`, `N4`, `R11`, `S4` pin-setting commands
- [`Docs/Warnings.md`](Warnings.md) — `SpiPinConfigError`, `DefaultConfigurationFuseBox`
