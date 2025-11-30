using AutoMapper;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Utilities;
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
            IMapper mapper,
            ILogger<MedicalCaseStateService> logger)
            : base(logger, mapper)
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
            try
            {
                _logger.LogInformation("开始更新病案状态，MedicalCaseId: {MedicalCaseId}, Status: {Status}",
                    medicalCaseId, status);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 业务规则验证：状态流转合法性（Issue #1757: 使用ValidationHelper）
                if (!ValidationHelper.IsValidMedicalCaseStatusTransition(medicalCase.CaseStatus, status))
                {
                    _logger.LogWarning("非法的状态流转，从{OldStatus}到{NewStatus}",
                        medicalCase.CaseStatus, status);
                    throw new InvalidOperationException($"不允许从{medicalCase.CaseStatus}状态转换到{status}状态");
                }

                // 更新状态
                medicalCase.CaseStatus = status;
                medicalCase.UpdatedAt = DateTime.Now;

                // 保存
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("病案状态更新成功，MedicalCaseId: {MedicalCaseId}, NewStatus: {Status}",
                    medicalCaseId, status);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新病案状态失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 完成病案（三步流程最后一步）
        /// Epic #1612: 验证三步流程完整性后标记为Completed
        /// 业务规则：BF-002（三步流程验证）
        /// </summary>
        public async Task<MedicalCase?> CompleteAsync(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("开始完成病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 业务规则验证：处方需求标记
                if (medicalCase.NeedsPrescription == null)
                {
                    _logger.LogWarning("未标记处方需求，无法完成病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    throw new InvalidOperationException("请先标记是否需要开处方");
                }

                // 如果标记需要开处方，验证处方存在
                if (medicalCase.NeedsPrescription == true)
                {
                    if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
                    {
                        _logger.LogWarning("已标记需要开处方但处方不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                        throw new InvalidOperationException("已标记需要开处方，但处方不存在，无法完成病案");
                    }
                }

                // 更新状态为Completed
                medicalCase.CaseStatus = MedicalCaseStatus.Completed;
                medicalCase.UpdatedAt = DateTime.Now;

                // 保存
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("病案完成成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成病案失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        public async Task<bool> CloseCaseAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("开始关闭病案，MedicalCaseId: {MedicalCaseId}", id);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", id);
                    return false;
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

                _logger.LogInformation("病案关闭成功，MedicalCaseId: {MedicalCaseId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭病案失败，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
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
            try
            {
                _logger.LogInformation("开始暂存医案，MedicalCaseId: {MedicalCaseId}", id);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", id);
                    return null;
                }

                // 保存变更前的状态用于审计
                var beforeState = CloneMedicalCaseForAudit(medicalCase);

                // 权限检查
                if (!MedicalCaseRules.CanEdit(medicalCase, operatorId, isAdmin))
                {
                    _logger.LogWarning("无权限编辑病案，MedicalCaseId: {MedicalCaseId}, UserId: {UserId}",
                        id, operatorId);
                    throw new UnauthorizedAccessException("无权限编辑此病案");
                }

                // 业务规则验证：只有Draft/Active状态可以暂存
                if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
                {
                    _logger.LogWarning("已完成的医案不可暂存，MedicalCaseId: {MedicalCaseId}", id);
                    throw new InvalidOperationException("已完成的医案不可暂存");
                }

                if (medicalCase.CaseStatus == MedicalCaseStatus.Cancelled)
                {
                    _logger.LogWarning("已取消的医案不可暂存，MedicalCaseId: {MedicalCaseId}", id);
                    throw new InvalidOperationException("已取消的医案不可暂存");
                }

                // 如果提供了诊断信息，更新Consultation
                if (request != null && medicalCase.Consultation != null)
                {
                    medicalCase.Consultation.ChiefComplaint = request.ChiefComplaint;
                    medicalCase.Consultation.PresentIllness = request.PresentIllness;
                    medicalCase.Consultation.Inspection = request.Inspection;
                    medicalCase.Consultation.AuscultationOlfaction = request.AuscultationOlfaction;
                    medicalCase.Consultation.Inquiry = request.Inquiry;
                    medicalCase.Consultation.Palpation = request.Palpation;
                    medicalCase.Consultation.TCMDiagnosis = request.TCMDiagnosis;
                    medicalCase.Consultation.TreatmentPrinciple = request.TreatmentPrinciple;
                    medicalCase.Consultation.MedicalAdvice = request.MedicalAdvice;
                    medicalCase.Remark = request.MedicalCaseRemark;
                    medicalCase.Consultation.UpdatedAt = DateTime.Now;
                }

                // 设置状态为Draft
                medicalCase.CaseStatus = MedicalCaseStatus.Draft;
                medicalCase.UpdatedAt = DateTime.Now;

                // 保存
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("医案暂存成功，MedicalCaseId: {MedicalCaseId}", id);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂存医案失败，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
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
            try
            {
                _logger.LogInformation("开始取消医案，MedicalCaseId: {MedicalCaseId}", id);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", id);
                    return null;
                }

                // 保存变更前的状态用于审计
                var beforeState = CloneMedicalCaseForAudit(medicalCase);

                // 权限检查
                if (!MedicalCaseRules.CanEdit(medicalCase, operatorId, isAdmin))
                {
                    _logger.LogWarning("无权限取消病案，MedicalCaseId: {MedicalCaseId}, UserId: {UserId}",
                        id, operatorId);
                    throw new UnauthorizedAccessException("无权限取消此病案");
                }

                // 业务规则验证：只有Draft/Active状态可以取消
                if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
                {
                    _logger.LogWarning("已完成的医案不可取消，MedicalCaseId: {MedicalCaseId}", id);
                    throw new InvalidOperationException("已完成的医案不可取消");
                }

                if (medicalCase.CaseStatus == MedicalCaseStatus.Cancelled)
                {
                    _logger.LogWarning("医案已经是取消状态，MedicalCaseId: {MedicalCaseId}", id);
                    throw new InvalidOperationException("医案已经是取消状态");
                }

                // 检查是否需要审计理由（非当天本人操作时）
                var requiresAuditReason = !MedicalCaseRules.IsSameDayByCreator(medicalCase, operatorId);
                if (requiresAuditReason && string.IsNullOrWhiteSpace(reason))
                {
                    _logger.LogWarning("取消医案需要提供原因，MedicalCaseId: {MedicalCaseId}", id);
                    throw new InvalidOperationException("取消非当天本人创建的医案需要提供原因");
                }

                // 设置状态为Cancelled
                medicalCase.CaseStatus = MedicalCaseStatus.Cancelled;
                medicalCase.UpdatedAt = DateTime.Now;

                // 保存
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("医案取消成功，MedicalCaseId: {MedicalCaseId}", id);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消医案失败，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }

        #region BaseService抽象方法实现

        /// <summary>
        /// 获取MedicalCase实体ID
        /// </summary>
        protected override Guid GetEntityId<TEntity>(TEntity entity) where TEntity : class
        {
            return entity switch
            {
                MedicalCase medicalCase => medicalCase.Id,
                _ => throw new ArgumentException($"不支持的实体类型: {typeof(TEntity).Name}")
            };
        }

        /// <summary>
        /// 获取MedicalCase创建用户ID
        /// </summary>
        protected override Guid GetCreatedUserId<TEntity>(TEntity entity) where TEntity : class
        {
            return entity switch
            {
                MedicalCase medicalCase => medicalCase.CreatedBy ?? Guid.Empty,
                _ => throw new ArgumentException($"不支持的实体类型: {typeof(TEntity).Name}")
            };
        }

        /// <summary>
        /// 获取MedicalCase创建时间
        /// </summary>
        protected override DateTime GetCreatedDate<TEntity>(TEntity entity) where TEntity : class
        {
            return entity switch
            {
                MedicalCase medicalCase => medicalCase.CreatedAt,
                _ => throw new ArgumentException($"不支持的实体类型: {typeof(TEntity).Name}")
            };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 克隆医案实体用于审计比较
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        private static MedicalCase CloneMedicalCaseForAudit(MedicalCase source)
        {
            return new MedicalCase
            {
                Id = source.Id,
                PatientId = source.PatientId,
                PatientName = source.PatientName,
                DoctorId = source.DoctorId,
                DoctorName = source.DoctorName,
                ConsultationDate = source.ConsultationDate,
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
                _logger.LogWarning(ex, "获取操作者信息失败，UserId: {UserId}", userId);
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
