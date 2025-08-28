using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Helpers;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services.Intelligence;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services.Workflow
{
    /// <summary>
    /// 处方工作流服务实现 - UltraThink重构版本
    /// 负责处方审批、提交、取消等工作流操作
    /// </summary>
    public class PrescriptionWorkflowService : IPrescriptionWorkflowService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly AppDbContext _dbContext;
        private readonly IPrescriptionIntelligentService _intelligentService;
        private readonly PrescriptionValidationHelper _validationHelper;
        private readonly ILogger<PrescriptionWorkflowService> _logger;

        public PrescriptionWorkflowService(
            IPrescriptionRepository repository,
            AppDbContext dbContext,
            IPrescriptionIntelligentService intelligentService,
            PrescriptionValidationHelper validationHelper,
            ILogger<PrescriptionWorkflowService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _intelligentService = intelligentService ?? throw new ArgumentNullException(nameof(intelligentService));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 审批通过处方 - 医生或管理员审批
        /// </summary>
        public async Task<ServiceResult<bool>> ApproveAsync(Guid id, string approvalNote, Guid operatorId, string operatorName)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以审批通过
                var canApproveResult = _validationHelper.ValidateCanApprove(prescription.Status);
                if (!canApproveResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(canApproveResult.ErrorMessage ?? "无法审批处方");
                }

                // 更新状态为已完成
                prescription.Status = PrescriptionStatus.Completed;
                // TODO: 添加审批记录和备注
                if (!string.IsNullOrEmpty(approvalNote))
                {
                    prescription.Remark = string.IsNullOrEmpty(prescription.Remark) 
                        ? $"审批备注: {approvalNote}"
                        : $"{prescription.Remark}\n审批备注: {approvalNote}";
                }

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("审批通过处方失败");
                }

                // 如果处方关联了医疗案例，同步更新案例状态
                if (prescription.MedicalCaseId != Guid.Empty)
                {
                    await _intelligentService.UpdateMedicalCaseStatusAsync(prescription.MedicalCaseId, "处方审批通过");
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 记录操作日志
                _logger.LogInformation("处方审批通过 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",
                    operatorName, operatorId, id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "审批通过处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure("审批通过处方失败");
            }
        }

        /// <summary>
        /// 拒绝处方
        /// </summary>
        public async Task<ServiceResult<bool>> RejectAsync(Guid id, string rejectionReason, Guid operatorId, string operatorName)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以拒绝
                var canRejectResult = _validationHelper.ValidateCanReject(prescription.Status);
                if (!canRejectResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(canRejectResult.ErrorMessage ?? "无法拒绝处方");
                }

                // 更新状态为已拒绝
                prescription.Status = PrescriptionStatus.Voided;
                if (!string.IsNullOrEmpty(rejectionReason))
                {
                    prescription.Remark = string.IsNullOrEmpty(prescription.Remark) 
                        ? $"拒绝原因: {rejectionReason}"
                        : $"{prescription.Remark}\n拒绝原因: {rejectionReason}";
                }

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("拒绝处方失败");
                }

                // 如果处方关联了医疗案例，同步更新案例状态
                if (prescription.MedicalCaseId != Guid.Empty)
                {
                    await _intelligentService.UpdateMedicalCaseStatusAsync(prescription.MedicalCaseId, "处方审批拒绝");
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 记录操作日志
                _logger.LogInformation("处方拒绝 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}, 原因: {Reason}",
                    operatorName, operatorId, id, rejectionReason);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "拒绝处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure("拒绝处方失败");
            }
        }

        /// <summary>
        /// 提交处方（从草稿状态提交待审批）
        /// </summary>
        public async Task<ServiceResult<bool>> SubmitAsync(Guid prescriptionId, Guid operatorId, string operatorName)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(prescriptionId);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以提交
                var canSubmitResult = _validationHelper.ValidateCanSubmit(prescription);
                if (!canSubmitResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(canSubmitResult.ErrorMessage ?? "无法提交处方");
                }

                // 更新状态为待审批
                prescription.Status = PrescriptionStatus.Completed;

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("提交处方失败");
                }

                // 记录操作日志
                _logger.LogInformation("处方提交 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",
                    operatorName, operatorId, prescriptionId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, prescriptionId);
                return ServiceResult<bool>.Failure("提交处方失败");
            }
        }

        /// <summary>
        /// 取消处方
        /// </summary>
        public async Task<ServiceResult<bool>> CancelAsync(Guid id, string cancellationReason, Guid operatorId, string operatorName)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以取消
                var canCancelResult = _validationHelper.ValidateCanCancel(prescription.Status);
                if (!canCancelResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(canCancelResult.ErrorMessage ?? "无法取消处方");
                }

                // 更新状态为已取消
                prescription.Status = PrescriptionStatus.Voided;
                if (!string.IsNullOrEmpty(cancellationReason))
                {
                    prescription.Remark = string.IsNullOrEmpty(prescription.Remark) 
                        ? $"取消原因: {cancellationReason}"
                        : $"{prescription.Remark}\n取消原因: {cancellationReason}";
                }

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("取消处方失败");
                }

                // 记录操作日志
                _logger.LogInformation("处方取消 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}, 原因: {Reason}",
                    operatorName, operatorId, id, cancellationReason);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure("取消处方失败");
            }
        }

        /// <summary>
        /// 快速保存（保持草稿状态）
        /// </summary>
        public async Task<ServiceResult<bool>> QuickSaveAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 确保状态为草稿状态
                if (prescription.Status != PrescriptionStatus.Draft)
                {
                    prescription.Status = PrescriptionStatus.Draft;
                    
                    var success = await _repository.UpdateAsync(prescription);
                    if (!success)
                    {
                        return ServiceResult<bool>.Failure("快速保存失败");
                    }
                }

                // 记录操作日志
                _logger.LogInformation("处方快速保存 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",
                    operatorName, operatorId, id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速保存处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure("快速保存处方失败");
            }
        }
    }
}