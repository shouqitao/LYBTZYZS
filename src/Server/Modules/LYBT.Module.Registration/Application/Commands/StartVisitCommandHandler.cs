using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 接诊处理器（接诊即建 D8 修复）。
/// 原子事务：Registration.Status=InProgress + 创建 MedicalCase(Active) 关联 RegistrationId，返回 MedicalCaseId。
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

        try
        {
            entity.StartVisit();
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ErrorCode.RegistrationInvalidStatusTransition, ex.Message);
        }

        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);
        try
        {
            var medicalCaseId = await _medicalCaseCrossModule.CreateMedicalCaseForRegistrationAsync(
                entity.PatientId, entity.Id, entity.DoctorId, cancellationToken);

            if (medicalCaseId is null)
            {
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
            return Result<Guid>.Failure(ex.TypedErrorCode ?? ErrorCode.Unknown, ex.Message);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
