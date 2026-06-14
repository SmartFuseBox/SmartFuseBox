/*
 * PowerControlHub
 * Copyright (C) 2025 Simon Carter (s1cart3r@gmail.com)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
#pragma once

#include <Arduino.h>
#include "IWifiClient.h"

/**
 * @brief Wrapper that implements IWifiClient and emits HTTP/1.1 chunked transfer encoding
 * 
 * Buffers writes and automatically formats them as hex-sized chunks with proper CRLF delimiters.
 * Call finalize() to send the terminating zero-length chunk.
 */
class ChunkedWifiClient : public IWifiClient
{
private:
	IWifiClient* _client;
	static constexpr size_t CHUNK_BUFFER_SIZE = 64;
	uint8_t _buffer[CHUNK_BUFFER_SIZE];
	size_t _bufferPos;

	void flushBuffer()
	{
		if (_bufferPos == 0)
		{
			return;
		}

		char hexSize[16];
		snprintf(hexSize, sizeof(hexSize), "%X\r\n", _bufferPos);
		_client->print(hexSize);
		_client->write(_buffer, _bufferPos);
		_client->print(F("\r\n"));
		_bufferPos = 0;
	}

public:
	explicit ChunkedWifiClient(IWifiClient* client)
		: _client(client)
		, _bufferPos(0)
	{
	}

	void finalize()
	{
		flushBuffer();
		_client->print(F("0\r\n\r\n"));
	}

	bool connected() override
	{
		return _client->connected();
	}

	int available() override
	{
		return _client->available();
	}

	int read() override
	{
		return _client->read();
	}

	int peek() override
	{
		return _client->peek();
	}

	size_t write(const uint8_t* buf, size_t size) override
	{
		size_t written = 0;
		while (written < size)
		{
			size_t canWrite = CHUNK_BUFFER_SIZE - _bufferPos;
			size_t toWrite = (size - written < canWrite) ? (size - written) : canWrite;
			memcpy(_buffer + _bufferPos, buf + written, toWrite);
			_bufferPos += toWrite;
			written += toWrite;

			if (_bufferPos >= CHUNK_BUFFER_SIZE)
			{
				flushBuffer();
			}
		}
		return written;
	}

	size_t write(uint8_t b) override
	{
		return write(&b, 1);
	}

	void stop() override
	{
		_client->stop();
	}

	void flush() override
	{
		flushBuffer();
		_client->flush();
	}

	int connect(const char* host, uint16_t port) override
	{
		return _client->connect(host, port);
	}

	size_t print(const char* str) override
	{
		if (!str)
		{
			return 0;
		}
		return write(reinterpret_cast<const uint8_t*>(str), strlen(str));
	}

	size_t print(const __FlashStringHelper* str) override
	{
		PGM_P p = reinterpret_cast<PGM_P>(str);
		size_t n = 0;
		while (true)
		{
			unsigned char c = pgm_read_byte(p++);
			if (c == 0)
			{
				break;
			}
			if (write(c))
			{
				n++;
			}
			else
			{
				break;
			}
		}
		return n;
	}

	size_t print(char c) override
	{
		return write(static_cast<uint8_t>(c));
	}

	size_t print(unsigned char b, int base = DEC) override
	{
		char buf[16];
		if (base == HEX)
		{
			snprintf(buf, sizeof(buf), "%X", b);
		}
		else
		{
			snprintf(buf, sizeof(buf), "%u", b);
		}
		return print(buf);
	}

	size_t print(int n, int base = DEC) override
	{
		char buf[16];
		if (base == HEX)
		{
			snprintf(buf, sizeof(buf), "%X", n);
		}
		else
		{
			snprintf(buf, sizeof(buf), "%d", n);
		}
		return print(buf);
	}

	size_t print(unsigned int n, int base = DEC) override
	{
		char buf[16];
		if (base == HEX)
		{
			snprintf(buf, sizeof(buf), "%X", n);
		}
		else
		{
			snprintf(buf, sizeof(buf), "%u", n);
		}
		return print(buf);
	}

	size_t print(long n, int base = DEC) override
	{
		char buf[32];
		if (base == HEX)
		{
			snprintf(buf, sizeof(buf), "%lX", n);
		}
		else
		{
			snprintf(buf, sizeof(buf), "%ld", n);
		}
		return print(buf);
	}

	size_t print(unsigned long n, int base = DEC) override
	{
		char buf[32];
		if (base == HEX)
		{
			snprintf(buf, sizeof(buf), "%lX", n);
		}
		else
		{
			snprintf(buf, sizeof(buf), "%lu", n);
		}
		return print(buf);
	}

	size_t println(const char* str) override
	{
		size_t n = print(str);
		n += print(F("\r\n"));
		return n;
	}

	size_t println(const __FlashStringHelper* str) override
	{
		size_t n = print(str);
		n += print(F("\r\n"));
		return n;
	}

	size_t println(char c) override
	{
		size_t n = print(c);
		n += print(F("\r\n"));
		return n;
	}

	size_t println(unsigned char b, int base = DEC) override
	{
		size_t n = print(b, base);
		n += print(F("\r\n"));
		return n;
	}

	size_t println(int n, int base = DEC) override
	{
		size_t n2 = print(n, base);
		n2 += print(F("\r\n"));
		return n2;
	}

	size_t println(unsigned int n, int base = DEC) override
	{
		size_t n2 = print(n, base);
		n2 += print(F("\r\n"));
		return n2;
	}

	size_t println(long n, int base = DEC) override
	{
		size_t n2 = print(n, base);
		n2 += print(F("\r\n"));
		return n2;
	}

	size_t println(unsigned long n, int base = DEC) override
	{
		size_t n2 = print(n, base);
		n2 += print(F("\r\n"));
		return n2;
	}

	size_t println() override
	{
		return print(F("\r\n"));
	}

	operator bool() override
	{
		return _client && _client->connected();
	}
};
