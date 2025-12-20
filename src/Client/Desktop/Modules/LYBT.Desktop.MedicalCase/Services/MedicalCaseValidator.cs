using FluentValidation.Results;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 病案验证器 - 聚合根验证模式
    /// Issue #1778: MedicalCase模块组件化改造
    ///
    /// 职责:
    /// - 病案基本信息验证
    /// - 诊疗数据验证
    /// - 处方数据验证
    /// - 集成FluentValidation
    /// </summary>
    public class MedicalCaseValidator : IComponentValidator
    {
        #region 字段

        private readonly IValidationService _validationService;
        private readonly MedicalCaseDataManager _dataManager;
        private readonly ILogger<MedicalCaseValidator> _logger;

        #endregion

        #region 构造函数

        public MedicalCaseValidator(
            IValidationService validationService,
            MedicalCaseDataManager dataManager,
            ILogger<MedicalCaseValidator> logger)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IComponentValidator实现

        /// <summary>
        /// 异步验证聚合根(病案+诊疗+处方)
        /// </summary>
        public async Task<ValidationResult> ValidateAsync()
        {
            try
            {
                _logger.LogDebug("开始验证病案聚合根数据");

                var errors = new List<ValidationFailure>();

                // 验证病案基本信息
                if (_dataManager.Current == null)
                {
                    errors.Add(new ValidationFailure("MedicalCase", "病案数据不能为空"));
                    return new ValidationResult(errors);
                }

                // Epic #1961: 使用 ToInputDto() 转换并验证
                var medicalCaseResult = await _validationService.ValidateAsync(_dataManager.Current.ToInputDto());
                if (!medicalCaseResult.IsValid)
                {
                    errors.AddRange(medicalCaseResult.Errors);
                }

                // 验证诊疗数据
                if (_dataManager.CurrentConsultation != null)
                {
                    var consultationResult = await _validationService.ValidateAsync(_dataManager.CurrentConsultation.ToInputDto());
                    if (!consultationResult.IsValid)
                    {
                        errors.AddRange(consultationResult.Errors);
                    }
                }

                // 验证处方数据(如果存在)
                if (_dataManager.CurrentPrescription != null)
                {
                    var prescriptionResult = await _validationService.ValidateAsync(_dataManager.CurrentPrescription.ToPrescriptionInputDto());
                    if (!prescriptionResult.IsValid)
                    {
                        errors.AddRange(prescriptionResult.Errors);
                    }
                }

                var result = new ValidationResult(errors);
                _logger.LogDebug("病案聚合根验证完成: {IsValid}, 错误数: {ErrorCount}", result.IsValid, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "病案聚合根验证过程发生错误");
                return new ValidationResult(new[]
                {
                    new ValidationFailure("Validation", ClientErrorMessageMapper.GetSafeOperationFailureMessage("验证", ex))
                });
            }
        }

        /// <summary>
        /// 同步验证聚合根
        /// </summary>
        public virtual bool IsValid(out string errorMessage)
        {
            try
            {
                _logger.LogDebug("开始同步验证病案聚合根数据");

                // 验证病案基本信息
                if (_dataManager.Current == null)
                {
                    errorMessage = "病案数据不能为空";
                    return false;
                }

                // Epic #1961: 使用 ToInputDto() 转换并验证
                if (!_validationService.IsValid(_dataManager.Current.ToInputDto(), out var medicalCaseError))
                {
                    errorMessage = medicalCaseError;
                    return false;
                }

                // 验证诊疗数据
                if (_dataManager.CurrentConsultation != null)
                {
                    if (!_validationService.IsValid(_dataManager.CurrentConsultation.ToInputDto(), out var consultationError))
                    {
                        errorMessage = consultationError;
                        return false;
                    }
                }

                // 验证处方数据(如果存在)
                if (_dataManager.CurrentPrescription != null)
                {
                    if (!_validationService.IsValid(_dataManager.CurrentPrescription.ToPrescriptionInputDto(), out var prescriptionError))
                    {
                        errorMessage = prescriptionError;
                        return false;
                    }
                }

                errorMessage = string.Empty;
                _logger.LogDebug("病案聚合根同步验证通过");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "病案聚合根同步验证过程发生错误");
                errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("验证", ex);
                return false;
            }
        }

        /// <summary>
        /// 验证特定属性
        /// </summary>
        public async Task<ValidationResult> ValidatePropertyAsync(string propertyName)
        {
            try
            {
                _logger.LogDebug("开始验证属性: {PropertyName}", propertyName);

                if (_dataManager.Current == null)
                {
                    return new ValidationResult(new[]
                    {
                        new ValidationFailure(propertyName, "病案数据不能为空")
                    });
                }

                // 执行完整验证
                var fullResult = await ValidateAsync();

                // 过滤出特定属性的错误
                var propertyErrors = fullResult.Errors
                    .Where(e => e.PropertyName == propertyName)
                    .ToList();

                return new ValidationResult(propertyErrors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证属性失败: {PropertyName}", propertyName);
                return new ValidationResult(new[]
                {
                    new ValidationFailure(propertyName, ClientErrorMessageMapper.GetSafeOperationFailureMessage("验证", ex))
                });
            }
        }

        /// <summary>
        /// 清除验证错误
        /// </summary>
        public void ClearErrors()
        {
            _logger.LogDebug("清除验证错误");
            // 验证错误由ValidationResult管理,无需额外操作
        }

        #endregion

        // [已移除] 三步流程相关验证方法 (CanCompleteStep1, CanMarkForPrescription, CanCreatePrescription, ValidateStepAsync)
        // 三步流程已取消，验证逻辑已简化
    }
}
