namespace LYBT.Infrastructure.SharedKernel.Events;

/// <summary>
/// 医案软删除集成事件（R-10：软删路径不再直调 IRegistrationCrossModuleService，
/// 与 MedicalCaseCancelledEvent 同构，由 Registrations 模块 Handler 回滚挂号）。
/// 定义于 Infrastructure 而非模块内：P07 禁止模块互引，跨模块事件契约与
/// <c>IXxxCrossModuleService</c> 同层共享，发布方/消费方均只依赖 Infrastructure。
/// 发布方：LYBT.Module.MedicalCases（MedicalCaseCommandService.DeleteAsync / BatchDeleteAsync）；
/// 消费方：LYBT.Module.Registrations（INotificationHandler）。
/// </summary>
public sealed record MedicalCaseDeletedEvent(
    Guid MedicalCaseId,
    Guid PatientId,
    Guid DoctorId,
    DateTime DeletedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
