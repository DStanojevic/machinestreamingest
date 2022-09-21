using System.Net.WebSockets;

namespace MachineDataApi.Implementation.WebSocketHelpers;

public interface IWebSocketWrapper: IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task ReconnectAsync(Uri uri, CancellationToken cancellationToken);
    Task<IMessageResult> ReadMessage(CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
}

public class WebSocketWrapper : IWebSocketWrapper
{
    private ClientWebSocket _clientWebSocket = new();
    private readonly ILogger<WebSocketWrapper> _logger;

    public WebSocketWrapper(ILogger<WebSocketWrapper> logger)
    {
        _logger = logger;
    }

    public WebSocketState State => _clientWebSocket.State;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) =>
        _clientWebSocket.ConnectAsync(uri, cancellationToken);

    public async Task ReconnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        _clientWebSocket.Dispose();
        _clientWebSocket = new ClientWebSocket();
        await ConnectAsync(uri, cancellationToken);
    }

    public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        _clientWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);

    public async Task<IMessageResult> ReadMessage(CancellationToken cancellationToken)
    {
        var buffer = new ArraySegment<byte>(new byte[2048]);
        WebSocketReceiveResult? result = null;
        var totalBytesReceived = 0;
        while (result == null || !result.EndOfMessage)
        {
            _logger.LogDebug("Waiting for the data from the socket...");
            try
            {
                result = await _clientWebSocket.ReceiveAsync(buffer, cancellationToken);
            }
            catch(WebSocketException ex)
            {
                _logger.LogError(ex, "Error during receiving data from the socket.");
                return new ConnectionLostMessageResult(ex.WebSocketErrorCode, ex.Message);
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new AbortedMessageResult();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error during receiving data from the socket.");
                throw;
            }
            
            _logger.LogDebug($"Receieved {result.Count} bytes.");
            totalBytesReceived += result.Count;

            if (cancellationToken.IsCancellationRequested)
                return new AbortedMessageResult();

            if (result.CloseStatus != null)
            {
                return new ConnectionLostMessageResult(result.CloseStatus.Value, result.CloseStatusDescription);
            }
        }

        var messageData = new byte[totalBytesReceived];
        Array.Copy(buffer.Array, messageData, totalBytesReceived);

        return new SuccessMessageResult(messageData);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clientWebSocket.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~WebSocketWrapper()
    {
        Dispose(false);
    }
}