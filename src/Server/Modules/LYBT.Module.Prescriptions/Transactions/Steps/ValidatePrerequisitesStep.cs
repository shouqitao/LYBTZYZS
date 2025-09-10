using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Transactions;

// Removed: using LYBT.Infrastructure.Transactions.Steps; - DatabaseTransactionStep now in main namespace
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Transactions.Steps
{
    /// <summary>
    /// 验证先决条件事务步骤
    /// 负责验证患者、医生、医疗案例等先决条件的存在性和有效性
    /// </summary>
    public class ValidatePrerequisitesStep : DatabaseTransactionStep<PrescriptionTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "ValidatePrerequisites";

        /// <inheritdoc />
        public override int Order => 1;

        /// <inheritdoc />
        public override bool SupportsCompensation => false; // 验证步骤无需补偿

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(30);

        public ValidatePrerequisitesStep(AppDbContext dbContext, ILogger<ValidatePrerequisitesStep> logger)
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
                // 验证上下文数据完整性
                var (isValid, errors) = context.ValidateContext();
                if (!isValid)
                {
                    context.LogError("Context validation failed: {Errors}", string.Join(", ", errors));
                    context.SetValidationResult("ContextValidation", new { IsValid = false, Errors = errors });
                    return false;
                }

                context.SetValidationResult("ContextValidation", new { IsValid = true });
                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate prerequisites conditions");
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
                var validationResults = new List<string>();

                // 1. 验证患者存在且状态正常
                var patient = await FindEntityAsync<LYBT.Entities.Patients.Patient>(context.PatientId, cancellationToken);
                if (patient == null)
                {
                    context.LogError("Patient not found: {PatientId}", context.PatientId);
                    context.SetValidationResult("PatientExists", false);
                    return CreateFailureResult(new InvalidOperationException("患者不存在"));
                }

                if (patient.Status != CommonStatus.Enabled)
                {
                    context.LogError("Patient is not active: {PatientId}, Status: {Status}", context.PatientId, patient.Status);
                    context.SetValidationResult("PatientActive", false);
                    return CreateFailureResult(new InvalidOperationException("患者状态异常，无法开具处方"));
                }

                context.PatientName = patient.Name;
                validationResults.Add($"Patient:{patient.Id}:OK");
                context.SetValidationResult("PatientExists", true);
                context.SetValidationResult("PatientActive", true);

                // 2. 验证医生存在且有权限开具处方
                var doctor = await FindEntityAsync<LYBT.Entities.Users.User>(context.DoctorId, cancellationToken);
                if (doctor == null)
                {
                    context.LogError("Doctor not found: {DoctorId}", context.DoctorId);
                    context.SetValidationResult("DoctorExists", false);
                    return CreateFailureResult(new InvalidOperationException("医生不存在"));
                }

                if (doctor.Role != UserRole.Doctor && doctor.Role != UserRole.Admin)
                {
                    context.LogError("User does not have prescription privileges: {DoctorId}, Role: {Role}", context.DoctorId, doctor.Role);
                    context.SetValidationResult("DoctorHasPrivileges", false);
                    return CreateFailureResult(new InvalidOperationException("用户没有开具处方的权限"));
                }

                if (doctor.Status != CommonStatus.Enabled)
                {
                    context.LogError("Doctor is not active: {DoctorId}, Status: {Status}", context.DoctorId, doctor.Status);
                    context.SetValidationResult("DoctorActive", false);
                    return CreateFailureResult(new InvalidOperationException("医生状态异常，无法开具处方"));
                }

                context.DoctorName = doctor.Name;
                validationResults.Add($"Doctor:{doctor.Id}:OK");
                context.SetValidationResult("DoctorExists", true);
                context.SetValidationResult("DoctorHasPrivileges", true);
                context.SetValidationResult("DoctorActive", true);

                // 3. 验证医疗案例存在且状态允许开具处方
                var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                    context.MedicalCaseId, cancellationToken);

                if (medicalCase == null)
                {
                    context.LogError("Medical case not found: {MedicalCaseId}", context.MedicalCaseId);
                    context.SetValidationResult("MedicalCaseExists", false);
                    return CreateFailureResult(new InvalidOperationException("医疗案例不存在"));
                }

                // 验证患者匹配
                if (medicalCase.PatientId != context.PatientId)
                {
                    context.LogError(
                        "Medical case patient mismatch: Case.PatientId={CasePatientId}, Context.PatientId={ContextPatientId}",
                        medicalCase.PatientId, context.PatientId);
                    context.SetValidationResult("MedicalCasePatientMatch", false);
                    return CreateFailureResult(new InvalidOperationException("医疗案例与患者不匹配"));
                }

                // 验证医疗案例状态
                if (medicalCase.Status == MedicalCaseStatus.Cancelled)
                {
                    context.LogError("Cannot create prescription for cancelled medical case: {MedicalCaseId}", context.MedicalCaseId);
                    context.SetValidationResult("MedicalCaseStatusValid", false);
                    return CreateFailureResult(new InvalidOperationException("已取消的医疗案例无法开具处方"));
                }

                validationResults.Add($"MedicalCase:{medicalCase.Id}:OK");
                context.SetValidationResult("MedicalCaseExists", true);
                context.SetValidationResult("MedicalCasePatientMatch", true);
                context.SetValidationResult("MedicalCaseStatusValid", true);

                // 4. 验证诊断记录（如果提供）
                if (context.ConsultationId.HasValue)
                {
                    var consultation = await FindEntityAsync<LYBT.Entities.Consultation.Consultation>(
                        context.ConsultationId.Value, cancellationToken);

                    if (consultation == null)
                    {
                        context.LogError("Consultation not found: {ConsultationId}", context.ConsultationId);
                        context.SetValidationResult("ConsultationExists", false);
                        return CreateFailureResult(new InvalidOperationException("诊断记录不存在"));
                    }

                    // 验证诊断记录与医疗案例的关联
                    if (consultation.MedicalCaseId != context.MedicalCaseId)
                    {
                        context.LogError(
                            "Consultation medical case mismatch: Consultation.MedicalCaseId={ConsultationMedicalCaseId}, Context.MedicalCaseId={ContextMedicalCaseId}",
                            consultation.MedicalCaseId, context.MedicalCaseId);
                        context.SetValidationResult("ConsultationMedicalCaseMatch", false);
                        return CreateFailureResult(new InvalidOperationException("诊断记录与医疗案例不匹配"));
                    }

                    validationResults.Add($"Consultation:{consultation.Id}:OK");
                    context.SetValidationResult("ConsultationExists", true);
                    context.SetValidationResult("ConsultationMedicalCaseMatch", true);
                }

                // 5. 验证药材存在性（基础验证）
                foreach (var item in context.Items)
                {
                    var herb = await FindEntityAsync<LYBT.Entities.Herbs.Herb>(item.HerbId, cancellationToken);
                    if (herb == null)
                    {
                        context.LogError("Herb not found: {HerbId}", item.HerbId);
                        context.SetValidationResult($"Herb_{item.HerbId}_Exists", false);
                        return CreateFailureResult(new InvalidOperationException($"药材不存在：{item.HerbName}"));
                    }

                    if (herb.Status != CommonStatus.Enabled)
                    {
                        context.LogError("Herb is not active: {HerbId}, Status: {Status}", item.HerbId, herb.Status);
                        context.SetValidationResult($"Herb_{item.HerbId}_Active", false);
                        return CreateFailureResult(new InvalidOperationException($"药材已停用：{herb.Name}"));
                    }

                    // 更新药材名称和单价（确保数据一致性）
                    item.HerbName = herb.Name;
                    if (item.UnitPrice == 0 && context.AutoCalculatePrice)
                    {
                        item.UnitPrice = herb.Price ?? 0;
                    }

                    validationResults.Add($"Herb:{herb.Id}:OK");
                    context.SetValidationResult($"Herb_{item.HerbId}_Exists", true);
                    context.SetValidationResult($"Herb_{item.HerbId}_Active", true);
                }

                // 6. 记录验证历史
                await RecordValidationHistoryAsync(context, validationResults, cancellationToken);

                Logger.LogInformation(
                    "Prerequisites validation completed successfully for prescription creation: Patient={PatientId}, Doctor={DoctorId}, MedicalCase={MedicalCaseId}",
                    context.PatientId, context.DoctorId, context.MedicalCaseId);

                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["ValidatedEntities"] = validationResults,
                    ["ValidatedItemCount"] = context.Items.Count,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to validate prerequisites for prescription creation");
                throw;
            }
        }

        /// <summary>
        /// 记录验证历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="validations">验证记录</param>
        /// <param name="cancellationToken">取消令牌</param>
        private Task RecordValidationHistoryAsync(
            PrescriptionTransactionContext context,
            List<string> validations,
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["PrerequisiteValidations"] = validations;
                context.PrescriptionMetadata["ValidationTimestamp"] = DateTime.UtcNow;

                Logger.LogDebug("Recorded prerequisite validation history: {Validations}", string.Join("; ", validations));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record prerequisite validation history");

                // 不抛出异常，因为这不是关键操作
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 检查是否存在重复的活跃处方
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在重复</returns>
        private async Task<bool> HasDuplicateActivePrescriptionAsync(
            PrescriptionTransactionContext context,
            CancellationToken cancellationToken)
        {
            return await DbContext.Set<LYBT.Entities.Prescriptions.Prescription>()
                .AnyAsync(
                    p => p.MedicalCaseId == context.MedicalCaseId &&
                              p.Status == PrescriptionStatus.Draft &&
                              p.Indication == context.Indication, cancellationToken);
        }
    }
}
