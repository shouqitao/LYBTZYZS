using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案命令服务 - 删除操作（软删除）
    /// U3-1: 从 MedicalCaseCommandService 拆分
    /// 共享主文件的 fields/ctor（partial 类同文件内可见）
    /// </summary>
    public partial class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService
    {
        /// <summary>
        /// 删除医案（软删除）— Admin 清理路径
        /// US-MC-015: 仅用于已完成医案；未完成医案走「取消」= 物理删除（MedicalCaseStateService.CancelAsync）
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.Delete - MedicalCaseId={MedicalCaseId} OperatorId={OperatorId}", id, operatorId);

            var medicalCase = await _repository.GetByIdAsync(id, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Delete -> NotFound - MedicalCaseId={MedicalCaseId}", id);
                return false;
            }

            // 权限检查: 确保操作者有权删除此医案
            MedicalCaseServiceHelper.EnsureCanDelete(medicalCase, operatorId, isAdmin, "Delete", _logger);

            // US-MC-015: 软删除仅限已完成医案，未完成医案应使用「取消」（物理删除）
            if (medicalCase.CaseStatus != MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.Delete → OnlyCompletedCanDelete - MedicalCaseId={MedicalCaseId} Status={Status}",
                    id, medicalCase.CaseStatus);
                throw new BusinessException(ErrorCode.McOnlyCompletedCanDelete, ErrorMessages.Get(ErrorCode.McOnlyCompletedCanDelete));
            }

            // D2 FIX: 删除前回滚关联的挂号记录
            await _registrationCrossModule.HandleMedicalCaseCancelledAsync(id, cancellationToken);
            _logger.LogInformation("[SVC] MedicalCase.Delete → RegistrationRolledBack - MedicalCaseId={MedicalCaseId}", id);

            var result = await _repository.SoftDeleteAsync(id, cancellationToken);
            if (result)
            {
                await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<LYBT.Shared.Models.Contracts.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var result = new LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
                try
                {
                    var entity = await _repository.GetByIdAsync(id, cancellationToken);
                    if (entity == null)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new LYBT.Shared.Models.Contracts.Common.BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "医案不存在"
                        });
                        continue;
                    }

                    // 权限检查: 确保操作者有权删除此医案
                    MedicalCaseServiceHelper.EnsureCanDelete(entity, operatorId, isAdmin, "BatchDelete", _logger);

                    // US-MC-015: 软删除仅限已完成医案，未完成医案应使用「取消」（物理删除）
                    if (entity.CaseStatus != MedicalCaseStatus.Completed)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new LYBT.Shared.Models.Contracts.Common.BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "仅已完成医案可删除（未完成医案请使用取消）"
                        });
                        _logger.LogWarning("[SVC] MedicalCase.BatchDelete → OnlyCompletedCanDelete - MedicalCaseId={MedicalCaseId}", id);
                        continue;
                    }

                    // D2 FIX: 与单删 DeleteAsync 一致，软删除前回滚关联挂号（Completed 医案走直调，不经 Cancelled 领域事件，无重复回滚风险）
                    await _registrationCrossModule.HandleMedicalCaseCancelledAsync(id, cancellationToken);
                    _logger.LogInformation("[SVC] MedicalCase.BatchDelete → RegistrationRolledBack - MedicalCaseId={MedicalCaseId}", id);

                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(entity, cancellationToken);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] MedicalCase.BatchDelete → ItemSuccess - MedicalCaseId={MedicalCaseId}", id);
                }
                catch (Exception ex)
                {
                    // 保留项级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new LYBT.Shared.Models.Contracts.Common.BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "删除操作失败"
                    });
                    _logger.LogError(ex, "[SVC] MedicalCase.BatchDelete → ItemFailed - MedicalCaseId={MedicalCaseId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return LYBT.Shared.Models.Contracts.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>.Success(result);
        }
    }
}
