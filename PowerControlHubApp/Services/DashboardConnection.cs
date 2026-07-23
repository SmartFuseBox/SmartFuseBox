using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using static PowerControlHubApp.Internal.Constants;
namespace PowerControlHubApp.Services
{
    public class DashboardConnection : IDashboardConnection, IDisposable
    {
        private readonly HttpClient _client;
        private readonly SocketsHttpHandler _handler;
        private string _baseUrl;
        private bool _disposed;

        public string BaseUrl => _baseUrl;

        public DashboardConnection()
        {
            _handler = CreateHandler();

            _client = new HttpClient(_handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(SecondsTen)
            };

            _client.DefaultRequestHeaders.ConnectionClose = false;
            _client.DefaultRequestHeaders.TryAddWithoutValidation(UserAgentKey, UserAgentValue);
            _client.DefaultRequestHeaders.TryAddWithoutValidation(ConnectionTypeKey, ConnectionTypePersistent);
            _baseUrl = string.Empty;
        }

        public void Configure(string ipAddress, int port)
        {
            _baseUrl = $"http://{ipAddress}:{port}";
            _client.BaseAddress = new Uri(_baseUrl + ForwardSlash);
        }

        public bool IsConfigured => !string.IsNullOrEmpty(_baseUrl);

        public async Task<SystemPinsResponseModel> GetSystemPinsAsync(CancellationToken ct = default)
        {
            string json = await _client.GetStringAsync(RouteSystemPins, ct);
            return JsonSerializer.Deserialize<SystemPinsResponseModel>(json, JsonOptions);
        }

        public async Task<IndexModel> GetDashboardDataAsync(CancellationToken ct = default)
        {
            string json = await _client.GetStringAsync(RouteApiIndex, ct);
            IndexModel result = JsonSerializer.Deserialize<IndexModel>(json, JsonOptions);

            for (int i = 0; i < result.Relays.Count; i++)
            {
                result.Relays[i].Index = i;
            }

            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty(JsonSensors, out var sensorsElement) &&
                sensorsElement.ValueKind == JsonValueKind.Object)
            {
                var sensorList = new List<SensorsModel>();

                foreach (var sensorProp in sensorsElement.EnumerateObject())
                {
                    string sensorName = sensorProp.Name;

                    // The firmware emits both "idType" (sensor id: Dht11=1, Light=2, ...) and
                    // "type" (SensorType: Local=0, Remote=1). We must use idType for template selection.
                    if (sensorProp.Value.TryGetProperty(JsonSensorIdType, out var idTypeEl) &&
                        idTypeEl.ValueKind == JsonValueKind.Number)
                    {
                        byte idType = idTypeEl.GetByte();
                        int uid = 0;

                        if (sensorProp.Value.TryGetProperty(JsonSensorUid, out var uidEl) && uidEl.ValueKind == JsonValueKind.Number)
                        {
                            uid = uidEl.GetInt32();
                        }

                        // Clone the JsonElement so it is independent of the
                        // using (JsonDocument doc) block and can be read later
                        // by the computed telemetry properties on SensorsModel.
                        sensorList.Add(new SensorsModel
                        {
                            Name = sensorName,
                            Uid = uid,
                            IdType = idType,
                            SensorType = (SensorType)idType,
                            ExtraFields = sensorProp.Value.Clone()
                        });
                    }
                }

                result.SensorsList = sensorList;
            }

            return result;
        }

        public async Task<List<string>> GetWarningsAsync(CancellationToken ct = default)
        {
            string json = await _client.GetStringAsync(RouteWarnings, ct);
            WarningsListResponseModel resp = JsonSerializer.Deserialize<WarningsListResponseModel>(json, JsonOptions);
            return resp?.Warnings ?? [];
        }

        private static SocketsHttpHandler CreateHandler()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(SecondsSixty),
                PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
                ConnectTimeout = TimeSpan.FromSeconds(SecondsFive),
                MaxConnectionsPerServer = 1,

                ConnectCallback = async (context, ct) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, SecondsTen);
                    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, SecondsFive);
                    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, SecondsThree);
                    await socket.ConnectAsync(context.DnsEndPoint, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
            };

            return handler;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _client?.Dispose();
            _handler?.Dispose();
            _disposed = true;
        }
    }
}
