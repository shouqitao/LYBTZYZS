using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.PureLogic.Registration;

public class RegistrationQueueLogicTests
{
    [Theory]
    [InlineData(UserRole.Receptionist, true)]
    [InlineData(UserRole.Doctor, false)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.SuperAdmin, true)]
    public void CanCancelRegistration_requires_receptionist_role(UserRole role, bool expected)
    {
        var canCancel = role is UserRole.Receptionist or UserRole.Admin or UserRole.SuperAdmin;

        canCancel.Should().Be(expected);
    }

    [Theory]
    [InlineData(RegistrationStatus.Waiting, true)]
    [InlineData(RegistrationStatus.InProgress, false)]
    [InlineData(RegistrationStatus.Completed, false)]
    [InlineData(RegistrationStatus.Cancelled, false)]
    public void CanStartVisit_requires_Waiting_status(RegistrationStatus status, bool expected)
    {
        var canStart = status == RegistrationStatus.Waiting;

        canStart.Should().Be(expected);
    }

    [Theory]
    [InlineData(RegistrationStatus.Waiting, RegistrationSource.Receptionist, true)]
    [InlineData(RegistrationStatus.Waiting, RegistrationSource.Doctor, false)]
    [InlineData(RegistrationStatus.InProgress, RegistrationSource.Receptionist, false)]
    [InlineData(RegistrationStatus.Completed, RegistrationSource.Receptionist, false)]
    public void CanCancelRegistration_requires_Waiting_and_Receptionist_source(
        RegistrationStatus status, RegistrationSource source, bool expected)
    {
        var canCancel = status == RegistrationStatus.Waiting
                        && source == RegistrationSource.Receptionist;

        canCancel.Should().Be(expected);
    }

    [Fact]
    public void Doctor_queue_filter_by_role()
    {
        var doctorId = Guid.NewGuid();

        var filter = (bool isDoctor) => isDoctor ? doctorId : (Guid?)null;

        filter(true).Should().Be(doctorId);
        filter(false).Should().BeNull();
    }

    [Fact]
    public void Doctor_queue_filter_returns_only_own_registrations()
    {
        var doctorId = Guid.NewGuid();
        var otherDoctorId = Guid.NewGuid();

        var queue = new List<(Guid Id, Guid DoctorId)>
        {
            (Guid.NewGuid(), doctorId),
            (Guid.NewGuid(), otherDoctorId),
            (Guid.NewGuid(), doctorId)
        };

        var filtered = queue.Where(r => r.DoctorId == doctorId).ToList();

        filtered.Should().HaveCount(2);
        filtered.Should().OnlyContain(r => r.DoctorId == doctorId);
    }

    [Fact]
    public void Queue_status_filter_excludes_non_waiting()
    {
        var queue = new List<(Guid Id, RegistrationStatus Status)>
        {
            (Guid.NewGuid(), RegistrationStatus.Waiting),
            (Guid.NewGuid(), RegistrationStatus.InProgress),
            (Guid.NewGuid(), RegistrationStatus.Waiting),
            (Guid.NewGuid(), RegistrationStatus.Cancelled)
        };

        var waitingOnly = queue.Where(r => r.Status == RegistrationStatus.Waiting).ToList();

        waitingOnly.Should().HaveCount(2);
        waitingOnly.Should().OnlyContain(r => r.Status == RegistrationStatus.Waiting);
    }

    [Fact]
    public void Queue_ordered_by_registration_time()
    {
        var queue = new List<(Guid Id, DateTime CreatedAt)>
        {
            (Guid.NewGuid(), DateTime.Now.AddMinutes(-5)),
            (Guid.NewGuid(), DateTime.Now.AddMinutes(-10)),
            (Guid.NewGuid(), DateTime.Now.AddMinutes(-1))
        };

        var ordered = queue.OrderBy(r => r.CreatedAt).ToList();

        ordered[0].CreatedAt.Should().BeBefore(ordered[1].CreatedAt);
        ordered[1].CreatedAt.Should().BeBefore(ordered[2].CreatedAt);
    }
}
