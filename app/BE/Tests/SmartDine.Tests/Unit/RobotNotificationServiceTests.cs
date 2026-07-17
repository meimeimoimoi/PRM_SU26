extern alias OrderApi;

using Moq;
using Microsoft.AspNetCore.SignalR;
using OrderApi::SmartDine.Order.API.Services;
using OrderApi::SmartDine.Order.API.Hubs;

namespace SmartDine.Tests.Unit;

public class RobotNotificationServiceTests
{
    private readonly Mock<IHubContext<RobotHub>> _hubMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly RobotNotificationService _sut;

    public RobotNotificationServiceTests()
    {
        _hubMock.Setup(h => h.Clients.Group("RobotGroup")).Returns(_clientProxyMock.Object);
        _sut = new RobotNotificationService(_hubMock.Object);
    }

    [Fact]
    public async Task SendCommandToRobotAsync_ShouldSendToRobotGroup()
    {
        await _sut.SendCommandToRobotAsync("NAV_TO_TABLE", "T1", "forward");

        _clientProxyMock.Verify(
            x => x.SendCoreAsync("ReceiveRobotCommand",
                It.Is<object[]>(a => a.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastRobotStateAsync_ShouldSendToRobotGroup()
    {
        await _sut.BroadcastRobotStateAsync(1.0, 2.0, 0.5, 0.3, 0.1, "moving");

        _clientProxyMock.Verify(
            x => x.SendCoreAsync("ReceiveRobotState",
                It.Is<object[]>(a => a.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastRobotPathAsync_ShouldSendToRobotGroup()
    {
        var path = new List<PathPoint> { new() { X = 1, Y = 2 } };

        await _sut.BroadcastRobotPathAsync(path);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync("ReceiveRobotPath",
                It.Is<object[]>(a => a.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyDeliveryCompletedAsync_ShouldSendToRobotGroup()
    {
        await _sut.NotifyDeliveryCompletedAsync("R01", 5);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync("DeliveryCompleted",
                It.Is<object[]>(a => a.Length == 1),
                default),
            Times.Once);
    }
}
