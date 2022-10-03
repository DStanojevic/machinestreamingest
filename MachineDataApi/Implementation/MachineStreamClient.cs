using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.WebSockets;
using MachineDataApi.Configuration;
using MachineDataApi.Implementation.Services;
using MachineDataApi.Implementation.WebSocketHelpers;
using MachineDataApi.Instrumentation;
using OpenTelemetry;
using OpenTelemetry.Metrics;

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
    private readonly ActivitySource _activitySource; 
    private readonly ILogger<MachineStreamClient> _logger;
    #endregion

    public MachineStreamClient(
        ApplicationConfiguration applicationConfiguration, 
        IWebSocketWrapper clientWebSocket, 
        IMachineDataService machineDataService, 
        ILogger<MachineStreamClient> logger,
        ActivitySource activitySource
        )
    {
        _webSocketEndpoint = applicationConfiguration.MachineStreamEndPointUrl;
        _clientWebSocket = clientWebSocket;
        _machineDataService = machineDataService;
        _activitySource = activitySource;
        _logger = logger;
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
            if(_clientWebSocket.State == WebSocketState.Aborted)
            {
                await _clientWebSocket.ReconnectAsync(new Uri(_webSocketEndpoint), cancellationToken);
            }
            else
            {
                await _clientWebSocket.ConnectAsync(new Uri(_webSocketEndpoint), cancellationToken);
            }
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
            await HandleMessageResult(messageResult, cancellationToken);
        }
    }


    private async Task HandleMessageResult(IMessageResult messageResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("HandleMessageResult", ActivityKind.Consumer);
        InstrumentationConstants.ProcessedMessagesCounter.Add(1);
        switch (messageResult)
        {
            case SuccessMessageResult successMessageResult:
                activity?.SetTag("result", "success");
                await HandleMessageResult(successMessageResult, cancellationToken);
                break;
            case ConnectionLostMessageResult connectionLostMessageResult:
                activity?.SetTag("result", "connectionissue");
                await HandleMessageResult(connectionLostMessageResult, cancellationToken);
                break;
            case AbortedMessageResult abortedMessageResult:
                activity?.SetTag("result", "aborted");
                await HandleMessageResult(abortedMessageResult, cancellationToken);
                break;
            default:
                activity?.SetTag("result", "unknownerror");
                throw new NotSupportedException("Not supported message result type.");
        }
    }

    #region Read socket result handlers
    private Task HandleMessageResult(AbortedMessageResult messageResult, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Listening socket aborted.");

        return Task.CompletedTask;
    }

    private async Task HandleMessageResult(ConnectionLostMessageResult messageResult, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Connection to {_webSocketEndpoint} was closed unexpectedly. Close status {messageResult.CloseStatus}, web socket error: {messageResult.WebSocketError}, description {messageResult.Description}.\n" +
                           "Message will be discarded. Reconnecting...");

         await Connect(cancellationToken, 1500);
         await StartIngestingMessages(cancellationToken);
    }

    private Task HandleMessageResult(SuccessMessageResult messageResult, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Received message of {messageResult.MessageData.Length} bytes.");

        return _machineDataService.SaveRawMessage(messageResult.MessageData);
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