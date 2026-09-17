using FluentAssertions;
using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server._Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server.Integration.SqlCore;

/// <summary>
/// Registration 真实 SQL Server 集成测试 — 覆盖 Create + StartVisit + Complete 流程与 Cancel 流程。
/// </summary>
[Collection(SqlServerIntegrationCollection.Name)]
public class RegistrationSqlIntegrationTests : IntegrationTestBase
{
    protected override DbContext CreateContext()
        => new AppDbContext(TestDbFactory.CreateOptions<AppDbContext>(ConnectionString));

    private static Registration CreateWaitingRegistration()
    {
        return new Registration
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = $"患者_{Guid.NewGuid():N}"[..12],
            DoctorId = Guid.NewGuid(),
            DoctorName = "李医生",
            Source = RegistrationSource.Receptionist,
            Status = RegistrationStatus.Waiting,
            QueueNumber = 1,
            RegistrationFee = 10m
        };
    }

    [Fact]
    public async Task Create_StartVisit_Complete_ShouldPersistStatusTransitions()
    {
        await using var context = (AppDbContext)CreateContext();

        var registration = CreateWaitingRegistration();
        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        registration.Status.Should().Be(RegistrationStatus.Waiting);

        // StartVisit: Waiting -> InProgress
        registration.StartVisit();
        await context.SaveChangesAsync();

        await using var midContext = (AppDbContext)CreateContext();
        var inProgress = await midContext.Registrations.AsNoTracking()
            .SingleAsync(r => r.Id == registration.Id);
        inProgress.Status.Should().Be(RegistrationStatus.InProgress);
        inProgress.Source.Should().Be(RegistrationSource.Receptionist);
        inProgress.QueueNumber.Should().Be(1);
        inProgress.RegistrationFee.Should().Be(10m);

        // Complete: InProgress -> Completed（重新加载实体，走真实领域方法）
        var toComplete = await midContext.Registrations.SingleAsync(r => r.Id == registration.Id);
        toComplete.Complete();
        await midContext.SaveChangesAsync();

        await using var finalContext = (AppDbContext)CreateContext();
        var completed = await finalContext.Registrations.AsNoTracking()
            .SingleAsync(r => r.Id == registration.Id);
        completed.Status.Should().Be(RegistrationStatus.Completed);
    }

    [Fact]
    public async Task Cancel_FromWaiting_ShouldSetCancelled()
    {
        await using var context = (AppDbContext)CreateContext();

        var registration = CreateWaitingRegistration();
        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        registration.Cancel();
        await context.SaveChangesAsync();

        await using var readContext = (AppDbContext)CreateContext();
        var cancelled = await readContext.Registrations.AsNoTracking()
            .SingleAsync(r => r.Id == registration.Id);
        cancelled.Status.Should().Be(RegistrationStatus.Cancelled);
        cancelled.MedicalCaseId.Should().BeNull();
    }

    [Fact]
    public async Task Cancel_FromInProgress_ShouldThrow()
    {
        await using var context = (AppDbContext)CreateContext();

        var registration = CreateWaitingRegistration();
        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        registration.StartVisit();
        await context.SaveChangesAsync();

        var act = () => registration.Cancel();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*等待中*");
    }

    [Fact]
    public async Task Cancel_WhenMedicalCaseAssigned_ShouldThrow()
    {
        await using var context = (AppDbContext)CreateContext();

        var registration = CreateWaitingRegistration();
        registration.AssignMedicalCase(Guid.NewGuid());
        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        var act = () => registration.Cancel();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*医案*");
    }
}
