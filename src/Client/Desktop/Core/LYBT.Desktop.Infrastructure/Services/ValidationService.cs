using FluentValidation;
using FluentValidation.Results;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 验证服务实现 - 集成FluentValidation
    /// Issue #1776 Task 3: 组件化基础设施搭建
    ///
    /// 职责：
    /// 1. 通过DI容器获取对应的Validator
    /// 2. 执行FluentValidation验证
    /// 3. 返回统一的ValidationResult
    ///
    /// 设计原则：
    /// - 泛型设计：支持任意DTO类型
    /// - 依赖注入：通过IServiceProvider获取Validator
    /// - 容错处理：Validator不存在时返回成功（可选验证）
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(
            IServiceProvider serviceProvider,
            ILogger<ValidationService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 异步验证DTO对象
        /// </summary>
        public async Task<ValidationResult> ValidateAsync<T>(T dto) where T : class
        {
            if (dto == null)
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure(typeof(T).Name, "验证对象不能为空")
                });
            }

            try
            {
                // 从DI容器获取对应的Validator
                var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;

                if (validator == null)
                {
                    // Validator不存在时，记录日志但返回成功（可选验证）
                    _logger.LogDebug("未找到类型 {TypeName} 的Validator，跳过验证", typeof(T).Name);
                    return new ValidationResult();
                }

                // 执行FluentValidation验证
                var result = await validator.ValidateAsync(dto);
                _logger.LogDebug("验证 {TypeName}: {IsValid}, 错误数: {ErrorCount}",
                    typeof(T).Name, result.IsValid, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证 {TypeName} 时发生错误", typeof(T).Name);
                return new ValidationResult(new[]
                {
                    new ValidationFailure(typeof(T).Name, $"验证过程发生错误: {ex.Message}")
                });
            }
        }

        /// <summary>
        /// 同步验证DTO对象
        /// </summary>
        public ValidationResult Validate<T>(T dto) where T : class
        {
            if (dto == null)
            {
                return new ValidationResult(new[]
                {
                    new ValidationFailure(typeof(T).Name, "验证对象不能为空")
                });
            }

            try
            {
                // 从DI容器获取对应的Validator
                var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;

                if (validator == null)
                {
                    // Validator不存在时，记录日志但返回成功（可选验证）
                    _logger.LogDebug("未找到类型 {TypeName} 的Validator，跳过验证", typeof(T).Name);
                    return new ValidationResult();
                }

                // 执行FluentValidation验证
                var result = validator.Validate(dto);
                _logger.LogDebug("验证 {TypeName}: {IsValid}, 错误数: {ErrorCount}",
                    typeof(T).Name, result.IsValid, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证 {TypeName} 时发生错误", typeof(T).Name);
                return new ValidationResult(new[]
                {
                    new ValidationFailure(typeof(T).Name, $"验证过程发生错误: {ex.Message}")
                });
            }
        }

        /// <summary>
        /// 快速验证DTO对象（简化版本）
        /// </summary>
        public bool IsValid<T>(T dto, out string errorMessage) where T : class
        {
            errorMessage = string.Empty;

            if (dto == null)
            {
                errorMessage = "验证对象不能为空";
                return false;
            }

            try
            {
                var result = Validate(dto);

                if (result.IsValid)
                {
                    return true;
                }

                // 组合所有错误信息
                errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速验证 {TypeName} 时发生错误", typeof(T).Name);
                errorMessage = $"验证过程发生错误: {ex.Message}";
                return false;
            }
        }
    }
}
