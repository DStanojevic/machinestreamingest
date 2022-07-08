using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MachineDataApi.Configuration;
using MachineDataApi.Implementation;
using MachineDataApi.Implementation.Services;
using MachineDataApi.Implementation.WebSocketHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace MachineDataApi.Tests;

[TestFixture]
public class MachineStreamClientTests
{
    private MockRepository _mockRepository;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new MockRepository(MockBehavior.Default);
    }


    [Test]
    public async Task HappyCaseTest()
    {
        #region Arrange
        var appConfig = new ApplicationConfiguration
        {
            MachineStreamEndPointUrl = "ws://machinestream.herokuapp.com/ws"
        };
        var webSocketMock = _mockRepository.Create<IWebSocketWrapper>();
        var machineServiceMock = _mockRepository.Create<IMachineDataService>();
        var loggerMock = _mockRepository.Create<ILogger<MachineStreamClient>>();
        var messageBytes = new byte[] {1, 2, 3, 4};

        webSocketMock.Setup(p => p.State).Returns(WebSocketState.None);
        webSocketMock.Setup(p => p.ReadMessage(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(1);
                return new SuccessMessageResult(messageBytes);
            });
        machineServiceMock.Setup(p => p.SaveRawMessage(It.Is<byte[]>(m => m.Length == messageBytes.Length))).Returns(Task.CompletedTask);
        var machineStreamClient = new MachineStreamClient(appConfig, webSocketMock.Object, machineServiceMock.Object, loggerMock.Object);
        #endregion

        #region Act
        await machineStreamClient.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await machineStreamClient.StopAsync(CancellationToken.None);
        #endregion
        machineServiceMock.Verify(p => p.SaveRawMessage(It.Is<byte[]>(m => m.Length == messageBytes.Length)), Times.Once);
        _mockRepository.VerifyAll();
    }

    [TestCase(100, 505, 5)]
    [TestCase(100, 1010, 10)]
    public async Task MachineServiceCalledExactNumberOfTimes(int receiveMessageMillisecondsDelay, int totalMillisecondsDelay, int expectedCallCount)
    {
        #region Arrange
        var appConfig = new ApplicationConfiguration
        {
            MachineStreamEndPointUrl = "ws://machinestream.herokuapp.com/ws"
        };
        var webSocketMock = _mockRepository.Create<IWebSocketWrapper>();
        var machineServiceMock = _mockRepository.Create<IMachineDataService>();
        var loggerMock = _mockRepository.Create<ILogger<MachineStreamClient>>();

        webSocketMock.Setup(p => p.State).Returns(WebSocketState.None);
        webSocketMock.Setup(p => p.ReadMessage(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(receiveMessageMillisecondsDelay);
                return new SuccessMessageResult(new byte[] { 1, 2, 3, 4 });
            });
        machineServiceMock.Setup(p => p.SaveRawMessage(It.IsAny<byte[]>())).Returns(Task.CompletedTask);
        var machineStreamClient = new MachineStreamClient(appConfig, webSocketMock.Object, machineServiceMock.Object, loggerMock.Object);
        #endregion

        #region Act
        await machineStreamClient.StartAsync(CancellationToken.None);
        await Task.Delay(totalMillisecondsDelay);
        await machineStreamClient.StopAsync(CancellationToken.None);
        await Task.Delay(1000);
        #endregion

        #region Assert
        machineServiceMock.Verify(p => p.SaveRawMessage(It.IsAny<byte[]>()), Times.Exactly(expectedCallCount));
        _mockRepository.VerifyAll();
        #endregion
    }

    [Test]
    public async Task WhenIsLostedReconnectIsInvokedTest()
    {
        #region Arrange
        var appConfig = new ApplicationConfiguration
        {
            MachineStreamEndPointUrl = "ws://machinestream.herokuapp.com/ws"
        };
        var webSocketMock = _mockRepository.Create<IWebSocketWrapper>();
        var machineServiceMock = _mockRepository.Create<IMachineDataService>();
        var loggerMock = _mockRepository.Create<ILogger<MachineStreamClient>>();

        webSocketMock.Setup(p => p.State).Returns(WebSocketState.None);
        webSocketMock.Setup(p => p.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()));
        webSocketMock.Setup(p => p.ReadMessage(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(1);
                return new ConnectionLostMessageResult(WebSocketCloseStatus.InternalServerError,
                    "Remote machine returned and error.");
            });
        var machineStreamClient = new MachineStreamClient(appConfig, webSocketMock.Object, machineServiceMock.Object, loggerMock.Object);
        #endregion

        #region Act
        await machineStreamClient.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await machineStreamClient.StopAsync(CancellationToken.None);
        #endregion

        webSocketMock.Verify(p => p.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.VerifyAll();
    }
}