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
        CloseStatus = closeStatus;
        Description = description;
    }
    public WebSocketCloseStatus CloseStatus { get; }
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