using LYBT.Module.Registration.Hubs;
using LYBT.Module.Registration.Services;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Server;

/// <summary>
/// NotificationService 测试 — SignalR 推送逻辑与按医生（DoctorId）分组过滤 (US-REG-008)。
/// </summary>
public class NotificationServiceTests
{
    private readonly FakeHubContext _hubContext = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(_hubContext, NullLogger<NotificationService>.Instance);
    }

    [Fact]
    public async Task NotifyNewRegistrationAsync_SendsRegistrationToDoctorGroup()
    {
        var doctorId = Guid.NewGuid();
        var registration = new RegistrationDetailDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "张三",
            Status = RegistrationStatus.Waiting
        };

        await _sut.NotifyNewRegistrationAsync(doctorId, registration);

        var message = _hubContext.Sent.Should().ContainSingle().Subject;
        message.Group.Should().Be(RegistrationHub.GetDoctorGroup(doctorId));
        message.Method.Should().Be("NewRegistration");

        var payload = message.Args[0].Should().BeOfType<RegistrationDetailDto>().Subject;
        payload.Id.Should().Be(registration.Id);
        payload.PatientName.Should().Be("张三");
    }

    [Fact]
    public async Task NotifyRegistrationStatusChangedAsync_SendsStatusToDoctorGroup()
    {
        var doctorId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        await _sut.NotifyRegistrationStatusChangedAsync(doctorId, registrationId, "Cancelled");

        var message = _hubContext.Sent.Should().ContainSingle().Subject;
        message.Group.Should().Be(RegistrationHub.GetDoctorGroup(doctorId));
        message.Method.Should().Be("RegistrationStatusChanged");
        message.Args[0].Should().Be(registrationId);
        message.Args[1].Should().Be("Cancelled");
    }

    [Fact]
    public async Task NotifyNewRegistrationAsync_EmptyDoctorId_DoesNotSend()
    {
        await _sut.NotifyNewRegistrationAsync(Guid.Empty, new RegistrationDetailDto());

        _hubContext.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyRegistrationStatusChangedAsync_EmptyDoctorId_DoesNotSend()
    {
        await _sut.NotifyRegistrationStatusChangedAsync(Guid.Empty, Guid.NewGuid(), "InProgress");

        _hubContext.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyDifferentDoctors_ArePushedToSeparateGroups_NoCrossDelivery()
    {
        var doctorA = Guid.NewGuid();
        var doctorB = Guid.NewGuid();

        await _sut.NotifyNewRegistrationAsync(doctorA, new RegistrationDetailDto { Id = Guid.NewGuid() });
        await _sut.NotifyRegistrationStatusChangedAsync(doctorB, Guid.NewGuid(), "InProgress");

        _hubContext.Sent.Should().HaveCount(2);
        _hubContext.Sent[0].Group.Should().Be(RegistrationHub.GetDoctorGroup(doctorA));
        _hubContext.Sent[1].Group.Should().Be(RegistrationHub.GetDoctorGroup(doctorB));
    }

    /// <summary>
    /// 手写 IHubContext fake（Server 测试禁止 mock 框架），记录按分组发送的消息。
    /// </summary>
    private sealed class FakeHubContext : IHubContext<RegistrationHub>
    {
        public FakeHubContext()
        {
            Clients = new FakeClients(Sent);
        }

        public List<SentMessage> Sent { get; } = [];

        public IHubClients Clients { get; }

        public IGroupManager Groups => throw new NotSupportedException();

        public sealed record SentMessage(string? Group, string Method, object?[] Args);

        private sealed class FakeClients(List<SentMessage> sent) : IHubClients
        {
            public IClientProxy All => new FakeProxy(sent, null);
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
            public IClientProxy Client(string connectionId) => throw new NotSupportedException();
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
            public IClientProxy Group(string groupName) => new FakeProxy(sent, groupName);
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
            public IClientProxy User(string userId) => throw new NotSupportedException();
            public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
        }

        private sealed class FakeProxy(List<SentMessage> sent, string? group) : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            {
                sent.Add(new SentMessage(group, method, args));
                return Task.CompletedTask;
            }
        }
    }
}
