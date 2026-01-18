#pragma once

#include <stddef.h>

// Convert `input` into phonetic words placed into `outBuf` (null-terminated).
// `outBufSize` is the total size of `outBuf`. `separator` defaults to ", ".
// Returns number of bytes written (excluding terminating NUL).
size_t phoneticize(const char* input, char* outBuf, size_t outBufSize, const char* separator = ", ");