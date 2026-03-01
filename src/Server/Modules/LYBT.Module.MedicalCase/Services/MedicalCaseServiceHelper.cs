using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.DTOs.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案服务共享 Helper
    /// 提取 CommandService + StateService 的重复逻辑
    /// </summary>
    public static class MedicalCaseServiceHelper
    {
        /// <summary>
        /// 克隆医案实体用于审计比较（增强版: 含 Consultation + Prescription 字段）
        /// </summary>
        public static MedicalCase CloneMedicalCaseForAudit(MedicalCase source)
        {
            var clone = new MedicalCase
            {
                Id = source.Id,
                PatientId = source.PatientId,
                PatientName = source.PatientName,
                UserId = source.UserId,
                DoctorName = source.DoctorName,
                CaseStatus = source.CaseStatus,
                CompletedAt = source.CompletedAt,
                Remark = source.Remark,
                NeedsPrescription = source.NeedsPrescription,
                IsDeleted = source.IsDeleted,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };

            // 增强: 克隆 Consultation 关键字段
            if (source.Consultation != null)
            {
                clone.Consultation = new LYBT.Entities.Consultations.Consultation
                {
                    Id = source.Consultation.Id,
                    PresentIllness = source.Consultation.PresentIllness,
                    TongueDiagnosis = source.Consultation.TongueDiagnosis,
                    PulseDiagnosis = source.Consultation.PulseDiagnosis,
                    TcmDiagnosis = source.Consultation.TcmDiagnosis,
                    UpdatedAt = source.Consultation.UpdatedAt
                };
            }

            // 增强: 克隆 Prescription 关键字段
            if (source.Prescription != null)
            {
                clone.Prescription = new LYBT.Entities.Prescriptions.Prescription
                {
                    Id = source.Prescription.Id,
                    MedicalCaseId = source.Prescription.MedicalCaseId,
                    DosageCount = source.Prescription.DosageCount,
                    Discount = source.Prescription.Discount,
                    Advice = source.Prescription.Advice,
                    ReferencedFormulas = source.Prescription.ReferencedFormulas,
                    IsDeleted = source.Prescription.IsDeleted,
                    UpdatedAt = source.Prescription.UpdatedAt
                };
            }

            return clone;
        }

        /// <summary>
        /// 获取操作者信息用于审计日志
        /// D5-1: 从 IUserRepository 迁移到 IUserCrossModuleService
        /// </summary>
        public static async Task<(string Name, UserRole Role)> GetOperatorInfoAsync(
            IUserCrossModuleService userCrossModule,
            Guid userId,
            bool isAdmin,
            ILogger? logger = null)
        {
            try
            {
                var user = await userCrossModule.GetUserBasicInfoAsync(userId);
                if (user != null)
                {
                    return (user.RealName, user.Role);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "[SVC] MedicalCase.GetOperatorInfo failed - UserId={UserId}", userId);
            }

            // 回退到基本信息
            return (
                "Unknown",
                isAdmin ? UserRole.Admin : UserRole.Doctor
            );
        }

        /// <summary>
        /// 验证创建医案的前置条件，返回 Patient 和 Doctor 基本信息
        /// D5-1: 从 IPatientRepository/IUserRepository 迁移到 ISP 接口，返回 DTO 替代 Entity
        /// </summary>
        public static async Task<(PatientBasicDto Patient, UserBasicDto Doctor)> ValidateAndFetchCreationContextAsync(
            Guid patientId,
            Guid doctorId,
            IPatientCrossModuleService patientCrossModule,
            IUserCrossModuleService userCrossModule,
            IMedicalCaseRepository medicalCaseRepository,
            ILogger logger)
        {
            if (doctorId == Guid.Empty)
            {
                logger.LogWarning("[SVC] MedicalCase.Create -> ValidationFailed - DoctorIdEmpty");
                throw new ArgumentException("DoctorId/UserId 不能为空");
            }

            var patient = await patientCrossModule.GetPatientBasicInfoAsync(patientId)
                ?? throw new InvalidOperationException($"患者不存在，PatientId: {patientId}");

            // T5-P2-09: 检查患者状态
            if (patient.Status != CommonStatus.Enabled)
            {
                logger.LogWarning("[SVC] MedicalCase.Create -> PatientDisabled - PatientId={PatientId} Status={Status}",
                    patientId, patient.Status);
                throw new BusinessException(EC.McPatientDisabled, "该患者已被禁用，无法创建医案");
            }

            var doctor = await userCrossModule.GetUserBasicInfoAsync(doctorId)
                ?? throw new InvalidOperationException($"医生不存在，DoctorId: {doctorId}");

            // BR-001: 单患者仅一条未完成医案
            var existingCases = await medicalCaseRepository.GetByPatientIdAsync(patientId);
            if (!MedicalCaseRules.CanCreateNewCase(existingCases))
            {
                if (MedicalCaseRules.HasActiveCase(existingCases))
                {
                    var activeCase = existingCases.FirstOrDefault(c => c.CaseStatus == MedicalCaseStatus.Active);
                    logger.LogWarning("[SVC] MedicalCase -> ActiveCaseExists - PatientId={PatientId} CaseId={CaseId}",
                        patientId, activeCase?.Id);
                    throw new InvalidOperationException("该患者已有进行中的医案，请先完成现有医案");
                }

                if (MedicalCaseRules.HasSuspendedCase(existingCases))
                {
                    var suspendedCase = existingCases.FirstOrDefault(c => c.CaseStatus == MedicalCaseStatus.Suspended);
                    logger.LogWarning("[SVC] MedicalCase -> SuspendedCaseExists - PatientId={PatientId} CaseId={CaseId}",
                        patientId, suspendedCase?.Id);
                    throw new InvalidOperationException("该患者已有暂存的医案，请先处理现有医案（继续或关闭）");
                }
            }

            return (patient, doctor);
        }

        /// <summary>
        /// 带并发重试的操作执行器
        /// 处理 EF Core DbUpdateConcurrencyException 和 Repository 层 "数据已被其他用户修改" 异常
        /// </summary>
        public static async Task<T> ExecuteWithConcurrencyRetryAsync<T>(
            Func<Task<T>> action,
            string operationName,
            ILogger logger,
            int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex) when (attempt < maxRetries)
                {
                    logger.LogWarning(ex, "[SVC] MedicalCase.{Operation} -> ConcurrencyRetry - Attempt={Attempt}",
                        operationName, attempt);
                    await Task.Delay(100 * attempt);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("数据已被其他用户修改") && attempt < maxRetries)
                {
                    logger.LogWarning("[SVC] MedicalCase.{Operation} -> ConcurrencyRetry - Attempt={Attempt}",
                        operationName, attempt);
                    await Task.Delay(100 * attempt);
                }
            }

            logger.LogError("[SVC] MedicalCase.{Operation} -> MaxRetriesExceeded", operationName);
            throw new InvalidOperationException($"{operationName}失败，请稍后重试");
        }

        /// <summary>
        /// 权限验证 helper: 检查编辑权限，失败时记录日志并抛出异常
        /// </summary>
        public static void EnsureCanEdit(
            IMedicalCasePermissionService permissionService,
            MedicalCase medicalCase,
            Guid userId,
            bool isAdmin,
            string operation,
            ILogger logger)
        {
            if (permissionService.CanEdit(userId, isAdmin, medicalCase)) return;

            var reason = isAdmin ? "权限不足" :
                (medicalCase.UserId != userId ? "非创建医生" : $"医案状态为{medicalCase.CaseStatus}");

            logger.LogWarning("[SVC] MedicalCase.{Operation} -> PermissionDenied - MedicalCaseId={MedicalCaseId} UserId={UserId} Reason={Reason}",
                operation, medicalCase.Id, userId, reason);
            throw new UnauthorizedAccessException($"无权限编辑此医案：{reason}");
        }

        /// <summary>
        /// 删除权限验证 helper
        /// </summary>
        public static void EnsureCanDelete(
            IMedicalCasePermissionService permissionService,
            MedicalCase medicalCase,
            Guid userId,
            bool isAdmin,
            string operation,
            ILogger logger)
        {
            if (permissionService.CanDelete(userId, isAdmin, medicalCase)) return;

            logger.LogWarning("[SVC] MedicalCase.{Operation} -> PermissionDenied - MedicalCaseId={MedicalCaseId} UserId={UserId}",
                operation, medicalCase.Id, userId);
            throw new UnauthorizedAccessException($"无权限执行此操作");
        }
    }
}
