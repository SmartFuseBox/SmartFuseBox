using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PowerControlHubApp.Models;

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
    private HttpClient? _client;
    private string _baseUrl = string.Empty;

    public string BaseUrl => _baseUrl;

    // ── Connection ────────────────────────────────────────────────────────────

    public void Configure(string ipAddress, int port)
    {
        _client?.Dispose();
        _baseUrl = $"http://{ipAddress}:{port}";
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5),
            BaseAddress = new Uri(_baseUrl + "/")
        };
    }

    public bool IsConfigured => _client != null;

    // ── Data retrieval ────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches relay and sensor data from GET /api/index.
    /// Returns (relays, sensors) or throws on network / parse failure.
    /// </summary>
    public async Task<(List<RelayModel> Relays, List<SensorModel> Sensors)> GetDashboardDataAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        string json = await _client!.GetStringAsync("api/index", ct);

        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            var relays  = ParseRelays(root);
            var sensors = ParseSensors(root);

            return (relays, sensors);
        }
        catch (JsonException ex)
        {
            throw new DeviceResponseException(
                $"Device returned invalid JSON: {ex.Message}", ex);
        }
    }

    // ── Relay control ─────────────────────────────────────────────────────────

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

        if (!response.IsSuccessStatusCode)
            return false;

        string body = await response.Content.ReadAsStringAsync(ct);
        using JsonDocument doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("success", out JsonElement s) && s.GetBoolean();
    }

    // ── OTA / firmware update ─────────────────────────────────────────────────

    /// <summary>
    /// Queries the device OTA status via GET /api/system/F13.
    /// Returns null when OTA is not supported by this firmware build.
    /// </summary>
    public async Task<OtaStatusModel?> GetOtaStatusAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureConfigured();

            HttpResponseMessage response = await _client!.GetAsync("api/system/F13", ct);
            if (!response.IsSuccessStatusCode)
                return null;

            string body = await response.Content.ReadAsStringAsync(ct);
            using JsonDocument doc = JsonDocument.Parse(body);

            // Device returns {"error":…} when OTA is compiled out
            if (doc.RootElement.TryGetProperty("error", out _))
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
                "api/system/F12?apply=1", null, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private static List<RelayModel> ParseRelays(JsonElement root)
    {
        var list = new List<RelayModel>();

        if (!root.TryGetProperty("relays", out JsonElement relaysEl) ||
            relaysEl.ValueKind != JsonValueKind.Array)
            return list;

        int index = 0;
        foreach (JsonElement el in relaysEl.EnumerateArray())
        {
            try
            {
                var relay = el.Deserialize<RelayModel>(JsonOptions) ?? new RelayModel();
                relay.Index = index;
                list.Add(relay);
            }
            catch
            {
                // skip malformed entry
            }
            index++;
        }

        return list;
    }

    private static List<SensorModel> ParseSensors(JsonElement root)
    {
        var list = new List<SensorModel>();

        if (!root.TryGetProperty("sensors", out JsonElement sensorsEl) ||
            sensorsEl.ValueKind != JsonValueKind.Object)
            return list;

        foreach (JsonProperty prop in sensorsEl.EnumerateObject())
        {
            try
            {
                var sensor = prop.Value.Deserialize<SensorModel>(JsonOptions) ?? new SensorModel();
                sensor.Name = prop.Name;
                list.Add(sensor);
            }
            catch
            {
                // skip malformed entry
            }
        }

        return list;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (_client == null)
            throw new InvalidOperationException("PowerHubService is not configured. Call Configure() first.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
