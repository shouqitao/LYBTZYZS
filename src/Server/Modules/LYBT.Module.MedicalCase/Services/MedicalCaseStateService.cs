using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案状态服务实现 - 状态管理操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：UpdateStatus, Complete, CloseCase, SaveDraft, Cancel等状态流转操作
    /// OpenSpec: adopt-mapperly-unified-mapping - 移除IMapper依赖（此Service无映射需求）
    /// </summary>
    public class MedicalCaseStateService : BaseService<MedicalCase>, IMedicalCaseStateService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IMedicalCaseAuditService _auditService;
        private readonly IMedicalCasePermissionService _permissionService;

        public MedicalCaseStateService(
            IMedicalCaseRepository repository,
            IUserRepository userRepository,
            IMedicalCaseAuditService auditService,
            IMedicalCasePermissionService permissionService,
            ILogger<MedicalCaseStateService> logger)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        }

        /// <summary>
        /// 更新医案状态
        /// 支持 Draft/Active/Completed 状态流转（Cancelled 已移除，使用 IsDeleted 替代）
        /// </summary>
        public async Task<MedicalCase?> UpdateStatusAsync(
            Guid medicalCaseId,
            MedicalCaseStatus status)
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdateStatus - MedicalCaseId={MedicalCaseId} Status={Status}",
                medicalCaseId, status);

            // Guard: 完成状态必须通过 CompleteAsync，不允许通过 UpdateStatus 直接设置
            if (status == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → CompletedBlocked - 请使用 CompleteAsync");
                throw new InvalidOperationException("完成医案请使用专用的 Complete 接口，不允许通过状态更新直接设置为 Completed");
            }

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 业务规则验证：状态流转合法性
            if (!MedicalCaseRules.IsValidStatusTransition(medicalCase.CaseStatus, status))
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → InvalidTransition - OldStatus={OldStatus} NewStatus={NewStatus}",
                    medicalCase.CaseStatus, status);
                throw new InvalidOperationException($"不允许从{medicalCase.CaseStatus}状态转换到{status}状态");
            }

            // 更新状态（仅 Draft <-> Active）
            medicalCase.CaseStatus = status;
            medicalCase.UpdatedAt = DateTime.Now;

            // 保存
            return await _repository.UpdateAsync(medicalCase);
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
            bool skipWorkflowValidation = false)
        {
            _logger.LogInformation("[SVC] MedicalCase.Complete - MedicalCaseId={MedicalCaseId} SkipValidation={Skip}",
                medicalCaseId, skipWorkflowValidation);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
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
                    throw new InvalidOperationException("请先标记是否需要开处方");
                }

                // 如果标记需要开处方，验证处方存在
                if (medicalCase.NeedsPrescription == true)
                {
                    if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
                    {
                        _logger.LogWarning("[SVC] MedicalCase.Complete → PrescriptionRequired - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                        throw new InvalidOperationException("已标记需要开处方，但处方不存在，无法完成医案");
                    }
                }
            }

            // DDD: 委托给聚合根域方法
            medicalCase.Complete();

            // 保存
            return await _repository.UpdateAsync(medicalCase);
        }

        /// <summary>
        /// 关闭医案（直接标记为Completed，不验证三步流程）
        /// 委托给统一的 CompleteAsync(skipWorkflowValidation: true)
        /// </summary>
        public async Task<MedicalCase?> CloseCaseAsync(Guid id)
        {
            return await CompleteAsync(id, Guid.Empty, isAdmin: false, skipWorkflowValidation: true);
        }

        /// <summary>
        /// 暂存医案（保存草稿）
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-010)
        /// 业务规则：保存当前数据，设置状态为Draft，不触发完成验证
        /// </summary>
        public async Task<MedicalCase?> SaveDraftAsync(
            Guid id,
            ConsultationInputDto? request,
            Guid operatorId,
            bool isAdmin = false)
        {
            _logger.LogInformation("[SVC] MedicalCase.SaveDraft - MedicalCaseId={MedicalCaseId}", id);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.SaveDraft → NotFound - MedicalCaseId={MedicalCaseId}", id);
                return null;
            }

            // 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, operatorId, isAdmin, "SaveDraft", _logger);

            // 业务规则验证：只有Draft/Active状态可以暂存
            if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.SaveDraft → AlreadyCompleted - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("已完成的医案不可暂存");
            }

            // 已软删除的医案不可暂存
            if (medicalCase.IsDeleted)
            {
                _logger.LogWarning("[SVC] MedicalCase.SaveDraft → AlreadyDeleted - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("已删除的医案不可暂存");
            }

            // DDD: 委托给聚合根域方法
            if (request != null)
            {
                medicalCase.UpdateConsultation(
                    request.PresentIllness, request.TongueDiagnosis,
                    request.PulseDiagnosis, request.TcmDiagnosis);
            }

            medicalCase.SaveAsDraft();

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);

            // 记录审计日志
            var operatorInfo = await GetOperatorInfoAsync(operatorId, isAdmin);
            await _auditService.LogAsync(
                before: beforeState,
                after: result,
                operatorId: operatorId,
                operatorName: operatorInfo.Name,
                role: operatorInfo.Role,
                operationType: AuditOperationType.Update);

            return result;
        }

        /// <summary>
        /// 取消医案（统一为软删除）
        /// 原 LIFECYCLE-011: 设置 IsDeleted=true 替代 CaseStatus=Cancelled
        /// </summary>
        public async Task<MedicalCase?> CancelAsync(
            Guid id,
            Guid operatorId,
            bool isAdmin = false,
            string? reason = null)
        {
            _logger.LogInformation("[SVC] MedicalCase.Cancel - MedicalCaseId={MedicalCaseId}", id);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → NotFound - MedicalCaseId={MedicalCaseId}", id);
                return null;
            }

            // 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, operatorId, isAdmin, "Cancel", _logger);

            // 业务规则验证：只有Draft/Active状态可以取消
            if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → AlreadyCompleted - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("已完成的医案不可取消");
            }

            // 已软删除的不重复处理
            if (medicalCase.IsDeleted)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → AlreadyDeleted - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("医案已被删除");
            }

            // DDD: 委托给聚合根域方法
            medicalCase.SoftDelete();

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);

            // 记录审计日志
            var operatorInfo = await GetOperatorInfoAsync(operatorId, isAdmin);
            await _auditService.LogAsync(
                before: beforeState,
                after: result,
                operatorId: operatorId,
                operatorName: operatorInfo.Name,
                role: operatorInfo.Role,
                operationType: AuditOperationType.SoftDelete,
                reason: reason);

            return result;
        }

        #region Private Helper Methods (委托给 MedicalCaseServiceHelper)

        private static MedicalCase CloneMedicalCaseForAudit(MedicalCase source)
            => MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        private async Task<(string Name, UserRole Role)> GetOperatorInfoAsync(Guid userId, bool isAdmin)
            => await MedicalCaseServiceHelper.GetOperatorInfoAsync(_userRepository, userId, isAdmin, _logger);

        #endregion
    }
}
