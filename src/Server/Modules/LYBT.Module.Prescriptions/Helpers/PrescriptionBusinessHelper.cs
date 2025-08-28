using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Prescriptions;

namespace LYBT.Module.Prescriptions.Helpers
{
    /// <summary>
    /// PrescriptionService业务逻辑助手类 - UltraThink Helper模式
    /// 负责复杂业务逻辑、工作流管理、审批流程和特殊功能
    /// </summary>
    public class PrescriptionBusinessHelper
    {
        private readonly IPrescriptionRepository _repository;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IIntelligentPrescriptionService _intelligentService;
        private readonly PrescriptionValidationHelper _validationHelper;
        private readonly ILogger<PrescriptionBusinessHelper> _logger;

        public PrescriptionBusinessHelper(
            IPrescriptionRepository repository,
            AppDbContext dbContext,
            IMapper mapper,
            IIntelligentPrescriptionService intelligentService,
            PrescriptionValidationHelper validationHelper,
            ILogger<PrescriptionBusinessHelper> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _intelligentService = intelligentService ?? throw new ArgumentNullException(nameof(intelligentService));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD操作

        /// <summary>
        /// 创建新处方 - 使用事务确保数据一致性
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 验证创建数据
                var validationResult = await _validationHelper.ValidateCreateAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionDto>.Failure(validationResult.ErrorMessage ?? "验证失败");                }

                if (!validationResult.Data.IsValid)
                {                    var errors = string.Join("; ", validationResult.Data.Errors);                    return ServiceResult<PrescriptionDto>.Failure($"数据验证失败: {errors}");                }

                // 映射到实体
                var model = _mapper.Map<LYBT.Entities.Prescriptions.Prescription>(dto);
                model.Id = Guid.NewGuid();
                model.Status = PrescriptionStatus.Draft; // 默认为草稿状态

                // 执行智能检查
                await PerformIntelligentChecks(dto.Items, model.Id, operatorName);

                // 保存到数据库
                var success = await _repository.AddAsync(model);
                if (!success)
                {                    return ServiceResult<PrescriptionDto>.Failure("保存处方失败");                }

                // 如果处方关联医疗案例，更新案例状态
                if (dto.ConsultationId.HasValue)
                {                    await UpdateMedicalCaseStatusAsync(dto.ConsultationId.Value, "处方已创建");                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 记录操作日志                _logger.LogInformation("处方新增 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",                     operatorName, operatorId, model.Id);

                // 返回创建的DTO
                var resultDto = _mapper.Map<PrescriptionDto>(model);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();                _logger.LogError(ex, "创建处方失败 - 操作者: {OperatorName}", operatorName);                return ServiceResult<PrescriptionDto>.Failure("创建处方失败");            }
        }

        /// <summary>
        /// 更新处方
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName)
        {
            try
            {
                // 验证更新数据
                var validationResult = await _validationHelper.ValidateUpdateAsync(dto);
                if (!validationResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(validationResult.ErrorMessage ?? "验证失败");                }

                if (!validationResult.Data.IsValid)
                {                    var errors = string.Join("; ", validationResult.Data.Errors);                    return ServiceResult<bool>.Failure($"数据验证失败: {errors}");                }

                // 获取现有处方
                var existingPrescription = await _repository.GetByIdAsync(dto.Id);
                if (existingPrescription == null)
                {                    return ServiceResult<bool>.Failure("处方不存在");                }

                // 验证是否可以编辑
                var canEditResult = _validationHelper.ValidateCanEdit(existingPrescription.Status);
                if (!canEditResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(canEditResult.ErrorMessage ?? "无法编辑");                }

                // 映射更新字段
                var updatedModel = _mapper.Map(dto, existingPrescription);

                // 保存更新
                var success = await _repository.UpdateAsync(updatedModel);
                if (!success)
                {                    return ServiceResult<bool>.Failure("更新处方失败");                }

                // 记录操作日志                _logger.LogInformation("处方编辑 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",                     operatorName, operatorId, dto.Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "更新处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, dto.Id);                return ServiceResult<bool>.Failure("更新处方失败");            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                // 获取处方
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {                    return ServiceResult<bool>.Failure("处方不存在");                }

                // 验证是否可以删除
                var canDeleteResult = _validationHelper.ValidateCanDelete(prescription.Status);
                if (!canDeleteResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(canDeleteResult.ErrorMessage ?? "无法删除");                }

                // 执行删除
                var success = await _repository.DeleteAsync(id);
                if (!success)
                {                    return ServiceResult<bool>.Failure("删除处方失败");                }

                // 记录操作日志                _logger.LogInformation("处方删除 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",                     operatorName, operatorId, id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "删除处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);                return ServiceResult<bool>.Failure("删除处方失败");            }
        }

        #endregion

        #region 工作流操作

        /// <summary>
        /// 批准处方 - 使用事务确保数据一致性
        /// </summary>
        public async Task<ServiceResult<bool>> ApproveAsync(Guid id, string approvalNote, Guid operatorId, string operatorName)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {                    return ServiceResult<bool>.Failure("处方不存在");                }

                // 验证是否可以批准
                var canApproveResult = _validationHelper.ValidateCanApprove(prescription.Status);
                if (!canApproveResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(canApproveResult.ErrorMessage ?? "无法批准");                }

                // 更新状态
                prescription.Status = PrescriptionStatus.Completed;
                // TODO: 添加审批记录字段

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {                    return ServiceResult<bool>.Failure("批准处方失败");                }

                // 如果处方关联医疗案例，更新案例状态
                if (prescription.MedicalCaseId != Guid.Empty)
                {                    await UpdateMedicalCaseStatusAsync(prescription.MedicalCaseId, "处方已批准");                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 记录操作日志                _logger.LogInformation("处方审批通过 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}, 备注: {Note}",                     operatorName, operatorId, id, approvalNote);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();                _logger.LogError(ex, "批准处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);                return ServiceResult<bool>.Failure("批准处方失败");            }
        }

        /// <summary>
        /// 拒绝处方
        /// </summary>
        public async Task<ServiceResult<bool>> RejectAsync(Guid id, string reason, Guid operatorId, string operatorName)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {                    return ServiceResult<bool>.Failure("处方不存在");                }

                // 验证是否可以拒绝
                var canRejectResult = _validationHelper.ValidateCanReject(prescription.Status);
                if (!canRejectResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(canRejectResult.ErrorMessage ?? "无法拒绝");                }

                // 更新状态（退回草稿）
                prescription.Status = PrescriptionStatus.Draft;
                // TODO: 添加拒绝记录字段

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {                    return ServiceResult<bool>.Failure("拒绝处方失败");                }

                // 记录操作日志                _logger.LogInformation("处方审批拒绝 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}, 原因: {Reason}",                     operatorName, operatorId, id, reason);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "拒绝处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);                return ServiceResult<bool>.Failure("拒绝处方失败");            }
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        public async Task<ServiceResult<bool>> CancelAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {                    return ServiceResult<bool>.Failure("处方不存在");                }

                // 执行作废操作
                var success = await _repository.CancelAsync(id);
                if (!success)
                {                    return ServiceResult<bool>.Failure("作废处方失败");                }

                // 记录操作日志                _logger.LogInformation("处方作废 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",                     operatorName, operatorId, id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "作废处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);                return ServiceResult<bool>.Failure("作废处方失败");            }
        }

        /// <summary>
        /// 快速保存处方（草稿状态）
        /// </summary>
        public async Task<ServiceResult<bool>> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)
        {
            try
            {
                // 验证快速保存数据
                var validationResult = _validationHelper.ValidateQuickSave(dto);
                if (!validationResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(validationResult.ErrorMessage ?? "验证失败");                }

                var prescription = await _repository.GetByIdAsync(prescriptionId);
                if (prescription == null)
                {                    return ServiceResult<bool>.Failure("处方不存在");                }

                // 验证是否可以编辑
                var canEditResult = _validationHelper.ValidateCanEdit(prescription.Status);
                if (!canEditResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(canEditResult.ErrorMessage ?? "无法编辑");                }

                // 更新处方信息
                prescription.Remark = dto.Diagnosis; // UltraThink v2.0简化：使用DTO的Diagnosis字段作为Remark
                prescription.Advice = dto.Advice;
                prescription.Status = PrescriptionStatus.Draft; // 确保为草稿状态

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {                    return ServiceResult<bool>.Failure("快速保存失败");                }

                // 记录操作日志                _logger.LogInformation("处方快速保存 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",                     operatorName, operatorId, prescriptionId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "快速保存处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, prescriptionId);                return ServiceResult<bool>.Failure("快速保存处方失败");            }
        }

        /// <summary>
        /// 提交处方（从草稿变为待审核）
        /// </summary>
        public async Task<ServiceResult<bool>> SubmitAsync(Guid prescriptionId, Guid operatorId, string operatorName)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(prescriptionId);
                if (prescription == null)
                {                    return ServiceResult<bool>.Failure("处方不存在");                }

                // 验证是否可以提交
                var canSubmitResult = _validationHelper.ValidateCanSubmit(prescription);
                if (!canSubmitResult.IsSuccess)
                {                    return ServiceResult<bool>.Failure(canSubmitResult.ErrorMessage ?? "无法提交");                }

                // 更新状态（保持为Draft，等待审批）
                prescription.Status = PrescriptionStatus.Draft;

                var success = await _repository.UpdateAsync(prescription);
                if (!success)
                {                    return ServiceResult<bool>.Failure("提交处方失败");                }

                // 记录操作日志                _logger.LogInformation("处方提交 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",                     operatorName, operatorId, prescriptionId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "提交处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, prescriptionId);                return ServiceResult<bool>.Failure("提交处方失败");            }
        }

        #endregion

        #region 复制和模板功能

        /// <summary>
        /// 复制处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid originalId, string newName, Guid operatorId, string operatorName)
        {
            try
            {
                var originalPrescription = await _repository.GetByIdAsync(originalId);
                if (originalPrescription == null)
                {                    return ServiceResult<PrescriptionDto>.Failure("原处方不存在");                }

                var copyDto = new PrescriptionCreateDto
                {
                    PatientId = originalPrescription.PatientId,
                    DoctorId = originalPrescription.UserId,
                    Remark = string.IsNullOrEmpty(newName) ? originalPrescription.Remark : newName,
                    DosageCount = originalPrescription.DosageCount,
                    Advice = originalPrescription.Advice,
                    Items = originalPrescription.Items.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Usage = item.Usage,
                        Remark = item.Remark
                    }).ToList()
                };

                return await CreateAsync(copyDto, operatorId, operatorName);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "复制处方失败 - 操作者: {OperatorName}, 原处方ID: {OriginalId}", operatorName, originalId);                return ServiceResult<PrescriptionDto>.Failure("复制处方失败");            }
        }

        /// <summary>
        /// 复制上次处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            try
            {
                // 获取患者最近的处方
                var allPrescriptions = await _repository.GetListAsync();
                var lastPrescription = allPrescriptions
                    .Where(p => p.PatientId == patientId)
                    .OrderByDescending(p => p.Id) // UltraThink v2.0简化：按Id排序（时间字段已删除）
                    .FirstOrDefault();

                if (lastPrescription == null)
                {                    return ServiceResult<PrescriptionDto>.Failure("患者没有历史处方记录");                }

                var copyDto = new PrescriptionCreateDto
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Remark = lastPrescription.Remark ?? string.Empty,
                    DosageCount = lastPrescription.DosageCount,
                    Advice = lastPrescription.Advice,
                    Items = lastPrescription.Items.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Usage = item.Usage,
                        Remark = item.Remark
                    }).ToList()
                };

                return await CreateAsync(copyDto, operatorId, operatorName);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "复制上次处方失败 - 操作者: {OperatorName}, 患者ID: {PatientId}", operatorName, patientId);                return ServiceResult<PrescriptionDto>.Failure("复制上次处方失败");            }
        }

        /// <summary>
        /// 从验方模板创建处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            try
            {
                // TODO: 实现从验方模板创建处方的逻辑
                // 需要与Formula模块集成
                await Task.CompletedTask;
                                _logger.LogWarning("从验方模板创建处方功能暂未实现 - 操作者: {OperatorName}, 模板ID: {TemplateId}", operatorName, templateId);                return ServiceResult<PrescriptionDto>.Failure("从验方模板创建处方功能暂未实现");            }
            catch (Exception ex)
            {                _logger.LogError(ex, "从验方模板创建处方失败 - 操作者: {OperatorName}, 模板ID: {TemplateId}", operatorName, templateId);                return ServiceResult<PrescriptionDto>.Failure("从验方模板创建处方失败");            }
        }

        #endregion

        #region 特殊功能

        /// <summary>
        /// 导出处方为PDF
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportToPdfAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {                    return ServiceResult<byte[]>.Failure("处方不存在");                }

                // TODO: 实现PDF导出功能
                // 需要引入PDF生成库（如iTextSharp或PdfSharpCore）
                await Task.CompletedTask;
                _logger.LogWarning("PDF导出功能暂未实现 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);                return ServiceResult<byte[]>.Failure("PDF导出功能暂未实现");            }
            catch (Exception ex)
            {                _logger.LogError(ex, "导出处方PDF失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);                return ServiceResult<byte[]>.Failure("导出处方PDF失败");            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 执行智能检查（药材重复和可用性）
        /// </summary>
        private async Task PerformIntelligentChecks(List<PrescriptionItemCreateDto> items, Guid prescriptionId, string operatorName)
        {
            try
            {
                if (items == null || !items.Any())
                    return;

                var prescriptionItems = items.Select(item => new PrescriptionItemModel
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Usage = item.Usage,
                    Remark = item.Remark ?? item.Note
                }).ToList();

                // 检测重复药材
                var duplicateResult = _intelligentService.DetectDuplicateHerbs(prescriptionItems);
                if (duplicateResult.HasDuplicates && duplicateResult.DuplicateHerbs.Any())
                {                    _logger.LogWarning("处方重复药材警告 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}, 重复药材: {DuplicateHerbs}",                         operatorName, prescriptionId, string.Join(", ", duplicateResult.DuplicateHerbs));                }

                // 检查药材可用性
                var availabilityResult = await _intelligentService.CheckHerbAvailabilityAsync(prescriptionItems);
                if (!availabilityResult.IsAvailable && availabilityResult.UnavailableHerbs.Any())
                {                    _logger.LogWarning("药材可用性警告 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}, 不可用药材: {UnavailableHerbs}",                         operatorName, prescriptionId, string.Join(", ", availabilityResult.UnavailableHerbs));                }
            }
            catch (Exception ex)
            {                _logger.LogWarning(ex, "执行智能检查失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, prescriptionId);            }
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        private async Task UpdateMedicalCaseStatusAsync(Guid medicalCaseId, string statusRemark)
        {
            try
            {
                var medicalCase = await _dbContext.MedicalCases.FindAsync(medicalCaseId);
                if (medicalCase != null)
                {
                    medicalCase.Remark = string.IsNullOrEmpty(medicalCase.Remark) 
                        ? statusRemark                         : $"{medicalCase.Remark}\n{statusRemark}";                    
                    _dbContext.MedicalCases.Update(medicalCase);
                }
            }
            catch (Exception ex)
            {                _logger.LogWarning(ex, "更新医疗案例状态失败 - 案例ID: {MedicalCaseId}, 状态: {Status}", 
                    medicalCaseId, statusRemark);
            }
        }

        #endregion
    }
}


