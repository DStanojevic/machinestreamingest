using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MachineDataApi.Configuration;
using MachineDataApi.Implementation;
using MachineDataApi.Implementation.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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

        webSocketMock.Setup(p => p.State).Returns(WebSocketState.None);
        webSocketMock.Verify();
        webSocketMock.Setup(p => p.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        webSocketMock.Setup(p => p.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(1000);
                return new WebSocketReceiveResult(0, WebSocketMessageType.Text, true);
            });
        #endregion

        #region Act
        var machineStreamClient = new MachineStreamClient(appConfig, webSocketMock.Object, machineServiceMock.Object, loggerMock.Object);

        await machineStreamClient.StartAsync(CancellationToken.None);
        await Task.Delay(1200);
        await machineStreamClient.StopAsync(CancellationToken.None);
        #endregion

        _mockRepository.VerifyAll();
    }

    [TestCase(100, 505, 5)]
    [TestCase(100, 1005, 10)]
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
        webSocketMock.Verify();
        webSocketMock.Setup(p => p.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        webSocketMock.Setup(p => p.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(receiveMessageMillisecondsDelay);
                return new WebSocketReceiveResult(10, WebSocketMessageType.Text, true);
            });
        #endregion

        #region Act
        var machineStreamClient = new MachineStreamClient(appConfig, webSocketMock.Object, machineServiceMock.Object, loggerMock.Object);

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
}