using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Transactions;
// Removed: using LYBT.Infrastructure.Transactions.Steps; - DatabaseTransactionStep now in main namespace
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Transactions.Steps
{
    /// <summary>
    /// 更新患者状态事务步骤
    /// 负责更新患者相关状态和医疗案例状态，完成诊疗流程的状态管理
    /// </summary>
    public class UpdatePatientStatusStep : DatabaseTransactionStep<ConsultationTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "UpdatePatientStatus";

        /// <inheritdoc />
        public override int Order => 3;

        /// <inheritdoc />
        public override bool SupportsCompensation => true;

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(30);

        public UpdatePatientStatusStep(AppDbContext dbContext, ILogger<UpdatePatientStatusStep> logger)
            : base(dbContext, logger)
        {
        }

        /// <inheritdoc />
        public override async Task<bool> CanExecuteAsync(ConsultationTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 检查基础条件
            if (!await base.CanExecuteAsync(context, cancellationToken))
                return false;

            try
            {
                // 必须已经创建医疗案例和诊断记录
                if (!context.MedicalCaseId.HasValue)
                {
                    context.LogError("Cannot update patient status without medical case ID");
                    return false;
                }

                if (!context.ConsultationId.HasValue)
                {
                    context.LogError("Cannot update patient status without consultation ID");
                    return false;
                }

                // 验证患者存在
                var patient = await FindEntityAsync<LYBT.Entities.Patients.Patient>(context.PatientId, cancellationToken);
                if (patient == null)
                {
                    context.LogError("Patient not found: {PatientId}", context.PatientId);
                    return false;
                }

                // 验证医疗案例存在且状态正确
                var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                    context.MedicalCaseId.Value, cancellationToken);

                if (medicalCase == null)
                {
                    context.LogError("Medical case not found: {MedicalCaseId}", context.MedicalCaseId);
                    return false;
                }

                // 保存原始状态用于补偿
                context.OriginalPatientStatus = patient.Status.ToString();
                context.SetData("OriginalMedicalCaseStatus", medicalCase.Status);

                // 验证状态转换是否合法
                if (!IsValidStatusTransition(medicalCase.Status, MedicalCaseStatus.InConsultation))
                {
                    context.LogWarning(
                        "Invalid medical case status transition: {CurrentStatus} -> {TargetStatus}",
                        medicalCase.Status, MedicalCaseStatus.InConsultation);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate patient status update conditions");
                return false;
            }
        }

        /// <inheritdoc />
        protected override async Task<TransactionStepResult> ExecuteDatabaseOperationAsync(
            ConsultationTransactionContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var updatedEntities = new List<string>();

                // 1. 更新医疗案例状态
                var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                    context.MedicalCaseId!.Value, cancellationToken);

                if (medicalCase != null)
                {
                    var oldStatus = medicalCase.Status;
                    medicalCase.Status = MedicalCaseStatus.InConsultation;

                    await UpdateEntityAsync(medicalCase, cancellationToken);
                    context.MedicalCaseStatus = medicalCase.Status;
                    updatedEntities.Add($"MedicalCase:{medicalCase.Id}:{oldStatus}->{medicalCase.Status}");

                    Logger.LogInformation(
                        "Updated medical case status: {MedicalCaseId} from {OldStatus} to {NewStatus}",
                        medicalCase.Id, oldStatus, medicalCase.Status);
                }

                // 2. 更新患者状态（如果需要）
                var patient = await FindEntityAsync<LYBT.Entities.Patients.Patient>(context.PatientId, cancellationToken);
                if (patient != null)
                {
                    var oldPatientStatus = patient.Status;

                    // 根据业务规则更新患者状态
                    var newPatientStatus = DeterminePatientStatus(context, patient);
                    if (newPatientStatus != oldPatientStatus)
                    {
                        patient.Status = newPatientStatus;
                        await UpdateEntityAsync(patient, cancellationToken);
                        updatedEntities.Add($"Patient:{patient.Id}:{oldPatientStatus}->{newPatientStatus}");

                        Logger.LogInformation(
                            "Updated patient status: {PatientId} from {OldStatus} to {NewStatus}",
                            patient.Id, oldPatientStatus, newPatientStatus);
                    }
                }

                // 3. 记录状态变更历史（可选）
                await RecordStatusChangeHistoryAsync(context, updatedEntities, cancellationToken);

                // 返回成功结果
                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["UpdatedEntities"] = updatedEntities,
                    ["MedicalCaseStatus"] = context.MedicalCaseStatus?.ToString(),
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update patient status for medical case: {MedicalCaseId}", context.MedicalCaseId);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<TransactionStepResult> CompensateAsync(
            ConsultationTransactionContext context,
            TransactionStepResult originalResult,
            CancellationToken cancellationToken = default)
        {
            if (!SupportsCompensation)
            {
                return await base.CompensateAsync(context, originalResult, cancellationToken);
            }

            try
            {
                var restoredEntities = new List<string>();

                // 1. 恢复医疗案例状态
                if (context.MedicalCaseId.HasValue)
                {
                    var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                        context.MedicalCaseId.Value, cancellationToken);

                    if (medicalCase != null)
                    {
                        var originalStatus = context.GetData<MedicalCaseStatus>("OriginalMedicalCaseStatus");
                        if (originalStatus != default(MedicalCaseStatus))
                        {
                            var currentStatus = medicalCase.Status;
                            medicalCase.Status = originalStatus;
                            await UpdateEntityAsync(medicalCase, cancellationToken);
                            restoredEntities.Add($"MedicalCase:{medicalCase.Id}:{currentStatus}->{originalStatus}");

                            Logger.LogInformation(
                                "Restored medical case status: {MedicalCaseId} from {CurrentStatus} to {OriginalStatus}",
                                medicalCase.Id, currentStatus, originalStatus);
                        }
                    }
                }

                // 2. 恢复患者状态
                if (!string.IsNullOrEmpty(context.OriginalPatientStatus))
                {
                    var patient = await FindEntityAsync<LYBT.Entities.Patients.Patient>(context.PatientId, cancellationToken);
                    if (patient != null)
                    {
                        if (Enum.TryParse<CommonStatus>(context.OriginalPatientStatus, out var originalStatus))
                        {
                            var currentStatus = patient.Status;
                            patient.Status = originalStatus;
                            await UpdateEntityAsync(patient, cancellationToken);
                            restoredEntities.Add($"Patient:{patient.Id}:{currentStatus}->{originalStatus}");

                            Logger.LogInformation(
                                "Restored patient status: {PatientId} from {CurrentStatus} to {OriginalStatus}",
                                patient.Id, currentStatus, originalStatus);
                        }
                    }
                }

                // 3. 记录补偿操作历史
                await RecordCompensationHistoryAsync(context, restoredEntities, cancellationToken);

                Logger.LogInformation("Successfully compensated patient status updates");

                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["Action"] = "StatusRestored",
                    ["RestoredEntities"] = restoredEntities,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compensate patient status updates");
                return CreateFailureResult(ex, new Dictionary<string, object> { ["Action"] = "CompensationFailed" });
            }
        }

        /// <summary>
        /// 验证状态转换是否合法
        /// </summary>
        /// <param name="currentStatus">当前状态</param>
        /// <param name="targetStatus">目标状态</param>
        /// <returns>是否为合法转换</returns>
        private bool IsValidStatusTransition(MedicalCaseStatus currentStatus, MedicalCaseStatus targetStatus)
        {
            // 定义合法的状态转换规则
            return currentStatus switch
            {
                MedicalCaseStatus.Registered => targetStatus == MedicalCaseStatus.InConsultation ||
                                              targetStatus == MedicalCaseStatus.Cancelled,
                MedicalCaseStatus.InConsultation => targetStatus == MedicalCaseStatus.Completed ||
                                                   targetStatus == MedicalCaseStatus.Suspended ||
                                                   targetStatus == MedicalCaseStatus.Cancelled,
                MedicalCaseStatus.Suspended => targetStatus == MedicalCaseStatus.InConsultation ||
                                              targetStatus == MedicalCaseStatus.Cancelled,
                MedicalCaseStatus.Completed => targetStatus == MedicalCaseStatus.Archived,
                MedicalCaseStatus.Cancelled => false, // 取消后不能转换到其他状态
                MedicalCaseStatus.Archived => false, // 归档后不能转换到其他状态
                _ => false
            };
        }

        /// <summary>
        /// 根据业务规则确定患者状态
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="patient">患者实体</param>
        /// <returns>新的患者状态</returns>
        private CommonStatus DeterminePatientStatus(ConsultationTransactionContext context, LYBT.Entities.Patients.Patient patient)
        {
            // 如果是急诊，可能需要特殊处理
            if (context.IsEmergency)
            {
                return CommonStatus.Enabled; // 或者定义特殊的急诊状态
            }

            // 正常情况下，患者状态保持启用状态
            return CommonStatus.Enabled;
        }

        /// <summary>
        /// 记录状态变更历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="changes">变更记录</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task RecordStatusChangeHistoryAsync(
            ConsultationTransactionContext context,
            List<string> changes,
            CancellationToken cancellationToken)
        {
            try
            {
                // 这里可以记录到专门的状态变更日志表
                // 简化实现：记录到事务元数据中
                context.ConsultationMetadata["StatusChanges"] = changes;
                context.ConsultationMetadata["StatusChangeTimestamp"] = DateTime.UtcNow;

                Logger.LogDebug("Recorded status change history: {Changes}", string.Join("; ", changes));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record status change history");

                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 记录补偿操作历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="restorations">恢复记录</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task RecordCompensationHistoryAsync(
            ConsultationTransactionContext context,
            List<string> restorations,
            CancellationToken cancellationToken)
        {
            try
            {
                context.ConsultationMetadata["StatusCompensations"] = restorations;
                context.ConsultationMetadata["CompensationTimestamp"] = DateTime.UtcNow;

                Logger.LogDebug("Recorded compensation history: {Restorations}", string.Join("; ", restorations));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record compensation history");

                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 检查患者是否有其他活跃的医疗案例
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="excludeMedicalCaseId">排除的医疗案例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有其他活跃案例</returns>
        private async Task<bool> HasOtherActiveMedicalCasesAsync(
            Guid patientId,
            Guid excludeMedicalCaseId,
            CancellationToken cancellationToken)
        {
            return await DbContext.Set<LYBT.Entities.MedicalCase.MedicalCase>()
                .AnyAsync(
                    mc => mc.PatientId == patientId &&
                              mc.Id != excludeMedicalCaseId &&
                              mc.Status != MedicalCaseStatus.Completed &&
                              mc.Status != MedicalCaseStatus.Cancelled &&
                              mc.Status != MedicalCaseStatus.Archived, cancellationToken);
        }
    }
}
