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

                    if (sensorProp.Value.TryGetProperty(SensorTypeJsonKey, out var typeEl) &&
                        typeEl.ValueKind == JsonValueKind.Number)
                    {
                        sensorList.Add(new SensorsModel
                        {
                            Name = sensorName,
                            SensorType = (SensorType)typeEl.GetByte(),
                            ExtraFields = sensorProp.Value
                        });
                    }
                }

                result.SensorsList = sensorList;
            }

            return result;
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
