using FluentValidation.Results;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊断验证器
    /// Issue #1779: Consultation模块组件化改造
    ///
    /// 职责:
    /// - 诊断数据验证（必填字段）
    /// - 集成FluentValidation
    /// - Step1完成验证逻辑
    /// </summary>
    public class ConsultationValidator : IComponentValidator
    {
        #region 字段

        private readonly IValidationService _validationService;
        private readonly ConsultationDataManager _dataManager;
        private readonly ILogger<ConsultationValidator> _logger;

        #endregion

        #region 构造函数

        public ConsultationValidator(
            IValidationService validationService,
            ConsultationDataManager dataManager,
            ILogger<ConsultationValidator> logger)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IComponentValidator实现

        /// <summary>
        /// 异步验证诊断数据
        /// </summary>
        public async Task<ValidationResult> ValidateAsync()
        {
            try
            {
                _logger.LogDebug("开始验证诊断数据");

                var errors = new List<ValidationFailure>();

                // 验证诊断基本信息
                if (_dataManager.Current == null)
                {
                    errors.Add(new ValidationFailure("Consultation", "诊断数据不能为空"));
                    return new ValidationResult(errors);
                }

                // 使用 FluentValidation 验证
                var consultationResult = await _validationService.ValidateAsync(_dataManager.Current);
                if (!consultationResult.IsValid)
                {
                    errors.AddRange(consultationResult.Errors);
                }

                var result = new ValidationResult(errors);
                _logger.LogDebug("诊断验证完成: {IsValid}, 错误数: {ErrorCount}", result.IsValid, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "诊断验证过程发生错误");
                return new ValidationResult(new[]
                {
                    new ValidationFailure("Validation", $"验证过程发生错误: {ex.Message}")
                });
            }
        }

        /// <summary>
        /// 同步验证诊断数据
        /// </summary>
        public virtual bool IsValid(out string errorMessage)
        {
            try
            {
                _logger.LogDebug("开始同步验证诊断数据");

                // 验证诊断基本信息
                if (_dataManager.Current == null)
                {
                    errorMessage = "诊断数据不能为空";
                    return false;
                }

                // 使用 ValidationService 同步验证
                if (!_validationService.IsValid(_dataManager.Current, out var validationError))
                {
                    errorMessage = validationError;
                    return false;
                }

                errorMessage = string.Empty;
                _logger.LogDebug("诊断同步验证通过");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "诊断同步验证过程发生错误");
                errorMessage = $"验证过程发生错误: {ex.Message}";
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
                        new ValidationFailure(propertyName, "诊断数据不能为空")
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
                    new ValidationFailure(propertyName, $"验证过程发生错误: {ex.Message}")
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

        #region 专用验证方法

        /// <summary>
        /// 验证是否可以完成Step1（必填字段：主诉、中医诊断）
        /// </summary>
        public virtual bool CanCompleteStep1(out string errorMessage)
        {
            if (_dataManager.Current == null)
            {
                errorMessage = "诊断数据不能为空";
                return false;
            }

            // 检查必填字段
            if (string.IsNullOrWhiteSpace(_dataManager.Current.ChiefComplaint))
            {
                errorMessage = "主诉不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_dataManager.Current.TCMDiagnosis))
            {
                errorMessage = "中医诊断不能为空";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 验证表单数据（简化版，用于UI）
        /// </summary>
        public bool ValidateForm(out List<string> errors)
        {
            errors = new List<string>();

            if (_dataManager.Current == null)
            {
                errors.Add("诊断数据不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_dataManager.Current.ChiefComplaint))
            {
                errors.Add("主诉不能为空");
            }

            if (string.IsNullOrWhiteSpace(_dataManager.Current.TCMDiagnosis))
            {
                errors.Add("中医诊断不能为空");
            }

            return errors.Count == 0;
        }

        #endregion
    }
}
