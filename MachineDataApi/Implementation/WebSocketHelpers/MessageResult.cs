using System.Net.WebSockets;

namespace MachineDataApi.Implementation.WebSocketHelpers;

public interface IMessageResult
{
}

public class AbortedMessageResult : IMessageResult
{
}

public class ConnectionLostMessageResult : IMessageResult
{
    public ConnectionLostMessageResult(WebSocketCloseStatus closeStatus, string description)
    {
        CloseStatus = closeStatus.ToString();
        Description = description;
    }

    public ConnectionLostMessageResult(WebSocketError errorCode, string description)
    {
        WebSocketError = errorCode.ToString();
        Description = description;
    }
    public string CloseStatus { get; }

    public string WebSocketError { get; }
    public string Description { get; }
}

public class SuccessMessageResult : IMessageResult
{
    public SuccessMessageResult(byte[] messageData)
    {
        MessageData = messageData;
    }
    public byte[] MessageData { get; }
}