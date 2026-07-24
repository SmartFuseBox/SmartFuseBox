using Microsoft.Extensions.Logging;
using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using static PowerControlHubApp.Internal.Constants;
namespace PowerControlHubApp.Services
{
    public class ConfigConnection : IConfigConnection, IDisposable
    {
        private readonly HttpClient _client;
        private readonly SocketsHttpHandler _handler;
        private readonly Channel<ConfigCommand> _queue;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<ConfigConnection> _log;
        private readonly CancellationTokenSource _cts;
        private readonly Task _consumerTask;
        private string _baseUrl;
        private bool _disposed;

        public ConfigConnection(IMessageBus messageBus, ILogger<ConfigConnection> log)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _handler = CreateHandler();

            _client = new HttpClient(_handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(SecondsTen)
            };

            _client.DefaultRequestHeaders.ConnectionClose = false;
            _client.DefaultRequestHeaders.TryAddWithoutValidation(UserAgentKey, UserAgentValue);
            _client.DefaultRequestHeaders.TryAddWithoutValidation(ConnectionTypeKey, ConnectionTypePersistent);

            _queue = Channel.CreateBounded<ConfigCommand>(new BoundedChannelOptions(ConfigConnectionQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            _cts = new CancellationTokenSource();
            _baseUrl = string.Empty;
            _consumerTask = Task.Run(() => ConsumeQueueAsync(_cts.Token));
        }

        public void Configure(string ipAddress, int port)
        {
            _baseUrl = $"http://{ipAddress}:{port}";
            _client.BaseAddress = new Uri(_baseUrl + ForwardSlash);
        }

        public bool IsConfigured => !string.IsNullOrEmpty(_baseUrl);

        public bool IsQueueEmpty => _queue.Reader.Count == 0;

        public int QueueLength => _queue.Reader.Count;

        public void EnqueueCommand(ConfigCommand command)
        {
            _queue.Writer.TryWrite(command);
        }

        private async Task ConsumeQueueAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    ConfigCommand command = await _queue.Reader.ReadAsync(ct);
                    _log.LogDebug(LogConfigExecuting, command.Description);
                    bool success = await command.ExecuteAsync(_client, ct);

                    if (success)
                    {
                        _log.LogDebug(LogConfigSucceeded, command.Description);
                        TryPublishSuccess(command);
                    }
                    else
                    {
                        _log.LogWarning(LogConfigFailed, command.Description);
                        TryPublishFailure(command, LogConfigFailureResponse);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, LogConfigConsumerError);
                }
            }
        }

        private void TryPublishSuccess(ConfigCommand command)
        {
            try
            {
                if (command.SuccessMessageType == ConfigSuccessRelay && command.Context is int relayIndex)
                {
                    _messageBus.Publish(new RelayConfigSucceeded(relayIndex));
                }
                else if (command.SuccessMessageType == ConfigSuccessSensor && command.Context is Tuple<int, string> sensorCtx)
                {
                    _messageBus.Publish(new SensorConfigSucceeded(sensorCtx.Item1, sensorCtx.Item2));
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, LogPublishSuccessFailed, command.Description);
            }
        }

        private void TryPublishFailure(ConfigCommand command, string error)
        {
            try
            {
                _messageBus.Publish(new ConfigCommandFailed(command.Description, error));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, LogPublishFailureFailed, command.Description);
            }
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

        public async Task<List<SensorTypeDescriptorModel>> GetSensorMetaAsync(CancellationToken ct = default)
        {
            string json = await _client.GetStringAsync($"{RouteLocalSensorConfig}{SensorConfigGetAll}?meta=1", ct);
            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty(JsonMeta, out var meta) &&
                meta.TryGetProperty(JsonMetaDescriptors, out var descriptors) &&
                descriptors.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<SensorTypeDescriptorModel>>(descriptors.GetRawText(), JsonOptions) ?? [];
            }

            return [];
        }

        public async Task<List<ExternalSensorConfigModel>> GetExternalSensorsAsync(CancellationToken ct = default)
        {
            string json = await _client.GetStringAsync(RouteExternalSensor, ct);
            using JsonDocument doc = JsonDocument.Parse(json);

            var list = new List<ExternalSensorConfigModel>();

            if (doc.RootElement.TryGetProperty(JsonSensors, out var sensors) && sensors.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in sensors.EnumerateArray())
                {
                    var m = new ExternalSensorConfigModel
                    {
                        Index = el.GetProperty(JsonSensorIndex).GetInt32(),
                        SensorId = el.GetProperty(JsonSensorId).GetInt32(),
                        Name = el.GetProperty(JsonSensorName).GetString() ?? string.Empty,
                        MqttName = el.GetProperty(JsonSensorMqttName).GetString() ?? string.Empty,
                        MqttSlug = el.GetProperty(JsonSensorMqttSlug).GetString() ?? string.Empty,
                        MqttTypeSlug = el.GetProperty(JsonSensorMqttType).GetString() ?? string.Empty,
                        MqttDeviceClass = el.GetProperty(JsonSensorMqttDeviceClass).GetString() ?? string.Empty,
                        MqttUnit = el.GetProperty(JsonSensorMqttUnit).GetString() ?? string.Empty,
                        MqttIsBinary = el.GetProperty(JsonSensorMqttBinary).GetBoolean()
                    };

                    list.Add(m);
                }
            }

            return list;
        }

        public async Task<bool> SetExternalSensorCoreAsync(int index, int sensorId, string name, string mqttName, string mqttSlug, CancellationToken ct = default)
        {
            string url = $"api/externalsensor/E1?i={index}&id={sensorId}&n={Uri.EscapeDataString(name)}&mn={Uri.EscapeDataString(mqttName)}&ms={Uri.EscapeDataString(mqttSlug)}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetExternalSensorMqttAsync(int index, string typeSlug, string deviceClass, string unit, bool isBinary, CancellationToken ct = default)
        {
            string url = $"api/externalsensor/E2?i={index}&mt={Uri.EscapeDataString(typeSlug)}&md={Uri.EscapeDataString(deviceClass)}&mu={Uri.EscapeDataString(unit)}&bin={(isBinary ? 1 : 0)}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> RemoveExternalSensorAsync(int index, CancellationToken ct = default)
        {
            string url = $"api/externalsensor/E3?v={index}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> RenameExternalSensorAsync(int index, string name, CancellationToken ct = default)
        {
            string url = $"api/externalsensor/E4?{index}={Uri.EscapeDataString(name)}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<List<LocalSensorConfigModel>> GetLocalSensorsAsync(CancellationToken ct = default)
        {
            string json = await _client.GetStringAsync(RouteLocalSensorConfig, ct);
            using JsonDocument doc = JsonDocument.Parse(json);

            var list = new List<LocalSensorConfigModel>();

            if (doc.RootElement.TryGetProperty(JsonSensors, out var sensors) && sensors.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in sensors.EnumerateArray())
                {
                    var m = new LocalSensorConfigModel
                    {
                        Index = el.GetProperty(JsonLocalSensorIndex).GetInt32(),
                        SensorType = el.GetProperty(JsonLocalSensorType).GetInt32(),
                        Name = el.GetProperty(JsonLocalSensorName).GetString() ?? string.Empty,
                        Pin0 = el.GetProperty(JsonLocalSensorPin0).GetInt32(),
                        Pin1 = el.GetProperty(JsonLocalSensorPin1).GetInt32(),
                        Opt1_0 = el.GetProperty(JsonLocalSensorOpt1_0).GetInt32(),
                        Opt1_1 = el.GetProperty(JsonLocalSensorOpt1_1).GetInt32(),
                        Opt2_0 = el.GetProperty(JsonLocalSensorOpt2_0).GetInt32(),
                        Opt2_1 = el.GetProperty(JsonLocalSensorOpt2_1).GetInt32(),
                        Enabled = el.GetProperty(JsonLocalSensorEnabled).GetInt32() != 0
                    };

                    list.Add(m);
                }
            }

            return list;
        }

        public async Task<bool> AddUpdateLocalSensorAsync(int index, int type, sbyte opt0, sbyte opt1, CancellationToken ct = default)
        {
            string url = $"api/sensor/S1?i={index}&t={type}&o0={opt0}&o1={opt1}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> RemoveLocalSensorAsync(int index, CancellationToken ct = default)
        {
            string url = $"api/sensor/S2?v={index}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> RenameLocalSensorAsync(int index, string name, CancellationToken ct = default)
        {
            string url = $"api/sensor/S3?{index}={Uri.EscapeDataString(name)}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetLocalSensorPinAsync(int index, int slot, byte pin, CancellationToken ct = default)
        {
            string url = $"api/sensor/S4?i={index}&s={slot}&v={pin}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetLocalSensorEnabledAsync(int index, bool enabled, CancellationToken ct = default)
        {
            string url = $"api/sensor/S5?{index}={(enabled ? 1 : 0)}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetLocalSensorOptionAsync(int index, int slot, int group, int value, CancellationToken ct = default)
        {
            string url = $"api/sensor/S6?i={index}&s={slot}&o={group}&v={value}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetRelayStateAsync(int relayIndex, bool on, CancellationToken ct = default)
        {
            int state = on ? 1 : 0;
            string url = $"api/relay/R3?{relayIndex}={state}";
            HttpResponseMessage response = await _client.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RenameRelayAsync(int index, string shortName, string longName, CancellationToken ct = default)
        {
            string value = string.IsNullOrWhiteSpace(longName) ? shortName : $"{shortName}|{longName}";
            string url = $"api/relay/R6?{index}={Uri.EscapeDataString(value)}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetRelayColorAsync(int index, int colorIndex, CancellationToken ct = default)
        {
            int nextionId = colorIndex <= RelayColorYellow ? colorIndex + NextionImageIdMin : UnconfiguredPin;
            string url = $"api/relay/R7?{index}={nextionId}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetRelayDefaultStateAsync(int index, int defaultState, CancellationToken ct = default)
        {
            string url = $"api/relay/R8?{index}={defaultState}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> LinkRelayAsync(int index, int linkedIndex, CancellationToken ct = default)
        {
            string url = $"api/relay/R9?{index}={linkedIndex}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetRelayActionTypeAsync(int index, int actionType, CancellationToken ct = default)
        {
            string url = $"api/relay/R10?{index}={actionType}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SetRelayPinAsync(int index, int pin, CancellationToken ct = default)
        {
            string url = $"api/relay/R11?{index}={pin}";
            HttpResponseMessage response = await _client.PostAsync(url, null, ct);
            return await IsSuccessResponseAsync(response, ct);
        }

        public async Task<bool> SaveSettingsAsync(CancellationToken ct = default)
        {
            HttpResponseMessage response = await _client.PostAsync(RouteSaveConfig, null, ct);
            return response.IsSuccessStatusCode;
        }

        public async Task<OtaStatusModel> GetOtaStatusAsync(CancellationToken ct = default)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync(RouteOtaUpdate, ct);

                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync(ct);
                using JsonDocument doc = JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty(ErrorKey, out _))
                    return null;

                return JsonSerializer.Deserialize<OtaStatusModel>(body, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> TriggerOtaInstallAsync(CancellationToken ct = default)
        {
            try
            {
                HttpResponseMessage response = await _client.PostAsync(RouteUpdateOta, null, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DateTimeOffset?> GetDateTimeAsync(CancellationToken ct = default)
        {
            try
            {
                string json = await _client.GetStringAsync(RouteSystemGetDateTime, ct);
                using JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty(ResultSuccess, out var success) &&
                    success.GetBoolean() &&
                    doc.RootElement.TryGetProperty(JsonValueKey, out var v))
                {
                    string dtStr = v.GetString();

                    if (DateTimeOffset.TryParse(dtStr, out var dt))
                        return dt;
                }
            }
            catch
            {
                // fall through
            }
            return null;
        }

        public async Task<bool> SetDateTimeAsync(long unixTimestamp, CancellationToken ct = default)
        {
            try
            {
                string url = $"{RouteSystemSetDateTime}?{JsonValueKey}={unixTimestamp}";
                HttpResponseMessage response = await _client.PostAsync(url, null, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cts.Cancel();

            _consumerTask.ContinueWith(_ => { });

            _client?.Dispose();
            _handler?.Dispose();
            _cts.Dispose();
            _disposed = true;
        }
    }
}
