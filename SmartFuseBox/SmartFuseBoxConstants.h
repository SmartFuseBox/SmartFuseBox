#pragma once 

#include <stdint.h>

constexpr uint16_t DefaultSoundStartDelayMs = 500;

// COLREGS Sound Signal Durations
constexpr uint16_t SoundBlastShortMs = 1000;  // ~1 second (COLREGS Rule 34)
constexpr uint16_t SoundBlastLongMs = 5000;   // 4-6 seconds (COLREGS Rule 34)
constexpr uint16_t SoundBlastGapMs = 1500;    // Gap between blasts in COLREGS sequences

// SOS-specific durations (Morse code timing for electronic signal)
constexpr uint16_t MorseCodeShortMs = 500;         // Dot duration
constexpr uint16_t MorseCodeLongMs = 1000;          // Dash duration (3x dot)
constexpr uint16_t MorseCodeGapMs = 400;            // Gap between dots/dashes

// COLREGS Repeat Intervals
constexpr uint32_t FogRepeatMs = 120000;  // 2 minutes (COLREGS Rule 35)
constexpr uint32_t SosRepeatMs = 10000;   // 10 seconds (distress signal)
constexpr uint32_t NoRepeat = 0;           // One-shot signals
