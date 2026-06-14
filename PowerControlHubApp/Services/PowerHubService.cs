using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using PowerControlHubApp.Models.Json;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Services;

/// <summary>
/// Thrown when the device responded but its payload could not be parsed.
/// Distinct from a network failure so callers can preserve the last known data.
/// </summary>
public class DeviceResponseException : Exception
{
    public DeviceResponseException(string message, Exception inner)
        : base(message, inner) { }
}

/// <summary>
/// Wraps all HTTP communication with the PowerControlHub ESP32 device.
/// Endpoints used:
///   GET  /api/index               — combined relays + sensors JSON
///   GET  /api/relay               — relay states only
///   GET  /api/relay/R3?{i}={0|1}  — set relay state (index i to on/off)
/// </summary>
public class PowerHubService
{

    private HttpClient _client;
    private SocketsHttpHandler _handler;
    private string _baseUrl = string.Empty;

    public string BaseUrl => _baseUrl;

    public void Configure(string ipAddress, int port)
    {
        _client?.Dispose();
        _handler?.Dispose();

        _baseUrl = $"http://{ipAddress}:{port}";
        _handler = CreateHandler();
        _client  = new HttpClient(_handler, disposeHandler: false)
        {
            Timeout     = TimeSpan.FromSeconds(SecondsTen),
            BaseAddress = new Uri(_baseUrl + ForwardSlash)
        };

        // Tell the firmware to keep the TCP socket open after each response.
        // WifiServer.cpp checks for both of these headers before granting a
        // persistent slot (MaxPersistentClients = 1, PersistentTimeoutMs = 30 s).
        _client.DefaultRequestHeaders.ConnectionClose = false;
        _client.DefaultRequestHeaders.TryAddWithoutValidation(UserAgentKey, UserAgentValue);
        _client.DefaultRequestHeaders.TryAddWithoutValidation(ConnectionTypeKey, ConnectionTypePersistent);
    }

    public bool IsConfigured => _client != null;

    /// <summary>
    /// Creates a <see cref="SocketsHttpHandler"/> that maintains a persistent
    /// TCP connection to the device.  A callback
    /// enables OS-level TCP keep-alive probes so the connection survives the
    /// idle gap between poll cycles without being silently dropped by the device
    /// or any intermediate NAT/router.
    /// </summary>
    private static SocketsHttpHandler CreateHandler()
    {
        var handler = new SocketsHttpHandler
        {
            // Must exceed the firmware's PersistentTimeoutMs (30 s) so the .NET pool
            // never races to close the socket before the device's keep-alive window expires.
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(SecondsSixty),
            PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
            ConnectTimeout = TimeSpan.FromSeconds(SecondsFive),
            MaxConnectionsPerServer = MaximumPermanentConnections,
            ConnectCallback = async (context, ct) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                    {
                        // Disable Nagle — send small command packets immediately
                        NoDelay = true
                    };

                    // Enable TCP keep-alive so the OS detects a silently-dropped link
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, SecondsTen);  // first probe after 10 s idle
                    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, SecondsFive);  // re-probe every 5 s
                    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, SecondsThree); // drop after 3 missed probes

                    await socket.ConnectAsync(context.DnsEndPoint, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
        };

        return handler;
    }

    /// <summary>
    /// Fetches relay and sensor data from GET /api/index.
    /// Returns (relays, sensors) or throws on network / parse failure.
    /// </summary>
    public async Task<IndexModel> GetDashboardDataAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        string json = await _client!.GetStringAsync(RouteApiIndex, ct);

        try
        {
            IndexModel result = JsonSerializer.Deserialize<IndexModel>(json, JsonOptions);

            for (int i = 0; i < result.Relays.Count; i++)
            {
                // Populate the RelayModel's IsOn property based on the bitfield in SystemModel.RelayState
                result.Relays[i].Index = i;
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new DeviceResponseException(
                $"Device returned invalid JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends GET /api/relay/R3?{index}={state} to toggle a relay.
    /// Returns true when the device acknowledges success.
    /// </summary>
    public async Task<bool> SetRelayStateAsync(int relayIndex, bool on, CancellationToken ct = default)
    {
        EnsureConfigured();

        int state = on ? 1 : 0;
        string url = $"api/relay/R3?{relayIndex}={state}";
        HttpResponseMessage response = await _client!.GetAsync(url, ct);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Renames relay at <paramref name="index"/> via POST /api/relay/R6.
    /// <paramref name="shortName"/> max 5 chars; <paramref name="longName"/> max 20 chars.
    /// </summary>
    public async Task<bool> RenameRelayAsync(int index, string shortName, string longName, CancellationToken ct = default)
    {
        EnsureConfigured();

        string value = string.IsNullOrWhiteSpace(longName) ? shortName : $"{shortName}|{longName}";
        string url = $"api/relay/R6?{index}={Uri.EscapeDataString(value)}";
        HttpResponseMessage response = await _client!.PostAsync(url, null, ct);
        return await IsSuccessResponseAsync(response, ct);
    }

    /// <summary>
    /// Sets the active button colour for relay at <paramref name="index"/> via POST /api/relay/R7.
    /// 0=Blue 1=Green 2=Orange 3=Purple 4=Red 5=Yellow 255=clear.
    /// </summary>
    public async Task<bool> SetRelayColorAsync(int index, int colorIndex, CancellationToken ct = default)
    {
        EnsureConfigured();

        string url = $"api/relay/R7?{index}={colorIndex}";
        HttpResponseMessage response = await _client!.PostAsync(url, null, ct);
        return await IsSuccessResponseAsync(response, ct);
    }

    /// <summary>
    /// Sets the power-on default state for relay at <paramref name="index"/> via POST /api/relay/R8.
    /// </summary>
    public async Task<bool> SetRelayDefaultStateAsync(int index, int defaultState, CancellationToken ct = default)
    {
        EnsureConfigured();

        string url = $"api/relay/R8?{index}={defaultState}";
        HttpResponseMessage response = await _client!.PostAsync(url, null, ct);
        return await IsSuccessResponseAsync(response, ct);
    }

    /// <summary>
    /// Links relay at <paramref name="index"/> to <paramref name="linkedIndex"/> via POST /api/relay/R9.
    /// Pass 255 to unlink.
    /// </summary>
    public async Task<bool> LinkRelayAsync(int index, int linkedIndex, CancellationToken ct = default)
    {
        EnsureConfigured();

        string url = $"api/relay/R9?{index}={linkedIndex}";
        HttpResponseMessage response = await _client!.PostAsync(url, null, ct);
        return await IsSuccessResponseAsync(response, ct);
    }

    /// <summary>
    /// Sets the action type for relay at <paramref name="index"/> via POST /api/relay/R10.
    /// 0=Default 1=Horn 2=NightRelay.
    /// </summary>
    public async Task<bool> SetRelayActionTypeAsync(int index, int actionType, CancellationToken ct = default)
    {
        EnsureConfigured();

        string url = $"api/relay/R10?{index}={actionType}";
        HttpResponseMessage response = await _client!.PostAsync(url, null, ct);
        return await IsSuccessResponseAsync(response, ct);
    }

    /// <summary>
    /// Sets the GPIO pin for relay at <paramref name="index"/> via POST /api/relay/R11.
    /// Pass 255 to disable.
    /// </summary>
    public async Task<bool> SetRelayPinAsync(int index, int pin, CancellationToken ct = default)
    {
        EnsureConfigured();

        string url = $"api/relay/R11?{index}={pin}";
        HttpResponseMessage response = await _client!.PostAsync(url, null, ct);
        return await IsSuccessResponseAsync(response, ct);
    }

    /// <summary>
    /// Persists all in-memory config to EEPROM via POST /api/config/C0.
    /// </summary>
    public async Task<bool> SaveSettingsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        HttpResponseMessage response = await _client!.PostAsync(RouteSaveConfig, null, ct);
        return response.IsSuccessStatusCode;
    }

    private static async Task<bool> IsSuccessResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            return false;

        string body = await response.Content.ReadAsStringAsync(ct);

        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty(ResultSuccess, out JsonElement s) && s.GetBoolean();
        }
        catch
        {
            return response.IsSuccessStatusCode;
        }
    }

    /// <summary>
    /// Queries the device OTA status via GET /api/system/F13.
    /// Returns null when OTA is not supported by this firmware build.
    /// </summary>
    public async Task<OtaStatusModel> GetOtaStatusAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureConfigured();

            HttpResponseMessage response = await _client!.GetAsync(RouteOtaUpdate, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            string body = await response.Content.ReadAsStringAsync(ct);
            using JsonDocument doc = JsonDocument.Parse(body);

            // Device returns {"error":…} when OTA is compiled out
            if (doc.RootElement.TryGetProperty(ErrorKey, out _))
                return null;

            return JsonSerializer.Deserialize<OtaStatusModel>(body, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Triggers an OTA check (and optionally install) via POST /api/system/F12.
    /// The device starts checking in the background; poll GetOtaStatusAsync for progress.
    /// </summary>
    public async Task<bool> TriggerOtaInstallAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            // F12 is a POST with no body; pass apply=1 so the device installs immediately
            HttpResponseMessage response = await _client!.PostAsync(
                RouteUpdateOta, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureConfigured()
    {
        if (_client == null)
            throw new InvalidOperationException(PowerHubNotConfigured);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
