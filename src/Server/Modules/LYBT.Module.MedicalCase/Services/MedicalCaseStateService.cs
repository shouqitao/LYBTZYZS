using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Validators.BusinessRules;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案状态服务实现 - 状态管理操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：UpdateStatus, Complete, CloseCase, Suspend, Cancel等状态流转操作
    /// </summary>
    public class MedicalCaseStateService : BaseService<MedicalCase>, IMedicalCaseStateService
    {
        /// <summary>
        /// 审计操作类型：0=创建 1=更新 2=状态变更 3=删除 4=取消（US-MC-014 取消统计）
        /// </summary>
        private const int AuditOperationCancel = 4;

        private readonly IMedicalCaseRepository _repository;
        private readonly IUserService _userCrossModule;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly IRegistrationCrossModuleService _registrationCrossModule;

        public MedicalCaseStateService(
            IMedicalCaseRepository repository,
            IUserService userCrossModule,
            ILogger<MedicalCaseStateService> logger,
            ICacheInvalidationService cacheInvalidation,
            IRegistrationCrossModuleService registrationCrossModule)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userCrossModule = userCrossModule ?? throw new ArgumentNullException(nameof(userCrossModule));
            _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
            _registrationCrossModule = registrationCrossModule ?? throw new ArgumentNullException(nameof(registrationCrossModule));
        }

        /// <summary>
        /// 更新医案状态
        /// 支持 Draft/Active/Completed 状态流转（Cancelled 已移除，使用 IsDeleted 替代）
        /// </summary>
        public async Task<MedicalCase?> UpdateStatusAsync(
            Guid medicalCaseId,
            MedicalCaseStatus status,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdateStatus - MedicalCaseId={MedicalCaseId} Status={Status}",
                medicalCaseId, status);

            // Guard: 完成状态必须通过 CompleteAsync，不允许通过 UpdateStatus 直接设置
            if (status == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → CompletedBlocked - 请使用 CompleteAsync");
                throw new BusinessException(ErrorCode.McInvalidStatusTransition, "完成医案请使用专用的 Complete 接口，不允许通过状态更新直接设置为 Completed");
            }

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 业务规则验证：状态流转合法性
            if (!MedicalCaseBusinessRules.IsValidStatusTransition(medicalCase.CaseStatus, status))
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → InvalidTransition - OldStatus={OldStatus} NewStatus={NewStatus}",
                    medicalCase.CaseStatus, status);
                throw new BusinessException(ErrorCode.McInvalidStatusTransition, $"不允许从{medicalCase.CaseStatus}状态转换到{status}状态");
            }

            // 更新状态（仅 Draft <-> Active）
            medicalCase.CaseStatus = status;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // 保存
            return await _repository.UpdateAsync(medicalCase, cancellationToken);
        }

        /// <summary>
        /// 统一完成医案入口
        /// skipWorkflowValidation=false: 验证 NeedsPrescription + 处方存在性 (BR-003)
        /// skipWorkflowValidation=true: 直接完成 (原 CloseCaseAsync 行为)
        /// 始终设置 CompletedAt
        /// </summary>
        public async Task<MedicalCase?> CompleteAsync(
            Guid medicalCaseId,
            Guid operatorId,
            bool isAdmin = false,
            bool skipWorkflowValidation = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.Complete - MedicalCaseId={MedicalCaseId} SkipValidation={Skip}",
                medicalCaseId, skipWorkflowValidation);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Complete → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 工作流验证（skipWorkflowValidation=false 时执行）
            if (!skipWorkflowValidation)
            {
                // 业务规则验证：处方需求标记
                if (medicalCase.NeedsPrescription == null)
                {
                    _logger.LogWarning("[SVC] MedicalCase.Complete → NeedsPrescriptionNotSet - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                    throw new BusinessException(ErrorCode.McPrescriptionFlagRequired, ErrorMessages.Get(ErrorCode.McPrescriptionFlagRequired));
                }

                // 如果标记需要开处方，验证处方存在
                if (medicalCase.NeedsPrescription == true)
                {
                    if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
                    {
                        _logger.LogWarning("[SVC] MedicalCase.Complete → PrescriptionRequired - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                        throw new BusinessException(ErrorCode.McPrescriptionRequired, ErrorMessages.Get(ErrorCode.McPrescriptionRequired));
                    }

                    // T5-P2-15: 验证处方明细不为空
                    if (medicalCase.Prescription.Items == null || !medicalCase.Prescription.Items.Any())
                    {
                        _logger.LogWarning("[SVC] MedicalCase.Complete → PrescriptionItemsEmpty - MedicalCaseId={MedicalCaseId}",
                            medicalCaseId);
                        throw new BusinessException(ErrorCode.McPrescriptionItemsRequired, ErrorMessages.Get(ErrorCode.McPrescriptionItemsRequired));
                    }
                }
            }

            // CODE-01: 完成医案时验证中医诊断必填
            if (string.IsNullOrWhiteSpace(medicalCase.Consultation?.TcmDiagnosis))
            {
                _logger.LogWarning("[SVC] MedicalCase.Complete -> TcmDiagnosisRequired - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                throw new BusinessException(ErrorCode.MedicalCaseMissingDiagnosis, ErrorMessages.Get(ErrorCode.MedicalCaseMissingDiagnosis));
            }

            // DDD: 委托给聚合根域方法
            medicalCase.Complete();

            // 保存
            var result = await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

            // US-REG-005: 医案完成时联动挂号状态 → Completed
            if (result != null)
            {
                await CompleteRegistrationAsync(medicalCaseId, cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// 挂起医案（暂停处理）
        /// 业务规则：保存当前数据，设置状态为Suspended，不触发完成验证
        /// </summary>
        public async Task<MedicalCase?> SuspendAsync(
            Guid id,
            ConsultationInputDto? request,
            Guid operatorId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.Suspend - MedicalCaseId={MedicalCaseId}", id);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(id, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Suspend → NotFound - MedicalCaseId={MedicalCaseId}", id);
                return null;
            }

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(medicalCase, operatorId, isAdmin, "Suspend", _logger);

            // 业务规则验证：只有Suspended/Active状态可以挂起
            if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.Suspend → AlreadyCompleted - MedicalCaseId={MedicalCaseId}", id);
                throw new BusinessException(ErrorCode.McCompletedCannotSuspend, ErrorMessages.Get(ErrorCode.McCompletedCannotSuspend));
            }

            // 已软删除的医案不可挂起
            if (medicalCase.IsDeleted)
            {
                _logger.LogWarning("[SVC] MedicalCase.Suspend → AlreadyDeleted - MedicalCaseId={MedicalCaseId}", id);
                throw new BusinessException(ErrorCode.McDeletedCannotSuspend, ErrorMessages.Get(ErrorCode.McDeletedCannotSuspend));
            }

            // DDD: 委托给聚合根域方法
            if (request != null)
            {
                medicalCase.UpdateConsultation(
                    request.PresentIllness, request.TongueDiagnosis,
                    request.PulseDiagnosis, request.TcmDiagnosis);
            }

            medicalCase.Suspend();

            // 保存
            var result = await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

            return result;
        }

        /// <summary>
        /// 取消医案（US-MC-014：物理删除）
        /// 2026-08-03 决策：取消 = 物理删除（不判内容），级联清除聚合
        /// （MedicalCase + Consultation + Prescription + PrescriptionItems + PrintLogs），
        /// 审计记录 OperationType=Cancel 用于统计；已完成医案不可取消（只可软删，Admin 清理）。
        /// </summary>
        public async Task<MedicalCase?> CancelAsync(
            Guid id,
            Guid operatorId,
            bool isAdmin = false,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.Cancel - MedicalCaseId={MedicalCaseId}", id);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(id, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → NotFound - MedicalCaseId={MedicalCaseId}", id);
                return null;
            }

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(medicalCase, operatorId, isAdmin, "Cancel", _logger);

            // T5-P2-16: 非当天本人取消需原因（US-MC-014 保留审计理由）
            var isSameDay = medicalCase.CreatedAt.Date == DateTime.Today;
            var isOwner = medicalCase.UserId == operatorId;
            if (!(isSameDay && isOwner) && string.IsNullOrWhiteSpace(reason))
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → ReasonRequired - MedicalCaseId={MedicalCaseId} IsOwner={IsOwner} IsSameDay={IsSameDay}",
                    id, isOwner, isSameDay);
                throw new BusinessException(ErrorCode.McCancelReasonRequired, ErrorMessages.Get(ErrorCode.McCancelReasonRequired));
            }

            // 业务规则验证：已完成医案不可取消（只可软删，Admin 清理）
            if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → AlreadyCompleted - MedicalCaseId={MedicalCaseId}", id);
                throw new BusinessException(ErrorCode.McCompletedCannotCancel, ErrorMessages.Get(ErrorCode.McCompletedCannotCancel));
            }

            // 已软删除的不重复处理
            if (medicalCase.IsDeleted)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → AlreadyDeleted - MedicalCaseId={MedicalCaseId}", id);
                throw new BusinessException(ErrorCode.McAlreadyDeleted, ErrorMessages.Get(ErrorCode.McAlreadyDeleted));
            }

            // 物理删除聚合根（DB 级联清除 Consultation/Prescription/Items/PrintLogs）
            var deleted = await _repository.HardDeleteAsync(medicalCase, cancellationToken);
            if (!deleted)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → HardDeleteFailed - MedicalCaseId={MedicalCaseId}", id);
                return null;
            }

            // 审计记录「取消」（US-MC-014；US-MC-017 异常隔离：审计失败不影响取消结果）
            await TryWriteCancelAuditAsync(medicalCase, operatorId, isAdmin, reason, cancellationToken);

            // G-9: 医案取消后，根据挂号来源回退挂号状态（Receptionist→Waiting / Doctor→Cancelled）
            await RollbackRegistrationAsync(id, medicalCase.CaseNumber ?? "N/A", cancellationToken);

            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

            return medicalCase;
        }

        /// <summary>
        /// 审计记录「取消」操作（OperationType=Cancel=4）
        /// US-MC-017 异常隔离：写入失败仅记录 Error 日志，不影响主业务流程
        /// </summary>
        private async Task TryWriteCancelAuditAsync(
            MedicalCase medicalCase,
            Guid operatorId,
            bool isAdmin,
            string? reason,
            CancellationToken cancellationToken)
        {
            try
            {
                var (operatorName, operatorRole) = await MedicalCaseServiceHelper
                    .GetOperatorInfoAsync(_userCrossModule, operatorId, isAdmin, _logger, cancellationToken);

                await _repository.AddAuditLogAsync(new MedicalCaseAuditLog
                {
                    MedicalCaseId = medicalCase.Id,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    OperatorRole = (int)operatorRole,
                    OperationType = AuditOperationCancel,
                    Reason = reason
                }, cancellationToken);

                _logger.LogInformation("[SVC] MedicalCase.Cancel → AuditRecorded - MedicalCaseId={MedicalCaseId}", medicalCase.Id);
            }
            catch (Exception ex)
            {
                // 审计隔离：记录失败不影响医案取消（US-MC-017）
                _logger.LogError(ex, "[SVC] MedicalCase.Cancel → AuditWriteFailed - MedicalCaseId={MedicalCaseId}", medicalCase.Id);
            }
        }

        /// <summary>
        /// G-9: 医案取消后回退挂号状态
        /// - Receptionist来源: 回退到Waiting，清除MedicalCaseId（原医案已物理删除，回来重新接诊时新建）
        /// - Doctor来源: 设置为Cancelled（闭环）
        /// </summary>
        private async Task RollbackRegistrationAsync(Guid medicalCaseId, string caseNumber, CancellationToken cancellationToken = default)
        {
            await _registrationCrossModule.HandleMedicalCaseCancelledAsync(medicalCaseId, cancellationToken);
            _logger.LogInformation("[SVC] MedicalCase.Cancel → RegistrationRolledBack - MedicalCaseId={MedicalCaseId} CaseNumber={CaseNumber}",
                medicalCaseId, caseNumber);
        }

        /// <summary>
        /// US-REG-005: 医案完成后联动挂号状态 → Completed
        /// </summary>
        private async Task CompleteRegistrationAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            await _registrationCrossModule.CompleteByMedicalCaseAsync(medicalCaseId, cancellationToken);
            _logger.LogInformation("[SVC] MedicalCase.Complete → RegistrationCompleted - MedicalCaseId={MedicalCaseId}",
                medicalCaseId);
        }

        #region 私有辅助方法

        #endregion
    }
}


