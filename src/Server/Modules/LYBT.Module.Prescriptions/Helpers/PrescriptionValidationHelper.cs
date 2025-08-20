using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using LYBT.Entities.Prescriptions;

namespace LYBT.Module.Prescriptions.Helpers
{
    /// <summary>
    /// PrescriptionService验证助手类 - UltraThink Helper模式
    /// 负责所有验证、业务规则检查和参数验证逻辑
    /// </summary>
    public class PrescriptionValidationHelper
    {
        private readonly IIntelligentPrescriptionService _intelligentService;
        private readonly ILogger<PrescriptionValidationHelper> _logger;

        public PrescriptionValidationHelper(
            IIntelligentPrescriptionService intelligentService,
            ILogger<PrescriptionValidationHelper> logger)
        {
            _intelligentService = intelligentService ?? throw new ArgumentNullException(nameof(intelligentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 处方数据验证

        /// <summary>
        /// 验证处方创建数据
        /// </summary>
        public async Task<ServiceResult<PrescriptionValidationResult>> ValidateCreateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                var result = new PrescriptionValidationResult();

                // 基本字段验证
                ValidateBasicFields(dto, result);

                // 处方项目验证
                ValidatePrescriptionItems(dto.Items, result);

                // 智能验证（药材重复和可用性检查）
                if (dto.Items != null && dto.Items.Any() && !result.Errors.Any())
                {
                    await ValidateIntelligentChecks(dto.Items, result);
                }

                result.IsValid = !result.Errors.Any();
                return ServiceResult<PrescriptionValidationResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方创建数据失败");
                return ServiceResult<PrescriptionValidationResult>.Failure("验证处方创建数据失败", ex);
            }
        }

        /// <summary>
        /// 验证处方更新数据
        /// </summary>
        public async Task<ServiceResult<PrescriptionValidationResult>> ValidateUpdateAsync(PrescriptionEditDto dto)
        {
            try
            {
                var result = new PrescriptionValidationResult();

                // 基本字段验证
                if (dto.Id == Guid.Empty)
                {
                    result.Errors.Add("处方ID不能为空");
                }

                if (dto.PatientId == Guid.Empty)
                {
                    result.Errors.Add("患者ID不能为空");
                }

                // 处方项目验证
                ValidatePrescriptionItems(dto.Items, result);

                // 智能验证（药材重复和可用性检查）
                if (dto.Items != null && dto.Items.Any() && !result.Errors.Any())
                {
                    var createItems = dto.Items.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Usage = item.Usage,
                        Remark = item.Remark
                    }).ToList();

                    await ValidateIntelligentChecks(createItems, result);
                }

                result.IsValid = !result.Errors.Any();
                return ServiceResult<PrescriptionValidationResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方更新数据失败");
                return ServiceResult<PrescriptionValidationResult>.Failure("验证处方更新数据失败", ex);
            }
        }

        /// <summary>
        /// 验证处方基本字段
        /// </summary>
        private void ValidateBasicFields(PrescriptionCreateDto dto, PrescriptionValidationResult result)
        {
            // 患者ID验证
            if (dto.PatientId == Guid.Empty)
            {
                result.Errors.Add("患者ID不能为空");
            }

            // 医生ID验证
            if (dto.DoctorId == Guid.Empty)
            {
                result.Errors.Add("医生ID不能为空");
            }

            // 剂数验证
            if (dto.DosageCount <= 0)
            {
                result.Errors.Add("剂数必须大于0");
            }

            if (dto.DosageCount > 999)
            {
                result.Errors.Add("剂数不能超过999");
            }

            // 备注长度验证
            if (!string.IsNullOrEmpty(dto.Remark) && dto.Remark.Length > 500)
            {
                result.Errors.Add("备注长度不能超过500个字符");
            }

            // 服用方法长度验证
            if (!string.IsNullOrEmpty(dto.Advice) && dto.Advice.Length > 1000)
            {
                result.Errors.Add("服用方法长度不能超过1000个字符");
            }

            // 处方项目不能为空
            if (dto.Items == null || !dto.Items.Any())
            {
                result.Errors.Add("处方药品不能为空");
            }
        }

        /// <summary>
        /// 验证处方项目
        /// </summary>
        private void ValidatePrescriptionItems(List<PrescriptionItemCreateDto> items, PrescriptionValidationResult result)
        {
            if (items == null || !items.Any())
            {
                result.Errors.Add("处方药品不能为空");
                return;
            }

            if (items.Count > 50)
            {
                result.Errors.Add("处方药品数量不能超过50种");
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var prefix = $"第{i + 1}个药品";

                // 药材ID验证
                if (item.HerbId == Guid.Empty)
                {
                    result.Errors.Add($"{prefix}: 药材ID不能为空");
                }

                // 药材名称验证
                if (string.IsNullOrWhiteSpace(item.HerbName))
                {
                    result.Errors.Add($"{prefix}: 药材名称不能为空");
                }
                else if (item.HerbName.Length > 100)
                {
                    result.Errors.Add($"{prefix}: 药材名称长度不能超过100个字符");
                }

                // 数量验证
                if (item.Quantity <= 0)
                {
                    result.Errors.Add($"{prefix}: 数量必须大于0");
                }

                if (item.Quantity > 9999)
                {
                    result.Errors.Add($"{prefix}: 数量不能超过9999");
                }

                // 单位验证
                if (string.IsNullOrWhiteSpace(item.Unit))
                {
                    result.Errors.Add($"{prefix}: 单位不能为空");
                }
                else if (item.Unit.Length > 10)
                {
                    result.Errors.Add($"{prefix}: 单位长度不能超过10个字符");
                }

                // 单价验证
                if (item.UnitPrice < 0)
                {
                    result.Errors.Add($"{prefix}: 单价不能为负数");
                }

                if (item.UnitPrice > 99999)
                {
                    result.Errors.Add($"{prefix}: 单价不能超过99999");
                }

                // 用法验证
                if (!string.IsNullOrEmpty(item.Usage) && item.Usage.Length > 200)
                {
                    result.Errors.Add($"{prefix}: 用法长度不能超过200个字符");
                }

                // 备注验证
                if (!string.IsNullOrEmpty(item.Remark) && item.Remark.Length > 200)
                {
                    result.Errors.Add($"{prefix}: 备注长度不能超过200个字符");
                }
            }
        }

        /// <summary>
        /// 智能验证检查（药材重复和可用性）
        /// </summary>
        private async Task ValidateIntelligentChecks(List<PrescriptionItemCreateDto> items, PrescriptionValidationResult result)
        {
            try
            {
                var prescriptionItems = items.Select(item => new PrescriptionItemModel
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity
                }).ToList();

                // 检测重复药材
                var duplicateResult = _intelligentService.DetectDuplicateHerbs(prescriptionItems);
                if (duplicateResult.HasDuplicates)
                {
                    result.Warnings.Add($"发现重复药材: {string.Join(", ", duplicateResult.DuplicateHerbs)}");
                }

                // 检查药材可用性
                var availabilityResult = await _intelligentService.CheckHerbAvailabilityAsync(prescriptionItems);
                if (!availabilityResult.IsAvailable)
                {
                    result.Warnings.Add($"部分药材不可用: {string.Join(", ", availabilityResult.UnavailableHerbs)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "智能验证检查失败，跳过此步骤");
                result.Warnings.Add("智能验证服务暂时不可用，请手工检查药材重复和可用性");
            }
        }

        #endregion

        #region 业务规则验证

        /// <summary>
        /// 验证处方状态转换
        /// </summary>
        public ServiceResult<bool> ValidateStatusTransition(PrescriptionStatus currentStatus, PrescriptionStatus newStatus)
        {
            try
            {
                // 定义允许的状态转换
                var allowedTransitions = new Dictionary<PrescriptionStatus, List<PrescriptionStatus>>
                {
                    { PrescriptionStatus.Draft, new List<PrescriptionStatus> { PrescriptionStatus.Draft, PrescriptionStatus.Completed } },
                    { PrescriptionStatus.Completed, new List<PrescriptionStatus> { PrescriptionStatus.Draft } } // 允许退回
                };

                if (!allowedTransitions.ContainsKey(currentStatus))
                {
                    return ServiceResult<bool>.Failure($"不支持的当前状态: {currentStatus}");
                }

                if (!allowedTransitions[currentStatus].Contains(newStatus))
                {
                    return ServiceResult<bool>.Failure($"不允许从状态 {currentStatus} 转换到 {newStatus}");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方状态转换失败");
                return ServiceResult<bool>.Failure("验证处方状态转换失败", ex);
            }
        }

        /// <summary>
        /// 验证处方是否可以删除
        /// </summary>
        public ServiceResult<bool> ValidateCanDelete(PrescriptionStatus status)
        {
            try
            {
                // 只有草稿状态的处方可以删除
                if (status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<bool>.Failure("只有草稿状态的处方可以删除");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方是否可以删除失败");
                return ServiceResult<bool>.Failure("验证处方是否可以删除失败", ex);
            }
        }

        /// <summary>
        /// 验证处方是否可以编辑
        /// </summary>
        public ServiceResult<bool> ValidateCanEdit(PrescriptionStatus status)
        {
            try
            {
                // 只有草稿状态的处方可以编辑
                if (status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<bool>.Failure("只有草稿状态的处方可以编辑");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方是否可以编辑失败");
                return ServiceResult<bool>.Failure("验证处方是否可以编辑失败", ex);
            }
        }

        /// <summary>
        /// 验证处方是否可以提交
        /// </summary>
        public ServiceResult<bool> ValidateCanSubmit(LYBT.Entities.Prescriptions.Prescription prescription)
        {
            try
            {
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                if (prescription.Status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<bool>.Failure("只有草稿状态的处方可以提交");
                }

                if (!prescription.Items.Any())
                {
                    return ServiceResult<bool>.Failure("处方必须包含至少一个药品");
                }

                if (prescription.DosageCount <= 0)
                {
                    return ServiceResult<bool>.Failure("剂数必须大于0");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方是否可以提交失败");
                return ServiceResult<bool>.Failure("验证处方是否可以提交失败", ex);
            }
        }

        /// <summary>
        /// 验证处方是否可以批准
        /// </summary>
        public ServiceResult<bool> ValidateCanApprove(PrescriptionStatus status)
        {
            try
            {
                // 草稿状态的处方可以批准
                if (status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<bool>.Failure("只有草稿状态的处方可以批准");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方是否可以批准失败");
                return ServiceResult<bool>.Failure("验证处方是否可以批准失败", ex);
            }
        }

        /// <summary>
        /// 验证处方是否可以拒绝
        /// </summary>
        public ServiceResult<bool> ValidateCanReject(PrescriptionStatus status)
        {
            try
            {
                // 已完成状态的处方可以拒绝（退回草稿）
                if (status != PrescriptionStatus.Completed)
                {
                    return ServiceResult<bool>.Failure("只有已完成状态的处方可以拒绝");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方是否可以拒绝失败");
                return ServiceResult<bool>.Failure("验证处方是否可以拒绝失败", ex);
            }
        }

        #endregion

        #region 参数验证

        /// <summary>
        /// 验证GUID格式
        /// </summary>
        public ServiceResult<Guid> ValidateGuidFormat(string guidString, string fieldName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(guidString))
                {
                    return ServiceResult<Guid>.Failure($"{fieldName}不能为空");
                }

                if (!Guid.TryParse(guidString, out var guid))
                {
                    return ServiceResult<Guid>.Failure($"{fieldName}格式不正确");
                }

                if (guid == Guid.Empty)
                {
                    return ServiceResult<Guid>.Failure($"{fieldName}不能为空GUID");
                }

                return ServiceResult<Guid>.Success(guid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证GUID格式失败: {FieldName}, {Value}", fieldName, guidString);
                return ServiceResult<Guid>.Failure($"验证{fieldName}失败", ex);
            }
        }

        /// <summary>
        /// 验证字符串长度
        /// </summary>
        public ServiceResult<bool> ValidateStringLength(string value, string fieldName, int maxLength, bool required = false)
        {
            try
            {
                if (required && string.IsNullOrWhiteSpace(value))
                {
                    return ServiceResult<bool>.Failure($"{fieldName}不能为空");
                }

                if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
                {
                    return ServiceResult<bool>.Failure($"{fieldName}长度不能超过{maxLength}个字符");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证字符串长度失败: {FieldName}", fieldName);
                return ServiceResult<bool>.Failure($"验证{fieldName}失败", ex);
            }
        }

        /// <summary>
        /// 验证数值范围
        /// </summary>
        public ServiceResult<bool> ValidateNumberRange(decimal value, string fieldName, decimal min, decimal max)
        {
            try
            {
                if (value < min)
                {
                    return ServiceResult<bool>.Failure($"{fieldName}不能小于{min}");
                }

                if (value > max)
                {
                    return ServiceResult<bool>.Failure($"{fieldName}不能大于{max}");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证数值范围失败: {FieldName}, {Value}", fieldName, value);
                return ServiceResult<bool>.Failure($"验证{fieldName}失败", ex);
            }
        }

        /// <summary>
        /// 验证快速保存数据
        /// </summary>
        public ServiceResult<bool> ValidateQuickSave(QuickPrescriptionDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ServiceResult<bool>.Failure("快速保存数据不能为空");
                }

                // 诊断信息长度验证
                var diagnosisValidation = ValidateStringLength(dto.Diagnosis, "诊断信息", 500, false);
                if (!diagnosisValidation.IsSuccess)
                {
                    return diagnosisValidation;
                }

                // 服用方法长度验证
                var adviceValidation = ValidateStringLength(dto.Advice, "服用方法", 1000, false);
                if (!adviceValidation.IsSuccess)
                {
                    return adviceValidation;
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证快速保存数据失败");
                return ServiceResult<bool>.Failure("验证快速保存数据失败", ex);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查是否为有效的处方状态
        /// </summary>
        public bool IsValidPrescriptionStatus(PrescriptionStatus status)
        {
            return Enum.IsDefined(typeof(PrescriptionStatus), status);
        }

        /// <summary>
        /// 获取状态转换错误信息
        /// </summary>
        public string GetStatusTransitionErrorMessage(PrescriptionStatus currentStatus, PrescriptionStatus newStatus)
        {
            return $"不能将处方状态从 {GetStatusDisplayName(currentStatus)} 更改为 {GetStatusDisplayName(newStatus)}";
        }

        /// <summary>
        /// 获取状态显示名称
        /// </summary>
        private string GetStatusDisplayName(PrescriptionStatus status)
        {
            return status switch
            {
                PrescriptionStatus.Draft => "草稿",
                PrescriptionStatus.Completed => "已完成",
                _ => status.ToString()
            };
        }

        #endregion
    }
}