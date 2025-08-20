using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoMapper;
using LYBT.Entities.MedicalCase;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Helpers
{
    /// <summary>
    /// MedicalCaseService业务逻辑助手类 - UltraThink Helper模式
    /// 负责所有业务操作：CRUD、状态管理、生命周期管理
    /// </summary>
    public class MedicalCaseBusinessHelper
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseBusinessHelper> _logger;
        private readonly MedicalCaseValidationHelper _validationHelper;

        public MedicalCaseBusinessHelper(
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseBusinessHelper> logger,
            MedicalCaseValidationHelper validationHelper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
        }

        #region CRUD操作

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                // 验证创建数据
                var validation = await _validationHelper.ValidateCreateAsync(dto);
                if (!validation.IsValid)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validation.ErrorMessage);
                }

                // 创建实体
                var model = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(dto);
                model.Id = Guid.NewGuid();
                model.Status = MedicalCaseStatus.InConsultation;

                _logger.LogInformation("创建医疗案例: 患者ID={PatientId}, 案例ID={CaseId}", dto.PatientId, model.Id);

                // 保存到数据库
                var created = await _repository.AddAsync(model);
                var createdDto = _mapper.Map<MedicalCaseDto>(created);

                _logger.LogInformation("医疗案例创建成功: {CaseId}", created.Id);
                return ServiceResult<MedicalCaseDto>.Success(createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                return ServiceResult<MedicalCaseDto>.Failure("创建医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                // 验证更新数据
                var validation = await _validationHelper.ValidateUpdateAsync(id, dto);
                if (!validation.IsValid)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validation.ErrorMessage);
                }

                var model = validation.MedicalCase!;
                _logger.LogInformation("更新医疗案例: {CaseId}", id);

                // 更新字段
                if (!string.IsNullOrWhiteSpace(dto.Status))
                {
                    if (Enum.TryParse<MedicalCaseStatus>(dto.Status, out var status))
                    {
                        _logger.LogInformation("更新案例状态: {CaseId} {OldStatus} -> {NewStatus}", 
                            id, model.Status, status);
                        model.Status = status;
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.Remark))
                {
                    model.Remark = dto.Remark;
                }

                // 保存更新
                var updated = await _repository.UpdateAsync(model);
                if (updated == null)
                    return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败");

                var updatedDto = _mapper.Map<MedicalCaseDto>(updated);
                _logger.LogInformation("医疗案例更新成功: {CaseId}", updated.Id);
                return ServiceResult<MedicalCaseDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败: {Id}", id);
                return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                // 验证案例存在性
                var validation = await _validationHelper.ValidateExistsAsync(id);
                if (!validation.IsValid)
                {
                    return ServiceResult<bool>.Failure(validation.ErrorMessage);
                }

                var model = validation.MedicalCase!;

                // 验证是否可以删除
                var deleteValidation = _validationHelper.ValidateCanDelete(model);
                if (!deleteValidation.IsValid)
                {
                    return ServiceResult<bool>.Failure(deleteValidation.ErrorMessage);
                }

                _logger.LogInformation("删除医疗案例: {CaseId} (软删除为Cancelled状态)", id);

                // 软删除：将状态设为Cancelled
                model.Status = MedicalCaseStatus.Cancelled;
                var result = await _repository.UpdateAsync(model);

                var success = result != null;
                if (success)
                {
                    _logger.LogInformation("医疗案例删除成功: {CaseId}", id);
                }

                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除医疗案例失败", ex);
            }
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 完成医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        {
            try
            {
                // 验证案例存在性
                var validation = await _validationHelper.ValidateExistsAsync(id);
                if (!validation.IsValid)
                {
                    return ServiceResult<bool>.Failure(validation.ErrorMessage);
                }

                var model = validation.MedicalCase!;

                // 验证是否可以完成
                var completeValidation = _validationHelper.ValidateCanComplete(model);
                if (!completeValidation.IsValid)
                {
                    return ServiceResult<bool>.Failure(completeValidation.ErrorMessage);
                }

                _logger.LogInformation("完成医疗案例: {CaseId}, 原因: {Reason}", id, completionReason);

                // 更新状态
                model.Status = MedicalCaseStatus.Completed;

                if (!string.IsNullOrWhiteSpace(completionReason))
                    model.Remark = completionReason;

                var result = await _repository.UpdateAsync(model);
                var success = result != null;

                if (success)
                {
                    _logger.LogInformation("医疗案例完成成功: {CaseId}", id);
                }

                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("完成医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 暂停医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
        {
            try
            {
                // 验证案例存在性
                var validation = await _validationHelper.ValidateExistsAsync(id);
                if (!validation.IsValid)
                {
                    return ServiceResult<bool>.Failure(validation.ErrorMessage);
                }

                var model = validation.MedicalCase!;

                // 验证是否可以暂停
                var suspendValidation = _validationHelper.ValidateCanSuspend(model);
                if (!suspendValidation.IsValid)
                {
                    return ServiceResult<bool>.Failure(suspendValidation.ErrorMessage);
                }

                _logger.LogInformation("暂停医疗案例: {CaseId}, 原因: {Reason}", id, reason);

                // 更新状态
                model.Status = MedicalCaseStatus.Suspended;
                if (!string.IsNullOrWhiteSpace(reason))
                    model.Remark = reason;

                var result = await _repository.UpdateAsync(model);
                var success = result != null;

                if (success)
                {
                    _logger.LogInformation("医疗案例暂停成功: {CaseId}", id);
                }

                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("暂停医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 恢复医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
        {
            try
            {
                // 验证案例存在性
                var validation = await _validationHelper.ValidateExistsAsync(id);
                if (!validation.IsValid)
                {
                    return ServiceResult<bool>.Failure(validation.ErrorMessage);
                }

                var model = validation.MedicalCase!;

                // 验证是否可以恢复
                var resumeValidation = _validationHelper.ValidateCanResume(model);
                if (!resumeValidation.IsValid)
                {
                    return ServiceResult<bool>.Failure(resumeValidation.ErrorMessage);
                }

                _logger.LogInformation("恢复医疗案例: {CaseId}", id);

                // 更新状态
                model.Status = MedicalCaseStatus.InConsultation;
                var result = await _repository.UpdateAsync(model);
                var success = result != null;

                if (success)
                {
                    _logger.LogInformation("医疗案例恢复成功: {CaseId}", id);
                }

                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("恢复医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 归档医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
        {
            try
            {
                // 验证案例存在性
                var validation = await _validationHelper.ValidateExistsAsync(id);
                if (!validation.IsValid)
                {
                    return ServiceResult<bool>.Failure(validation.ErrorMessage);
                }

                var model = validation.MedicalCase!;

                // 验证是否可以归档
                var archiveValidation = _validationHelper.ValidateCanArchive(model);
                if (!archiveValidation.IsValid)
                {
                    return ServiceResult<bool>.Failure(archiveValidation.ErrorMessage);
                }

                _logger.LogInformation("归档医疗案例: {CaseId}, 原因: {Reason}", id, archiveReason);

                // 更新状态
                model.Status = MedicalCaseStatus.Archived;
                if (!string.IsNullOrWhiteSpace(archiveReason))
                    model.Remark = archiveReason;

                var result = await _repository.UpdateAsync(model);
                var success = result != null;

                if (success)
                {
                    _logger.LogInformation("医疗案例归档成功: {CaseId}", id);
                }

                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "归档医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("归档医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, int status)
        {
            try
            {
                // 验证案例存在性
                var validation = await _validationHelper.ValidateExistsAsync(id);
                if (!validation.IsValid)
                {
                    return ServiceResult<bool>.Failure(validation.ErrorMessage);
                }

                var model = validation.MedicalCase!;

                // 验证状态值有效性
                if (!Enum.IsDefined(typeof(MedicalCaseStatus), status))
                {
                    return ServiceResult<bool>.Failure($"无效的状态值: {status}");
                }

                var newStatus = (MedicalCaseStatus)status;

                // 验证状态转换
                var statusValidation = _validationHelper.ValidateStatusTransition(model.Status, newStatus);
                if (!statusValidation.IsValid)
                {
                    return ServiceResult<bool>.Failure(statusValidation.ErrorMessage);
                }

                _logger.LogInformation("更新医疗案例状态: {CaseId} {OldStatus} -> {NewStatus}", 
                    id, model.Status, newStatus);

                // 更新状态
                model.Status = newStatus;
                var result = await _repository.UpdateAsync(model);
                var success = result != null;

                if (success)
                {
                    _logger.LogInformation("医疗案例状态更新成功: {CaseId}", id);
                }

                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败: {Id}", id);
                return ServiceResult<bool>.Failure("更新案例状态失败", ex);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 批量更新案例状态
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(Guid[] ids, MedicalCaseStatus status)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return ServiceResult<int>.Failure("案例ID列表不能为空");
                }

                _logger.LogInformation("批量更新医疗案例状态: {Count}个案例 -> {Status}", ids.Length, status);

                int successCount = 0;
                foreach (var id in ids)
                {
                    var result = await UpdateStatusAsync(id, (int)status);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                }

                _logger.LogInformation("批量更新完成: 成功{SuccessCount}/{TotalCount}", successCount, ids.Length);
                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新医疗案例状态失败");
                return ServiceResult<int>.Failure("批量更新失败", ex);
            }
        }

        /// <summary>
        /// 复制医疗案例（创建副本）
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CloneAsync(Guid id)
        {
            try
            {
                // 验证原案例存在性
                var validation = await _validationHelper.ValidateExistsAsync(id);
                if (!validation.IsValid)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validation.ErrorMessage);
                }

                var originalCase = validation.MedicalCase!;
                _logger.LogInformation("复制医疗案例: {OriginalId}", id);

                // 创建副本
                var cloneCase = new LYBT.Entities.MedicalCase.MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = originalCase.PatientId,
                    DoctorId = originalCase.DoctorId,
                    Status = MedicalCaseStatus.InConsultation, // 新案例从初始状态开始
                    Remark = $"复制自案例 {originalCase.Id}: {originalCase.Remark}"
                    // UltraThink v2.0简化：不包含CreateTime等字段
                };

                var created = await _repository.AddAsync(cloneCase);
                var createdDto = _mapper.Map<MedicalCaseDto>(created);

                _logger.LogInformation("医疗案例复制成功: {OriginalId} -> {CloneId}", id, created.Id);
                return ServiceResult<MedicalCaseDto>.Success(createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制医疗案例失败: {Id}", id);
                return ServiceResult<MedicalCaseDto>.Failure("复制医疗案例失败", ex);
            }
        }

        #endregion
    }
}