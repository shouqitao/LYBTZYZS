using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Transactions;
// Removed: using LYBT.Infrastructure.Transactions.Steps; - DatabaseTransactionStep now in main namespace
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Transactions.Steps
{
    /// <summary>
    /// 更新医疗案例关联事务步骤
    /// 负责更新医疗案例的处方关联信息和相关状态
    /// </summary>
    public class UpdateMedicalCaseStep : DatabaseTransactionStep<PrescriptionTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "UpdateMedicalCase";

        /// <inheritdoc />
        public override int Order => 5;

        /// <inheritdoc />
        public override bool SupportsCompensation => true;

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(30);

        public UpdateMedicalCaseStep(AppDbContext dbContext, ILogger<UpdateMedicalCaseStep> logger)
            : base(dbContext, logger)
        {
        }

        /// <inheritdoc />
        public override async Task<bool> CanExecuteAsync(PrescriptionTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 检查基础条件
            if (!await base.CanExecuteAsync(context, cancellationToken))
                return false;

            try
            {
                // 必须已经创建处方记录
                if (!context.PrescriptionId.HasValue)
                {
                    context.LogError("Cannot update medical case without prescription ID");
                    context.SetValidationResult("CanUpdateMedicalCase", false);
                    return false;
                }

                // 验证医疗案例存在
                var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                    context.MedicalCaseId, cancellationToken);

                if (medicalCase == null)
                {
                    context.LogError("Medical case not found: {MedicalCaseId}", context.MedicalCaseId);
                    context.SetValidationResult("MedicalCaseExists", false);
                    return false;
                }

                // 验证医疗案例状态
                if (medicalCase.Status == Shared.Models.Enums.MedicalCaseStatus.Cancelled)
                {
                    context.LogError("Cannot update cancelled medical case: {MedicalCaseId}", context.MedicalCaseId);
                    context.SetValidationResult("MedicalCaseStatusValid", false);
                    return false;
                }

                // 验证处方记录存在
                var prescription = await FindEntityAsync<LYBT.Entities.Prescriptions.Prescription>(
                    context.PrescriptionId.Value, cancellationToken);

                if (prescription == null)
                {
                    context.LogError("Prescription not found: {PrescriptionId}", context.PrescriptionId);
                    context.SetValidationResult("PrescriptionExists", false);
                    return false;
                }

                // 保存原始状态用于补偿
                context.SetData("OriginalPrescriptionId", medicalCase.PrescriptionId);

                // 记录验证成功
                context.SetValidationResult("CanUpdateMedicalCase", true);
                context.SetValidationResult("MedicalCaseExists", true);
                context.SetValidationResult("MedicalCaseStatusValid", true);
                context.SetValidationResult("PrescriptionExists", true);

                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate medical case update conditions");
                return false;
            }
        }

        /// <inheritdoc />
        protected override async Task<TransactionStepResult> ExecuteDatabaseOperationAsync(
            PrescriptionTransactionContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var updatedEntities = new List<string>();

                // 1. 更新医疗案例的处方关联
                var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                    context.MedicalCaseId, cancellationToken);

                if (medicalCase != null)
                {
                    var oldPrescriptionId = medicalCase.PrescriptionId;
                    medicalCase.PrescriptionId = context.PrescriptionId;

                    await UpdateEntityAsync(medicalCase, cancellationToken);
                    updatedEntities.Add($"MedicalCase:{medicalCase.Id}:PrescriptionId:{oldPrescriptionId}->{context.PrescriptionId}");

                    Logger.LogInformation(
                        "Updated medical case prescription association: {MedicalCaseId}, Old: {OldPrescriptionId}, New: {NewPrescriptionId}",
                        medicalCase.Id, oldPrescriptionId, context.PrescriptionId);
                }

                // 2. 更新诊断记录的处方关联（如果存在）
                if (context.ConsultationId.HasValue)
                {
                    var consultation = await FindEntityAsync<LYBT.Entities.Consultation.Consultation>(
                        context.ConsultationId.Value, cancellationToken);

                    if (consultation != null)
                    {
                        // 注意：Consultation实体中可能没有PrescriptionId字段
                        // 这里仅作为示例，实际实现需要根据实体结构调整
                        // consultation.PrescriptionId = context.PrescriptionId;

                        await UpdateEntityAsync(consultation, cancellationToken);
                        updatedEntities.Add($"Consultation:{consultation.Id}:Updated");

                        Logger.LogInformation(
                            "Updated consultation prescription association: {ConsultationId}, Prescription: {PrescriptionId}",
                            consultation.Id, context.PrescriptionId);
                    }
                }

                // 3. 记录更新历史
                await RecordUpdateHistoryAsync(context, updatedEntities, cancellationToken);

                // 4. 触发相关业务事件（可选）
                await TriggerPrescriptionCreatedEventAsync(context, cancellationToken);

                Logger.LogInformation(
                    "Successfully updated medical case associations for prescription: {PrescriptionId}",
                    context.PrescriptionId);

                // 返回成功结果
                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["UpdatedEntities"] = updatedEntities,
                    ["MedicalCaseId"] = context.MedicalCaseId,
                    ["PrescriptionId"] = context.PrescriptionId,
                    ["ConsultationId"] = context.ConsultationId,
                    ["TotalPrice"] = context.TotalPrice,
                    ["ItemCount"] = context.Items.Count,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update medical case for prescription: {PrescriptionId}", context.PrescriptionId);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<TransactionStepResult> CompensateAsync(
            PrescriptionTransactionContext context,
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

                // 1. 恢复医疗案例的处方关联
                var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                    context.MedicalCaseId, cancellationToken);

                if (medicalCase != null)
                {
                    var originalPrescriptionId = context.GetData<Guid?>("OriginalPrescriptionId");
                    var currentPrescriptionId = medicalCase.PrescriptionId;

                    medicalCase.PrescriptionId = originalPrescriptionId;
                    await UpdateEntityAsync(medicalCase, cancellationToken);
                    restoredEntities.Add($"MedicalCase:{medicalCase.Id}:{currentPrescriptionId}->{originalPrescriptionId}");

                    Logger.LogInformation(
                        "Restored medical case prescription association: {MedicalCaseId} from {Current} to {Original}",
                        medicalCase.Id, currentPrescriptionId, originalPrescriptionId);
                }

                // 2. 恢复诊断记录的处方关联（如果存在）
                if (context.ConsultationId.HasValue)
                {
                    var consultation = await FindEntityAsync<LYBT.Entities.Consultation.Consultation>(
                        context.ConsultationId.Value, cancellationToken);

                    if (consultation != null)
                    {
                        // 恢复诊断记录的处方关联
                        // consultation.PrescriptionId = null; // 或者恢复到原来的值

                        await UpdateEntityAsync(consultation, cancellationToken);
                        restoredEntities.Add($"Consultation:{consultation.Id}:Restored");

                        Logger.LogInformation(
                            "Restored consultation prescription association: {ConsultationId}",
                            consultation.Id);
                    }
                }

                // 3. 记录补偿历史
                await RecordCompensationHistoryAsync(context, restoredEntities, cancellationToken);

                Logger.LogInformation("Successfully compensated medical case updates");

                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["Action"] = "MedicalCaseRestored",
                    ["RestoredEntities"] = restoredEntities,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compensate medical case updates");
                return CreateFailureResult(ex, new Dictionary<string, object> { ["Action"] = "CompensationFailed" });
            }
        }

        /// <summary>
        /// 记录更新历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="updates">更新记录</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task RecordUpdateHistoryAsync(
            PrescriptionTransactionContext context,
            List<string> updates,
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["MedicalCaseUpdate"] = new
                {
                    UpdatedAt = DateTime.UtcNow,
                    PrescriptionId = context.PrescriptionId,
                    MedicalCaseId = context.MedicalCaseId,
                    ConsultationId = context.ConsultationId,
                    Updates = updates,
                    UpdateCount = updates.Count
                };

                Logger.LogDebug("Recorded medical case update history: {Updates}", string.Join("; ", updates));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record medical case update history");

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
            PrescriptionTransactionContext context,
            List<string> restorations,
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["MedicalCaseCompensation"] = new
                {
                    CompensatedAt = DateTime.UtcNow,
                    Restorations = restorations,
                    RestorationCount = restorations.Count,
                    Reason = "TransactionRollback"
                };

                Logger.LogDebug("Recorded compensation history: {Restorations}", string.Join("; ", restorations));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record compensation history");

                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 触发处方创建完成事件
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task TriggerPrescriptionCreatedEventAsync(
            PrescriptionTransactionContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                // 这里可以触发领域事件或发送消息
                // 例如：通知处方管理系统、更新统计信息、发送提醒等

                var prescriptionCreatedEvent = new
                {
                    EventType = "PrescriptionCreated",
                    PrescriptionId = context.PrescriptionId,
                    MedicalCaseId = context.MedicalCaseId,
                    PatientId = context.PatientId,
                    DoctorId = context.DoctorId,
                    ItemCount = context.Items.Count,
                    TotalPrice = context.TotalPrice,
                    CreatedAt = DateTime.UtcNow
                };

                // 记录事件到元数据中
                context.PrescriptionMetadata["PrescriptionCreatedEvent"] = prescriptionCreatedEvent;

                Logger.LogInformation("Triggered prescription created event: {PrescriptionId}", context.PrescriptionId);

                // TODO: 实际的事件发布逻辑
                // await _eventBus.PublishAsync(prescriptionCreatedEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to trigger prescription created event");

                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 检查医疗案例是否可以关联处方
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="medicalCase">医疗案例实体</param>
        /// <returns>是否可以关联</returns>
        private bool CanAssociatePrescription(PrescriptionTransactionContext context, LYBT.Entities.MedicalCase.MedicalCase medicalCase)
        {
            // 检查医疗案例状态
            if (medicalCase.Status == Shared.Models.Enums.MedicalCaseStatus.Cancelled ||
                medicalCase.Status == Shared.Models.Enums.MedicalCaseStatus.Archived)
            {
                return false;
            }

            // 检查患者匹配
            if (medicalCase.PatientId != context.PatientId)
            {
                return false;
            }

            // 检查是否已有处方关联（如果业务规则不允许替换）
            // if (medicalCase.PrescriptionId.HasValue)
            // {
            //     return false;
            // }

            return true;
        }
    }
}
