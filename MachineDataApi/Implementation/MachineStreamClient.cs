using System.Net.WebSockets;
using MachineDataApi.Configuration;
using MachineDataApi.Implementation.Services;

namespace MachineDataApi.Implementation;

public interface IMachineStreamClient : IDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class MachineStreamClient : IMachineStreamClient
{
    private readonly string _webSocketEndpoint;
    private readonly IWebSocketWrapper _clientWebSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _ingestionActive;
    private readonly IMachineDataService _machineDataService;
    private ILogger<MachineStreamClient> _logger;

    public MachineStreamClient(ApplicationConfiguration applicationConfiguration, IWebSocketWrapper clientWebSocket, IMachineDataService machineDataService, ILogger<MachineStreamClient> logger)
    {
        _webSocketEndpoint = applicationConfiguration.MachineStreamEndPointUrl;
        _clientWebSocket = clientWebSocket;
        _machineDataService = machineDataService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting machine stream client.");
        if (_ingestionActive)
        {
            _logger.LogInformation("Machine stream client was already started.");
            return;
        }
        await Connect(cancellationToken);
        _cancellationTokenSource = new CancellationTokenSource();
        _ = StartIngestingMessages(_cancellationTokenSource.Token);
        _ingestionActive = true;
        _logger.LogInformation("Machine stream client successfully started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping machine stream client.");
        if (!_ingestionActive)
        {
            _logger.LogInformation("Machine stream client is inactive.");
            return;
        }
        try
        {
            _cancellationTokenSource.Cancel(); 
            if (_clientWebSocket.State == WebSocketState.Connecting || _clientWebSocket.State == WebSocketState.Open)
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "Client stopped ingesting messages.", cancellationToken);
            _logger.LogInformation("Machine stream client successfully stopped.");
        }
        finally
        {
            _ingestionActive = false;
        }
    }

    private async Task Connect(CancellationToken cancellationToken, int millisecondsDelay = 0)
    {
        _logger.LogInformation($"Connecting to {_webSocketEndpoint}");
        if (millisecondsDelay > 0)
            await Task.Delay(millisecondsDelay, cancellationToken);

        if (_clientWebSocket.State == WebSocketState.Connecting || _clientWebSocket.State == WebSocketState.Open)
        {
            _logger.LogInformation($"Client is already connected to {_webSocketEndpoint}");
            return;
        }

        try
        {
            await _clientWebSocket.ConnectAsync(new Uri(_webSocketEndpoint), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to connect to web socket at {_webSocketEndpoint}");
            throw;
        }
        
        _logger.LogInformation($"Successfully connected to {_webSocketEndpoint}");
    }

    private async Task StartIngestingMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            var buffer = new ArraySegment<byte>(new byte[2048]);
            var totalBytesReceived = 0;
            do
            {
                result = await _clientWebSocket.ReceiveAsync(buffer, cancellationToken);
                totalBytesReceived += result.Count;
                if (result.CloseStatus != null)
                {
                    //Reset buffer
                    buffer = new ArraySegment<byte>();
                    _logger.LogWarning($"Connection to {_webSocketEndpoint} was closed unexpectedly. Close status {result.CloseStatus}, description {result.CloseStatusDescription}.\n"+
                                       "Message will be discarded. Reconnecting...");
                    await Connect(cancellationToken, 1500);
                    break;
                }
            } while (!result.EndOfMessage);

            if (result.EndOfMessage)
            {
                _logger.LogDebug($"Received message of {totalBytesReceived} bytes.");
                try
                {
                    var messageData = new byte[totalBytesReceived];
                    Array.Copy(buffer.Array, messageData, totalBytesReceived);
                    await _machineDataService.SaveRawMessage(messageData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save message");
                }
            }
        }
    }

    private void ReleaseUnmanagedResources()
    {
        _cancellationTokenSource?.Dispose();
        _clientWebSocket.Dispose();
        _ingestionActive = false;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MachineStreamClient()
    {
        ReleaseUnmanagedResources();
    }
}