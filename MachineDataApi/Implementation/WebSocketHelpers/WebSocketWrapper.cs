using System.Net.WebSockets;

namespace MachineDataApi.Implementation.WebSocketHelpers;

public interface IWebSocketWrapper: IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task<IMessageResult> ReadMessage(CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
}

public class WebSocketWrapper : IWebSocketWrapper
{
    private readonly ClientWebSocket _clientWebSocket = new ClientWebSocket();

    public WebSocketState State => _clientWebSocket.State;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) =>
        _clientWebSocket.ConnectAsync(uri, cancellationToken);

    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        _clientWebSocket.ReceiveAsync(buffer, cancellationToken);

    public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        _clientWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);

    public async Task<IMessageResult> ReadMessage(CancellationToken cancellationToken)
    {
        var buffer = new ArraySegment<byte>(new byte[2048]);
        WebSocketReceiveResult result = null;
        var totalBytesReceived = 0;
        while (result == null || !result.EndOfMessage)
        {
            result = await _clientWebSocket.ReceiveAsync(buffer, cancellationToken);
            totalBytesReceived += result.Count;
            if (result.CloseStatus != null)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new AbortedMessageResult();
                return new ConnectionLostMessageResult(result.CloseStatus.Value, result.CloseStatusDescription);
            }
        }

        var messageData = new byte[totalBytesReceived];
        Array.Copy(buffer.Array, messageData, totalBytesReceived);
        return new SuccessMessageResult(messageData);
    }

    private void ReleaseUnmanagedResources()
    {
        _clientWebSocket.Dispose();
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
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