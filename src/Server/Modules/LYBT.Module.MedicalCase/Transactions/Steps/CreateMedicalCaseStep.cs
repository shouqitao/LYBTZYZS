using System;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Transactions;

// Removed: using LYBT.Infrastructure.Transactions.Steps; - DatabaseTransactionStep now in main namespace
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Transactions.Steps
{
    /// <summary>
    /// 创建医疗案例事务步骤
    /// 负责创建医疗案例记录，包括业务规则验证和数据持久化
    /// </summary>
    public class CreateMedicalCaseStep : DatabaseTransactionStep<ConsultationTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "CreateMedicalCase";

        /// <inheritdoc />
        public override int Order => 1;

        /// <inheritdoc />
        public override bool SupportsCompensation => true;

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(30);

        public CreateMedicalCaseStep(AppDbContext dbContext, ILogger<CreateMedicalCaseStep> logger)
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
                // 验证上下文数据完整性
                var (isValid, errors) = context.ValidateContext();
                if (!isValid)
                {
                    context.LogError("Context validation failed: {Errors}", string.Join(", ", errors));
                    context.SetValidationResult("ContextValidation", new { IsValid = false, Errors = errors });
                    return false;
                }

                // 检查患者是否存在
                var patientExists = await DbContext.Set<LYBT.Entities.Patients.Patient>()
                    .AnyAsync(p => p.Id == context.PatientId, cancellationToken);

                if (!patientExists)
                {
                    context.LogError("Patient not found: {PatientId}", context.PatientId);
                    context.SetValidationResult("PatientExists", false);
                    return false;
                }

                // 检查医生是否存在
                var doctorExists = await DbContext.Set<LYBT.Entities.Users.User>()
                    .AnyAsync(u => u.Id == context.DoctorId, cancellationToken);

                if (!doctorExists)
                {
                    context.LogError("Doctor not found: {DoctorId}", context.DoctorId);
                    context.SetValidationResult("DoctorExists", false);
                    return false;
                }

                // 检查是否已有活跃的医疗案例
                var hasActiveMedicalCase = await DbContext.Set<LYBT.Entities.MedicalCase.MedicalCase>()
                    .AnyAsync(
                        mc => mc.PatientId == context.PatientId &&
                                  mc.Status != MedicalCaseStatus.Completed &&
                                  mc.Status != MedicalCaseStatus.Cancelled &&
                                  mc.Status != MedicalCaseStatus.Archived, cancellationToken);

                if (hasActiveMedicalCase)
                {
                    context.LogWarning("Patient already has active medical case: {PatientId}", context.PatientId);
                    context.SetValidationResult("HasActiveMedicalCase", true);

                    // 如果不是急诊，不允许创建重复案例
                    if (!context.IsEmergency)
                    {
                        return false;
                    }
                }

                // 记录验证成功
                context.SetValidationResult("PatientExists", true);
                context.SetValidationResult("DoctorExists", true);
                context.SetValidationResult("CanCreateMedicalCase", true);

                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate medical case creation conditions");
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
                // 创建医疗案例实体
                var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = context.PatientId,
                    PatientName = context.PatientName,
                    DoctorId = context.DoctorId,
                    DoctorName = context.DoctorName,
                    ConsultationDate = context.ConsultationDate,
                    Status = MedicalCaseStatus.Registered,
                    Remark = context.Remark,
                    PrescriptionId = null // 初始时没有处方
                };

                // 保存到数据库
                var createdCase = await CreateEntityAsync(medicalCase, cancellationToken);

                // 更新上下文
                context.MedicalCaseId = createdCase.Id;
                context.MedicalCaseStatus = createdCase.Status;

                // 设置实体ID用于补偿
                context.SetEntityId("MedicalCase", createdCase.Id);

                Logger.LogInformation(
                    "Created medical case successfully: {MedicalCaseId} for patient: {PatientId}",
                    createdCase.Id, context.PatientId);

                // 返回成功结果
                return CreateEntitySuccessResult("MedicalCase", createdCase.Id, "Create");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create medical case for patient: {PatientId}", context.PatientId);
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
                var medicalCaseId = context.GetEntityId("MedicalCase");
                if (medicalCaseId == null)
                {
                    Logger.LogWarning("No medical case ID found for compensation");
                    return CreateSuccessResult(new Dictionary<string, object> { ["Action"] = "NoCompensationNeeded" });
                }

                Logger.LogInformation("Starting compensation: deleting medical case {MedicalCaseId}", medicalCaseId);

                // 删除创建的医疗案例
                var deleted = await DeleteEntityAsync<LYBT.Entities.MedicalCase.MedicalCase>(medicalCaseId.Value, cancellationToken);

                if (deleted)
                {
                    // 清除上下文中的相关信息
                    context.MedicalCaseId = null;
                    context.MedicalCaseStatus = null;
                    context.RemoveEntityId("MedicalCase");

                    Logger.LogInformation("Successfully compensated: deleted medical case {MedicalCaseId}", medicalCaseId);

                    return CreateSuccessResult(new Dictionary<string, object>
                    {
                        ["Action"] = "MedicalCaseDeleted",
                        ["DeletedId"] = medicalCaseId
                    });
                }
                else
                {
                    Logger.LogWarning("Medical case not found during compensation: {MedicalCaseId}", medicalCaseId);
                    return CreateSuccessResult(new Dictionary<string, object> { ["Action"] = "NotFound" });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compensate medical case creation");
                return CreateFailureResult(ex, new Dictionary<string, object> { ["Action"] = "CompensationFailed" });
            }
        }

        /// <summary>
        /// 获取患者当前活跃医疗案例数量
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>活跃案例数量</returns>
        private async Task<int> GetActiveCardsCountAsync(Guid patientId, CancellationToken cancellationToken)
        {
            return await DbContext.Set<LYBT.Entities.MedicalCase.MedicalCase>()
                .CountAsync(
                    mc => mc.PatientId == patientId &&
                                 mc.Status != MedicalCaseStatus.Completed &&
                                 mc.Status != MedicalCaseStatus.Cancelled &&
                                 mc.Status != MedicalCaseStatus.Archived, cancellationToken);
        }

        /// <summary>
        /// 检查医生是否可以接诊新患者
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否可以接诊</returns>
        private async Task<bool> CanDoctorAcceptNewPatientAsync(Guid doctorId, CancellationToken cancellationToken)
        {
            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1);

            // 检查医生今天的案例数量（简单的负载均衡）
            var todayCasesCount = await DbContext.Set<LYBT.Entities.MedicalCase.MedicalCase>()
                .CountAsync(
                    mc => mc.DoctorId == doctorId &&
                                 mc.ConsultationDate >= todayStart &&
                                 mc.ConsultationDate < todayEnd &&
                                 mc.Status != MedicalCaseStatus.Cancelled, cancellationToken);

            // 假设每个医生每天最多处理20个案例
            return todayCasesCount < 20;
        }
    }
}
