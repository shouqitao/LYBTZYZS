using LYBT.Module.Registrations.Hubs;

namespace LYBT.Tests.Server;

/// <summary>
/// RegistrationConnectionManager 测试 — ConnectionId ↔ DoctorId 连接映射。
/// </summary>
public class RegistrationConnectionManagerTests
{
    [Fact]
    public void TryRemove_RemovesMappingAndReturnsDoctorId()
    {
        var sut = new RegistrationConnectionManager();
        var doctorId = Guid.NewGuid();
        const string connectionId = "conn-2";

        sut.Add(connectionId, doctorId);

        sut.TryRemove(connectionId, out var removed).Should().BeTrue();
        removed.Should().Be(doctorId);
        sut.TryRemove(connectionId, out _).Should().BeFalse();
    }
}
