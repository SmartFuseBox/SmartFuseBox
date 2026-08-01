using System.Security.Cryptography;
using System.Text;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Services;

/// <summary>
/// DelegatingHandler that adds device authentication headers to every outbound HTTP request.
/// Supports two modes (either or both may be active):
///   • API Key  — adds <c>X-API-Key</c> header
///   • HMAC     — adds <c>X-Auth-Timestamp</c> + <c>X-Auth-Signature</c> headers
///
/// Values are read from thread-safe shared state that can be updated at runtime via
/// <see cref="Configure"/> without disrupting the handler pipeline.
/// </summary>
public sealed class DeviceAuthHandler : DelegatingHandler
{
    // Thread-safe shared state
    private string _apiKey;
    private string _hmacKey;
    private readonly object _lock = new();

    public DeviceAuthHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _apiKey = string.Empty;
        _hmacKey = string.Empty;
    }

    /// <summary>Update auth credentials at runtime without replacing the handler.</summary>
    public void Configure(string apiKey, string hmacKey)
    {
        lock (_lock)
        {
            _apiKey = apiKey ?? string.Empty;
            _hmacKey = hmacKey ?? string.Empty;
        }
    }

    public bool HasCredentials
    {
        get
        {
            lock (_lock)
            {
                return _apiKey.Length > 0 || _hmacKey.Length > 0;
            }
        }
    }

    /// <summary>Current API key (thread-safe read).</summary>
    public string ApiKey
    {
        get
        {
            lock (_lock)
            {
                return _apiKey;
            }
        }
    }

    /// <summary>Current HMAC key (thread-safe read).</summary>
    public string HmacKey
    {
        get
        {
            lock (_lock)
            {
                return _hmacKey;
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string apiKey;
        string hmacKey;

        lock (_lock)
        {
            apiKey = _apiKey;
            hmacKey = _hmacKey;
        }

        // API Key auth (simplest)
        if (apiKey.Length > 0)
        {
            request.Headers.TryAddWithoutValidation(HeaderApiKey, apiKey);
        }

        // HMAC-SHA256 auth
        if (hmacKey.Length > 0)
        {
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // Build canonical signing input: timestamp + "\n" + METHOD + "\n" + path + "\n" + body
            string method = request.Method.Method;
            string path = request.RequestUri?.PathAndQuery ?? HmacSignSeparator;
            string body = string.Empty;

            if (request.Content is not null)
            {
                // Attempt to read body (must be buffered). For POST requests with
                // small payloads this is safe. For streaming bodies the signature
                // will use an empty body.
                try
                {
                    using var ms = new MemoryStream();
                    await request.Content.CopyToAsync(ms, cancellationToken);
                    ms.Position = 0;
                    body = Encoding.UTF8.GetString(ms.ToArray());

                    // Re-buffer the content so the inner handler can read it
                    var bufferred = new ByteArrayContent(ms.ToArray());
                    foreach (var h in request.Content.Headers)
                        bufferred.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    request.Content = bufferred;
                }
                catch
                {
                    body = string.Empty;
                }
            }

            string signInput = $"{timestamp}{HmacSignSeparator}{method}{HmacSignSeparator}{path}{HmacSignSeparator}{body}";

            byte[] keyBytes = Encoding.UTF8.GetBytes(hmacKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(signInput);

            using var hmacSha = new HMACSHA256(keyBytes);
            byte[] hash = hmacSha.ComputeHash(messageBytes);

            string signature = BitConverter.ToString(hash).Replace(HmacHexDash, string.Empty).ToLowerInvariant();

            request.Headers.TryAddWithoutValidation(HeaderAuthTimestamp, timestamp);
            request.Headers.TryAddWithoutValidation(HeaderAuthSignature, signature);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
