using FluentValidation.Results;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Components
{
    /// <summary>
    /// 组件验证器基类
    /// OpenSpec: optimize-desktop-code-reuse Phase 2 - 提取公共的异常处理和日志记录逻辑
    ///
    /// 职责:
    /// - 统一的异常处理框架
    /// - 日志记录标准化
    /// - 子类只需实现核心验证逻辑
    /// </summary>
    public abstract class ComponentValidatorBase : IComponentValidator
    {
        #region 字段

        protected readonly ILogger Logger;

        #endregion

        #region 构造函数

        protected ComponentValidatorBase(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IComponentValidator实现

        /// <summary>
        /// 异步验证数据（模板方法）
        /// </summary>
        public async Task<ValidationResult> ValidateAsync()
        {
            try
            {
                Logger.LogDebug("{ValidatorName} 开始异步验证", GetType().Name);

                var result = await ValidateAsyncCore();

                Logger.LogDebug("{ValidatorName} 验证完成: {IsValid}, 错误数: {ErrorCount}",
                    GetType().Name, result.IsValid, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{ValidatorName} 验证过程发生错误", GetType().Name);
                return CreateErrorResult($"验证过程发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步验证数据（模板方法）
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            try
            {
                Logger.LogDebug("{ValidatorName} 开始同步验证", GetType().Name);

                var isValid = IsValidCore(out errorMessage);

                if (isValid)
                {
                    Logger.LogDebug("{ValidatorName} 同步验证通过", GetType().Name);
                }
                else
                {
                    Logger.LogDebug("{ValidatorName} 同步验证失败: {ErrorMessage}", GetType().Name, errorMessage);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{ValidatorName} 同步验证过程发生错误", GetType().Name);
                errorMessage = $"验证过程发生错误: {ex.Message}";
                return false;
            }
        }

        #endregion

        #region 抽象方法（子类必须实现）

        /// <summary>
        /// 核心异步验证逻辑（子类实现）
        /// </summary>
        protected abstract Task<ValidationResult> ValidateAsyncCore();

        /// <summary>
        /// 核心同步验证逻辑（子类实现）
        /// </summary>
        protected abstract bool IsValidCore(out string errorMessage);

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建错误验证结果
        /// </summary>
        protected static ValidationResult CreateErrorResult(string errorMessage, string propertyName = "Validation")
        {
            return new ValidationResult(new[]
            {
                new ValidationFailure(propertyName, errorMessage)
            });
        }

        /// <summary>
        /// 创建空数据错误结果
        /// </summary>
        protected static ValidationResult CreateNullDataResult(string dataName)
        {
            return new ValidationResult(new[]
            {
                new ValidationFailure(dataName, $"{dataName}不能为空")
            });
        }

        #endregion
    }
}
