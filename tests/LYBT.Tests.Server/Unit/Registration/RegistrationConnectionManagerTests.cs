using LYBT.Module.Registrations.Hubs;

namespace LYBT.Tests.Server;

/// <summary>
/// RegistrationConnectionManager 测试 — ConnectionId ↔ DoctorId 连接映射。
/// </summary>
public class RegistrationConnectionManagerTests
{
    [Fact]
    public void Add_Then_TryGetDoctorId_ReturnsMappedDoctor()
    {
        var sut = new RegistrationConnectionManager();
        var doctorId = Guid.NewGuid();
        const string connectionId = "conn-1";

        sut.Add(connectionId, doctorId);

        sut.TryGetDoctorId(connectionId, out var mapped).Should().BeTrue();
        mapped.Should().Be(doctorId);
    }

    [Fact]
    public void TryRemove_RemovesMappingAndReturnsDoctorId()
    {
        var sut = new RegistrationConnectionManager();
        var doctorId = Guid.NewGuid();
        const string connectionId = "conn-2";

        sut.Add(connectionId, doctorId);

        sut.TryRemove(connectionId, out var removed).Should().BeTrue();
        removed.Should().Be(doctorId);
        sut.TryGetDoctorId(connectionId, out _).Should().BeFalse();
    }

    [Fact]
    public void CountConnections_CountsOnlyTargetDoctorConnections()
    {
        var sut = new RegistrationConnectionManager();
        var doctorA = Guid.NewGuid();
        var doctorB = Guid.NewGuid();

        sut.Add("conn-1", doctorA);
        sut.Add("conn-2", doctorA);
        sut.Add("conn-3", doctorB);

        sut.CountConnections(doctorA).Should().Be(2);
        sut.CountConnections(doctorB).Should().Be(1);
    }

    [Fact]
    public void TryGetDoctorId_UnknownConnection_ReturnsFalse()
    {
        var sut = new RegistrationConnectionManager();

        sut.TryGetDoctorId("unknown", out _).Should().BeFalse();
    }
}
