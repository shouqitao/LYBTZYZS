using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Registrations.Application.Commands;

/// <summary>
/// 接诊处理器（接诊即建 D8 修复）。
/// 事务策略（design-03 §2 方案 C）：两步写 + 幂等 + 补偿，不是跨库原子事务——
/// 挂号侧（RegistrationDbContext）与医案侧（MedicalCaseDbContext）分属模块自有 DbContext（ADR-0017），
/// 两连接不同，任一方事务都不可能覆盖对方写入，故不引入 TransactionScope/分布式事务。
/// 步骤：
/// 1) 建案：_medicalCaseCrossModule.CreateMedicalCaseForRegistrationAsync →
///    MedicalCaseCommandService.SaveAsync → BaseRepository.AddAsync 单次 SaveChanges（医案聚合自成一个隐式事务）；
/// 2) 写挂号：本处理器事务内写 Registration.Status=InProgress（同一 DI 作用域下
///    RegistrationCrossModuleService 复用同一个 RegistrationDbContext 实例，其 SaveChanges 自动参与本事务，
///    与 Status 一并在 CommitAsync 提交）。
/// 幂等键 = Registration.MedicalCaseId（关联由挂号侧持有；MedicalCase 实体无 RegistrationId 列）：
/// 重试时若该挂号已处于 InProgress 且已关联医案，直接返回既有 MedicalCaseId，绝不重复建案。
/// 补偿：建案返回 null（未落库）→ RevertToWaiting() 回退等待态并提交，如实返回失败；
/// 事务回滚路径同样 RevertToWaiting()，避免回滚后的脏跟踪实体被后续 SaveChanges 复活。
/// 失败窗口（如实标注）：第 1 步医案已提交、第 2 步挂号侧失败回滚 → 医案已落库但挂号未关联；
/// 重试时由 BR-001 单活跃医案约束（MedicalCaseServiceHelper.ValidateAndFetchCreationContextAsync → McActiveCaseExists）
/// 拒绝重复建案，故不会产生重复医案。
/// 复用 BR-001 单活跃医案约束，碰撞时提示「重开现有医案」。
/// </summary>
public sealed class StartVisitCommandHandler
    : IRequestHandler<StartVisitCommand, Result<Guid>>
{
    private readonly IRegistrationRepository _repository;
    private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModule;
    private readonly INotificationService _notificationService;

    public StartVisitCommandHandler(
        IRegistrationRepository repository,
        IMedicalCaseCrossModuleService medicalCaseCrossModule,
        INotificationService notificationService)
    {
        _repository = repository;
        _medicalCaseCrossModule = medicalCaseCrossModule;
        _notificationService = notificationService;
    }

    public async Task<Result<Guid>> Handle(
        StartVisitCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (entity is null)
            return Result<Guid>.Failure(ErrorCode.RegistrationNotFound, ErrorMessages.Get(ErrorCode.RegistrationNotFound));

        // 幂等（design-03 §2 方案 C）：该挂号已接诊且已关联医案 → 直接复用既有医案，绝不重复建案。
        // 幂等键 = Registration.MedicalCaseId（关联由挂号侧持有，MedicalCase 实体无 RegistrationId 列）。
        if (entity.Status == RegistrationStatus.InProgress && entity.MedicalCaseId.HasValue)
            return Result<Guid>.Success(entity.MedicalCaseId.Value);

        try
        {
            entity.StartVisit();
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ErrorCode.RegistrationInvalidStatusTransition, ex.Message);
        }

        // 事务只覆盖挂号侧写入：跨模块建案走 MedicalCaseDbContext（另一连接），其提交独立于本事务（design-03 §2）
        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);
        try
        {
            var medicalCaseId = await _medicalCaseCrossModule.CreateMedicalCaseForRegistrationAsync(
                entity.PatientId, entity.Id, entity.DoctorId, cancellationToken);

            if (medicalCaseId is null)
            {
                // 补偿：建案未落库 → 回退等待态并提交本事务，如实返回失败（禁止静默成功）
                entity.RevertToWaiting();
                await _repository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result<Guid>.Failure(ErrorCode.Unknown, "医案创建失败，挂号已恢复等待状态");
            }

            await _repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // US-REG-008: 接诊状态变更实时同步到该医生待诊列表
            await _notificationService.NotifyRegistrationStatusChangedAsync(
                entity.DoctorId, entity.Id, entity.Status.ToString(), cancellationToken);

            return Result<Guid>.Success(medicalCaseId.Value);
        }
        catch (BusinessException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            CompensateInMemory(entity);
            return Result<Guid>.Failure(ex.TypedErrorCode ?? ErrorCode.Unknown, ex.Message);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            CompensateInMemory(entity);
            throw;
        }
    }

    /// <summary>
    /// 回滚后补偿：把内存中的挂号聚合还原为「等待」，与已回滚的 DB 状态一致。
    /// 回滚不清理 ChangeTracker，若不还原，残留的 Status=InProgress 会被同一作用域后续的 SaveChanges 再次落库。
    /// </summary>
    private static void CompensateInMemory(Registration entity)
        => entity.RevertToWaiting();
}
