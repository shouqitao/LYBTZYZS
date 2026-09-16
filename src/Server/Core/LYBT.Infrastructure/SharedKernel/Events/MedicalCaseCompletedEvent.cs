namespace LYBT.Infrastructure.SharedKernel.Events;

/// <summary>
/// 医案完成集成事件（ADR-0018 首个用例：MedicalCase → Registration，US-REG-005）。
/// 定义于 Infrastructure 而非模块内：P07 禁止模块互引，跨模块事件契约与
/// <c>IXxxCrossModuleService</c> 同层共享，发布方/消费方均只依赖 Infrastructure。
/// 发布方：LYBT.Module.MedicalCases（MedicalCaseStateService.CompleteAsync）；
/// 消费方：LYBT.Module.Registrations（INotificationHandler）。
/// </summary>
public sealed record MedicalCaseCompletedEvent(
    Guid MedicalCaseId,
    Guid PatientId,
    Guid DoctorId,
    DateTime CompletedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
