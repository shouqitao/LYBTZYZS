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
    /// 病案状态服务实现 - 状态管理操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：UpdateStatus, Complete, CloseCase, SaveDraft, Cancel等状态流转操作
    /// OpenSpec: adopt-mapperly-unified-mapping - 移除IMapper依赖（此Service无映射需求）
    /// </summary>
    public class MedicalCaseStateService : BaseService<MedicalCase>, IMedicalCaseStateService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IMedicalCaseAuditService _auditService;

        public MedicalCaseStateService(
            IMedicalCaseRepository repository,
            IUserRepository userRepository,
            IMedicalCaseAuditService auditService,
            ILogger<MedicalCaseStateService> logger)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        /// <summary>
        /// 更新病案状态
        /// Epic #1612: 支持Active/Completed/Cancelled状态流转
        /// </summary>
        public async Task<MedicalCase?> UpdateStatusAsync(
            Guid medicalCaseId,
            MedicalCaseStatus status)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.UpdateStatus started - MedicalCaseId={MedicalCaseId} Status={Status}",
                medicalCaseId, status);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 业务规则验证：状态流转合法性（Issue #1757: 使用MedicalCaseValidationHelper）
            if (!MedicalCaseValidationHelper.IsValidStatusTransition(medicalCase.CaseStatus, status))
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateStatus → InvalidTransition - OldStatus={OldStatus} NewStatus={NewStatus}",
                    medicalCase.CaseStatus, status);
                throw new InvalidOperationException($"不允许从{medicalCase.CaseStatus}状态转换到{status}状态");
            }

            // 更新状态
            medicalCase.CaseStatus = status;
            medicalCase.UpdatedAt = DateTime.Now;

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("[SVC] MedicalCase.UpdateStatus completed - MedicalCaseId={MedicalCaseId} NewStatus={Status}",
                medicalCaseId, status);
            return result;
        }

        /// <summary>
        /// 完成病案（三步流程最后一步）
        /// Epic #1612: 验证三步流程完整性后标记为Completed
        /// 业务规则：BF-002（三步流程验证）
        /// </summary>
        public async Task<MedicalCase?> CompleteAsync(Guid medicalCaseId)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.Complete started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Complete → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

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
                    throw new InvalidOperationException("已标记需要开处方，但处方不存在，无法完成病案");
                }
            }

            // 更新状态为Completed
            medicalCase.CaseStatus = MedicalCaseStatus.Completed;
            medicalCase.UpdatedAt = DateTime.Now;

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("[SVC] MedicalCase.Complete completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        public async Task<MedicalCase?> CloseCaseAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.Close started - MedicalCaseId={MedicalCaseId}", id);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Close → NotFound - MedicalCaseId={MedicalCaseId}", id);
                return null;
            }

            // 直接更新状态为Completed（不验证三步流程）
            medicalCase.CaseStatus = MedicalCaseStatus.Completed;
            medicalCase.UpdatedAt = DateTime.Now;

            // 设置CompletedAt时间戳
            if (medicalCase.Consultation != null)
            {
                medicalCase.Consultation.UpdatedAt = DateTime.Now;
            }

            // 保存
            await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("[SVC] MedicalCase.Close completed - MedicalCaseId={MedicalCaseId}", id);
            
            // OpenSpec: optimize-medicalcase-api - 返回更新后的实体用于DTO映射
            return medicalCase;
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
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.SaveDraft started - MedicalCaseId={MedicalCaseId}", id);

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
            if (!MedicalCaseRules.CanEdit(medicalCase, operatorId, isAdmin))
            {
                _logger.LogWarning("[SVC] MedicalCase.SaveDraft → PermissionDenied - MedicalCaseId={MedicalCaseId} UserId={UserId}",
                    id, operatorId);
                throw new UnauthorizedAccessException("无权限编辑此病案");
            }

            // 业务规则验证：只有Draft/Active状态可以暂存
            if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.SaveDraft → AlreadyCompleted - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("已完成的医案不可暂存");
            }

            if (medicalCase.CaseStatus == MedicalCaseStatus.Cancelled)
            {
                _logger.LogWarning("[SVC] MedicalCase.SaveDraft → AlreadyCancelled - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("已取消的医案不可暂存");
            }

            // 如果提供了诊断信息，更新Consultation
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            if (request != null && medicalCase.Consultation != null)
            {
                medicalCase.Consultation.PresentIllness = request.PresentIllness;
                medicalCase.Consultation.TongueDiagnosis = request.TongueDiagnosis;
                medicalCase.Consultation.PulseDiagnosis = request.PulseDiagnosis;
                medicalCase.Consultation.TcmDiagnosis = request.TcmDiagnosis;
                medicalCase.Consultation.UpdatedAt = DateTime.Now;
            }

            // 设置状态为Draft
            medicalCase.CaseStatus = MedicalCaseStatus.Draft;
            medicalCase.UpdatedAt = DateTime.Now;

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("[SVC] MedicalCase.SaveDraft completed - MedicalCaseId={MedicalCaseId}", id);

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
        /// 取消医案
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
        /// 业务规则：设置状态为Cancelled，需要审计理由（非当天本人操作时）
        /// </summary>
        public async Task<MedicalCase?> CancelAsync(
            Guid id,
            Guid operatorId,
            bool isAdmin = false,
            string? reason = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.Cancel started - MedicalCaseId={MedicalCaseId}", id);

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
            if (!MedicalCaseRules.CanEdit(medicalCase, operatorId, isAdmin))
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → PermissionDenied - MedicalCaseId={MedicalCaseId} UserId={UserId}",
                    id, operatorId);
                throw new UnauthorizedAccessException("无权限取消此病案");
            }

            // 业务规则验证：只有Draft/Active状态可以取消
            if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → AlreadyCompleted - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("已完成的医案不可取消");
            }

            if (medicalCase.CaseStatus == MedicalCaseStatus.Cancelled)
            {
                _logger.LogWarning("[SVC] MedicalCase.Cancel → AlreadyCancelled - MedicalCaseId={MedicalCaseId}", id);
                throw new InvalidOperationException("医案已经是取消状态");
            }

            // Draft/Active 状态不受跨日限制，取消时不强制要求原因
            // 原因仅用于审计记录（可选）

            // 设置状态为Cancelled
            medicalCase.CaseStatus = MedicalCaseStatus.Cancelled;
            medicalCase.UpdatedAt = DateTime.Now;

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("[SVC] MedicalCase.Cancel completed - MedicalCaseId={MedicalCaseId}", id);

            // 记录审计日志
            var operatorInfo = await GetOperatorInfoAsync(operatorId, isAdmin);
            await _auditService.LogAsync(
                before: beforeState,
                after: result,
                operatorId: operatorId,
                operatorName: operatorInfo.Name,
                role: operatorInfo.Role,
                operationType: AuditOperationType.Cancel,
                reason: reason);

            return result;
        }

        #region Private Helper Methods

        /// <summary>
        /// 克隆医案实体用于审计比较
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId, ConsultationDate移除
        private static MedicalCase CloneMedicalCaseForAudit(MedicalCase source)
        {
            return new MedicalCase
            {
                Id = source.Id,
                PatientId = source.PatientId,
                PatientName = source.PatientName,
                UserId = source.UserId,
                DoctorName = source.DoctorName,
                CaseStatus = source.CaseStatus,
                Remark = source.Remark,
                NeedsPrescription = source.NeedsPrescription,
                IsDeleted = source.IsDeleted,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        /// <summary>
        /// 获取操作者信息用于审计日志
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        private async Task<(string Name, UserRole Role)> GetOperatorInfoAsync(Guid userId, bool isAdmin)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    return (user.RealName, user.Role);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SVC] MedicalCase.GetOperatorInfo failed - UserId={UserId}", userId);
            }

            // 回退到基本信息
            return (
                "Unknown",
                isAdmin ? UserRole.Admin : UserRole.Doctor
            );
        }

        #endregion
    }
}
