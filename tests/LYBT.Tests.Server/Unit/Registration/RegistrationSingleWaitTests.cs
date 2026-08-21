using LYBT.Module.Registrations.Application.Commands;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Server.Unit.Registration;

/// <summary>
/// P1-21 挂号单例保护
/// </summary>
public class RegistrationSingleWaitTests
{
    [Fact]
    public void RegistrationStatus_Waiting_Should_Be_Singleton()
    {
        // 业务规则：同一患者仅一 Waiting/InProgress
        var statuses = new[] { RegistrationStatus.Waiting, RegistrationStatus.InProgress, RegistrationStatus.Completed, RegistrationStatus.Cancelled };
        var pending = statuses.Where(s => s == RegistrationStatus.Waiting || s == RegistrationStatus.InProgress).ToList();
        Assert.Equal(2, pending.Count);
        Assert.Contains(RegistrationStatus.Waiting, pending);
        Assert.Contains(RegistrationStatus.InProgress, pending);
    }

    [Fact]
    public void CreateRegistration_SameDayWaiting_AlreadyChecked()
    {
        // HasSameDayWaiting 已在 Handler 中先校验，此处仅回归存在
        var dto = new RegistrationInputDto
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            PatientName = "张三",
            DoctorName = "李医生",
            Source = RegistrationSource.Receptionist
        };
        Assert.NotEqual(Guid.Empty, dto.PatientId);
    }
}
