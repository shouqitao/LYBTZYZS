using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Transactions.Steps;
using LYBT.Infrastructure.Transactions;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Prescriptions.Transactions.Steps
{
    /// <summary>
    /// 创建处方基础记录事务步骤
    /// 负责创建处方主记录，不包含药材项目
    /// </summary>
    public class CreatePrescriptionStep : DatabaseTransactionStep<PrescriptionTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "CreatePrescription";

        /// <inheritdoc />
        public override int Order => 2;

        /// <inheritdoc />
        public override bool SupportsCompensation => true;

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(30);

        public CreatePrescriptionStep(AppDbContext dbContext, ILogger<CreatePrescriptionStep> logger)
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
                // 必须已经通过先决条件验证
                var prerequisitesPassed = context.GetValidationResult<bool>("PatientExists") &&
                                        context.GetValidationResult<bool>("DoctorExists") &&
                                        context.GetValidationResult<bool>("MedicalCaseExists");

                if (!prerequisitesPassed)
                {
                    context.LogError("Cannot create prescription without passing prerequisites validation");
                    context.SetValidationResult("CanCreatePrescription", false);
                    return false;
                }

                // 验证是否已存在同名处方
                var existingPrescription = await DbContext.Set<Prescription>()
                    .FirstOrDefaultAsync(p => p.MedicalCaseId == context.MedicalCaseId &&
                                            p.Indication == context.Indication &&
                                            p.Status == PrescriptionStatus.Draft, cancellationToken);

                if (existingPrescription != null)
                {
                    context.LogWarning("Prescription with same indication already exists for medical case: {MedicalCaseId}, Indication: {Indication}", 
                        context.MedicalCaseId, context.Indication);
                    context.SetValidationResult("DuplicatePrescription", true);
                    
                    // 可以选择是否允许重复，这里设为允许
                    // return false;
                }

                // 记录验证成功
                context.SetValidationResult("CanCreatePrescription", true);
                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate prescription creation conditions");
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
                // 计算处方总价（如果需要）
                if (context.AutoCalculatePrice)
                {
                    context.CalculateTotalPrice();
                }

                // 创建处方基础记录
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = context.MedicalCaseId,
                    PatientId = context.PatientId,
                    UserId = context.DoctorId,
                    Indication = context.Indication,
                    DosageCount = context.DosageCount,
                    Discount = context.Discount,
                    Advice = context.Advice,
                    FormulaSource = context.FormulaSource,
                    Status = context.PrescriptionStatus,
                    Remark = context.Remark
                };

                // 保存到数据库
                var createdPrescription = await CreateEntityAsync(prescription, cancellationToken);
                
                // 更新上下文
                context.PrescriptionId = createdPrescription.Id;
                
                // 设置实体ID用于补偿
                context.SetEntityId("Prescription", createdPrescription.Id);
                
                Logger.LogInformation("Created prescription successfully: {PrescriptionId} for medical case: {MedicalCaseId}, Patient: {PatientId}", 
                    createdPrescription.Id, context.MedicalCaseId, context.PatientId);

                // 记录处方创建历史
                await RecordPrescriptionCreationHistoryAsync(context, createdPrescription, cancellationToken);

                // 返回成功结果
                return CreateEntitySuccessResult("Prescription", createdPrescription.Id, "Create", new Dictionary<string, object>
                {
                    ["PrescriptionId"] = createdPrescription.Id,
                    ["MedicalCaseId"] = context.MedicalCaseId,
                    ["PatientId"] = context.PatientId,
                    ["DoctorId"] = context.DoctorId,
                    ["Indication"] = context.Indication,
                    ["DosageCount"] = context.DosageCount,
                    ["Status"] = context.PrescriptionStatus.ToString(),
                    ["ItemsToAdd"] = context.Items.Count,
                    ["TotalPrice"] = context.TotalPrice,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create prescription for medical case: {MedicalCaseId}", context.MedicalCaseId);
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
                var prescriptionId = context.GetEntityId("Prescription");
                if (prescriptionId == null)
                {
                    Logger.LogWarning("No prescription ID found for compensation");
                    return CreateSuccessResult(new Dictionary<string, object> { ["Action"] = "NoCompensationNeeded" });
                }

                Logger.LogInformation("Starting compensation: deleting prescription {PrescriptionId}", prescriptionId);

                // 删除创建的处方记录
                var deleted = await DeleteEntityAsync<Prescription>(prescriptionId.Value, cancellationToken);
                
                if (deleted)
                {
                    // 清除上下文中的相关信息
                    context.PrescriptionId = null;
                    context.RemoveEntityId("Prescription");
                    
                    // 记录补偿历史
                    await RecordCompensationHistoryAsync(context, prescriptionId.Value, cancellationToken);
                    
                    Logger.LogInformation("Successfully compensated: deleted prescription {PrescriptionId}", prescriptionId);
                    
                    return CreateSuccessResult(new Dictionary<string, object> 
                    { 
                        ["Action"] = "PrescriptionDeleted",
                        ["DeletedId"] = prescriptionId,
                        ["CompensationTimestamp"] = DateTime.UtcNow
                    });
                }
                else
                {
                    Logger.LogWarning("Prescription not found during compensation: {PrescriptionId}", prescriptionId);
                    return CreateSuccessResult(new Dictionary<string, object> { ["Action"] = "NotFound" });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compensate prescription creation");
                return CreateFailureResult(ex, new Dictionary<string, object> { ["Action"] = "CompensationFailed" });
            }
        }

        /// <summary>
        /// 记录处方创建历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="prescription">处方实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task RecordPrescriptionCreationHistoryAsync(
            PrescriptionTransactionContext context, 
            Prescription prescription, 
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["PrescriptionCreation"] = new
                {
                    PrescriptionId = prescription.Id,
                    CreatedAt = DateTime.UtcNow,
                    PatientId = context.PatientId,
                    PatientName = context.PatientName,
                    DoctorId = context.DoctorId,
                    DoctorName = context.DoctorName,
                    MedicalCaseId = context.MedicalCaseId,
                    Indication = context.Indication,
                    ItemCount = context.Items.Count,
                    Status = context.PrescriptionStatus.ToString()
                };
                
                Logger.LogDebug("Recorded prescription creation history: {PrescriptionId}", prescription.Id);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record prescription creation history");
                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 记录补偿操作历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task RecordCompensationHistoryAsync(
            PrescriptionTransactionContext context, 
            Guid prescriptionId, 
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["PrescriptionCompensation"] = new
                {
                    CompensatedPrescriptionId = prescriptionId,
                    CompensatedAt = DateTime.UtcNow,
                    Reason = "TransactionRollback"
                };
                
                Logger.LogDebug("Recorded compensation history: {PrescriptionId}", prescriptionId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record compensation history");
                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 验证处方基本信息的合理性
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <returns>验证结果</returns>
        private (bool IsValid, List<string> Errors) ValidatePrescriptionData(PrescriptionTransactionContext context)
        {
            var errors = new List<string>();

            // 验证主治长度
            if (string.IsNullOrEmpty(context.Indication))
            {
                errors.Add("主治不能为空");
            }
            else if (context.Indication.Length > 500)
            {
                errors.Add("主治内容不能超过500字符");
            }

            // 验证帖数合理性
            if (context.DosageCount <= 0 || context.DosageCount > 100)
            {
                errors.Add("处方帖数必须在1-100之间");
            }

            // 验证折扣合理性
            if (context.Discount < 0 || context.Discount > 1)
            {
                errors.Add("折扣必须在0-1之间");
            }

            // 验证医嘱长度
            if (!string.IsNullOrEmpty(context.Advice) && context.Advice.Length > 500)
            {
                errors.Add("医嘱内容不能超过500字符");
            }

            // 验证备注长度
            if (!string.IsNullOrEmpty(context.Remark) && context.Remark.Length > 500)
            {
                errors.Add("备注内容不能超过500字符");
            }

            return (errors.Count == 0, errors);
        }
    }
}