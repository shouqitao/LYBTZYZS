using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Transactions.Steps;
using LYBT.Infrastructure.Transactions;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Transactions.Steps
{
    /// <summary>
    /// 初始化诊断记录事务步骤
    /// 负责创建Consultation记录，建立与MedicalCase的1:1关联
    /// </summary>
    public class InitializeConsultationStep : DatabaseTransactionStep<ConsultationTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "InitializeConsultation";

        /// <inheritdoc />
        public override int Order => 2;

        /// <inheritdoc />
        public override bool SupportsCompensation => true;

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(30);

        public InitializeConsultationStep(AppDbContext dbContext, ILogger<InitializeConsultationStep> logger)
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
                // 必须已经创建医疗案例
                if (!context.MedicalCaseId.HasValue)
                {
                    context.LogError("Cannot initialize consultation without medical case ID");
                    context.SetValidationResult("MedicalCaseExists", false);
                    return false;
                }

                // 验证医疗案例是否存在且状态正确
                var medicalCase = await FindEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(
                    context.MedicalCaseId.Value, cancellationToken);

                if (medicalCase == null)
                {
                    context.LogError("Medical case not found: {MedicalCaseId}", context.MedicalCaseId);
                    context.SetValidationResult("MedicalCaseExists", false);
                    return false;
                }

                // 检查医疗案例状态是否允许创建诊断
                if (medicalCase.Status != MedicalCaseStatus.Registered)
                {
                    context.LogWarning("Medical case status not valid for consultation creation: {Status}", medicalCase.Status);
                    context.SetValidationResult("MedicalCaseStatusValid", false);
                    return false;
                }

                // 检查是否已存在诊断记录
                var existingConsultation = await DbContext.Set<LYBT.Entities.Consultation.Consultation>()
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == context.MedicalCaseId, cancellationToken);

                if (existingConsultation != null)
                {
                    context.LogWarning("Consultation already exists for medical case: {MedicalCaseId}", context.MedicalCaseId);
                    context.SetValidationResult("ConsultationAlreadyExists", true);
                    
                    // 如果不是强制覆盖，则不允许重复创建
                    var allowOverwrite = context.GetData<bool>("AllowConsultationOverwrite");
                    if (!allowOverwrite)
                    {
                        return false;
                    }
                }

                // 记录验证成功
                context.SetValidationResult("MedicalCaseExists", true);
                context.SetValidationResult("MedicalCaseStatusValid", true);
                context.SetValidationResult("CanCreateConsultation", true);

                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate consultation initialization conditions");
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
                // 检查是否需要删除已有的诊断记录
                var allowOverwrite = context.GetData<bool>("AllowConsultationOverwrite");
                if (allowOverwrite)
                {
                    var existingConsultation = await DbContext.Set<LYBT.Entities.Consultation.Consultation>()
                        .FirstOrDefaultAsync(c => c.MedicalCaseId == context.MedicalCaseId, cancellationToken);

                    if (existingConsultation != null)
                    {
                        DbContext.Set<LYBT.Entities.Consultation.Consultation>().Remove(existingConsultation);
                        await DbContext.SaveChangesAsync(cancellationToken);
                        Logger.LogInformation("Removed existing consultation: {ConsultationId}", existingConsultation.Id);
                    }
                }

                // 创建诊断记录实体
                var consultation = new LYBT.Entities.Consultation.Consultation
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = context.MedicalCaseId!.Value,
                    PatientId = context.PatientId,
                    UserId = context.DoctorId,
                    ChiefComplaint = context.ChiefComplaint ?? string.Empty,
                    PresentIllness = context.PresentIllness ?? string.Empty,
                    
                    // 中医四诊初始化为空，等待医生填写
                    Inspection = null,
                    AuscultationOlfaction = null,
                    Inquiry = null,
                    Palpation = null,
                    
                    // 中医诊断初始化
                    TCMDiagnosis = "待诊断", // 必填字段，提供默认值
                    TreatmentPrinciple = null,
                    MedicalAdvice = null,
                    
                    Status = CommonStatus.Enabled,
                    Remark = context.Remark
                };

                // 保存到数据库
                var createdConsultation = await CreateEntityAsync(consultation, cancellationToken);
                
                // 更新上下文
                context.ConsultationId = createdConsultation.Id;
                
                // 设置实体ID用于补偿
                context.SetEntityId("Consultation", createdConsultation.Id);
                
                Logger.LogInformation("Created consultation successfully: {ConsultationId} for medical case: {MedicalCaseId}", 
                    createdConsultation.Id, context.MedicalCaseId);

                // 返回成功结果
                return CreateEntitySuccessResult("Consultation", createdConsultation.Id, "Create");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create consultation for medical case: {MedicalCaseId}", context.MedicalCaseId);
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
                var consultationId = context.GetEntityId("Consultation");
                if (consultationId == null)
                {
                    Logger.LogWarning("No consultation ID found for compensation");
                    return CreateSuccessResult(new Dictionary<string, object> { ["Action"] = "NoCompensationNeeded" });
                }

                Logger.LogInformation("Starting compensation: deleting consultation {ConsultationId}", consultationId);

                // 删除创建的诊断记录
                var deleted = await DeleteEntityAsync<LYBT.Entities.Consultation.Consultation>(consultationId.Value, cancellationToken);
                
                if (deleted)
                {
                    // 清除上下文中的相关信息
                    context.ConsultationId = null;
                    context.RemoveEntityId("Consultation");
                    
                    Logger.LogInformation("Successfully compensated: deleted consultation {ConsultationId}", consultationId);
                    
                    return CreateSuccessResult(new Dictionary<string, object> 
                    { 
                        ["Action"] = "ConsultationDeleted",
                        ["DeletedId"] = consultationId
                    });
                }
                else
                {
                    Logger.LogWarning("Consultation not found during compensation: {ConsultationId}", consultationId);
                    return CreateSuccessResult(new Dictionary<string, object> { ["Action"] = "NotFound" });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compensate consultation creation");
                return CreateFailureResult(ex, new Dictionary<string, object> { ["Action"] = "CompensationFailed" });
            }
        }

        /// <summary>
        /// 验证诊断数据的基本完整性
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <returns>验证结果</returns>
        private (bool IsValid, List<string> Errors) ValidateConsultationData(ConsultationTransactionContext context)
        {
            var errors = new List<string>();

            // 验证主诉长度
            if (!string.IsNullOrEmpty(context.ChiefComplaint) && context.ChiefComplaint.Length > 500)
            {
                errors.Add("主诉内容不能超过500字符");
            }

            // 验证现病史长度
            if (!string.IsNullOrEmpty(context.PresentIllness) && context.PresentIllness.Length > 1000)
            {
                errors.Add("现病史内容不能超过1000字符");
            }

            // 验证备注长度
            if (!string.IsNullOrEmpty(context.Remark) && context.Remark.Length > 500)
            {
                errors.Add("备注内容不能超过500字符");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 应用默认的诊断模板（如果配置了的话）
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="consultation">诊断实体</param>
        private void ApplyConsultationTemplate(ConsultationTransactionContext context, LYBT.Entities.Consultation.Consultation consultation)
        {
            // 可以根据医生偏好或者患者历史记录应用默认模板
            var templateData = context.GetData<Dictionary<string, object>>("ConsultationTemplate");
            if (templateData != null)
            {
                if (templateData.TryGetValue("DefaultTCMDiagnosis", out var diagnosis) && diagnosis is string diagnosisStr)
                {
                    consultation.TCMDiagnosis = diagnosisStr;
                }

                if (templateData.TryGetValue("DefaultTreatmentPrinciple", out var principle) && principle is string principleStr)
                {
                    consultation.TreatmentPrinciple = principleStr;
                }
            }
        }
    }
}