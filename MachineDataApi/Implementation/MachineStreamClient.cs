using System.Net.WebSockets;
using MachineDataApi.Configuration;
using MachineDataApi.Implementation.Services;
using MachineDataApi.Implementation.WebSocketHelpers;

namespace MachineDataApi.Implementation;

public interface IMachineStreamClient : IDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class MachineStreamClient : IMachineStreamClient
{
    #region Private fields
    private readonly string _webSocketEndpoint;
    private readonly IWebSocketWrapper _clientWebSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _ingestionActive;
    private readonly IMachineDataService _machineDataService;
    private readonly Dictionary<Type, Func<IMessageResult, CancellationToken, Task>> _socketMessageResultHandlers;
    private ILogger<MachineStreamClient> _logger;
    #endregion

    public MachineStreamClient(ApplicationConfiguration applicationConfiguration, IWebSocketWrapper clientWebSocket, IMachineDataService machineDataService, ILogger<MachineStreamClient> logger)
    {
        _webSocketEndpoint = applicationConfiguration.MachineStreamEndPointUrl;
        _clientWebSocket = clientWebSocket;
        _machineDataService = machineDataService;
        _logger = logger;
        _socketMessageResultHandlers =
            new Dictionary<Type, Func<IMessageResult, CancellationToken, Task>>()
            {
                {typeof(AbortedMessageResult), HandleAbortResult},
                {typeof(ConnectionLostMessageResult), HandleConnectionLostResult},
                {typeof(SuccessMessageResult), HandleSuccessfulResult}
            };
    }

    #region Public methods
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting machine stream client.");
        if (_ingestionActive)
        {
            _logger.LogInformation("Machine stream client was already started.");
            return;
        }
        _cancellationTokenSource = new CancellationTokenSource();
        await Connect(cancellationToken);
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
    #endregion

    #region Private methods
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
            var messageResult = await _clientWebSocket.ReadMessage(cancellationToken);
            await _socketMessageResultHandlers[messageResult.GetType()].Invoke(messageResult, cancellationToken);
        }
    }

    #region Read socket result handlers
    private Task HandleAbortResult(IMessageResult messageResult, CancellationToken cancellationToken)
    {
        if (messageResult is not AbortedMessageResult)
            throw new InvalidOperationException($"Invalid message result of type {messageResult.GetType().FullName}.");

        _logger.LogWarning("Listening socket aborted.");

        return Task.CompletedTask;
    }

    private Task HandleConnectionLostResult(IMessageResult messageResult, CancellationToken cancellationToken)
    {
        var connectionLostResult = messageResult as ConnectionLostMessageResult;
        if (connectionLostResult == null)
            throw new InvalidOperationException($"Invalid message result of type {messageResult.GetType().FullName}.");

        _logger.LogWarning($"Connection to {_webSocketEndpoint} was closed unexpectedly. Close status {connectionLostResult.CloseStatus}, description {connectionLostResult.Description}.\n" +
                           "Message will be discarded. Reconnecting...");

        return Connect(cancellationToken, 1500);
    }

    private Task HandleSuccessfulResult(IMessageResult messageResult, CancellationToken cancellationToken)
    {
        var successMessageResult = messageResult as SuccessMessageResult;
        if (successMessageResult == null)
            throw new InvalidOperationException($"Invalid message result of type {messageResult.GetType().FullName}.");

        _logger.LogDebug($"Received message of {successMessageResult.MessageData.Length} bytes.");

        return _machineDataService.SaveRawMessage(successMessageResult.MessageData);
    }
    #endregion
    #endregion

    #region IDisposable implementation
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
    #endregion
}