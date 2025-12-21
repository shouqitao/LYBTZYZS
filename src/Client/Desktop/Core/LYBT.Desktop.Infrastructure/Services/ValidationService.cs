using FluentValidation;
using FluentValidation.Results;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>验证服务实现 - 集成FluentValidation</summary>
    public class ValidationService : IValidationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(IServiceProvider serviceProvider, ILogger<ValidationService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ValidationResult> ValidateAsync<T>(T dto) where T : class
        {
            if (dto == null) return new ValidationResult(new[] { new ValidationFailure(typeof(T).Name, "验证对象不能为空") });
            try
            {
                var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;
                if (validator == null) { _logger.LogDebug("未找到类型 {TypeName} 的Validator，跳过验证", typeof(T).Name); return new ValidationResult(); }
                var result = await validator.ValidateAsync(dto);
                _logger.LogDebug("验证 {TypeName}: {IsValid}, 错误数: {ErrorCount}", typeof(T).Name, result.IsValid, result.Errors.Count);
                return result;
            }
            catch (Exception ex) { _logger.LogError(ex, "验证 {TypeName} 时发生错误", typeof(T).Name); return new ValidationResult(new[] { new ValidationFailure(typeof(T).Name, ClientErrorMessageMapper.GetSafeOperationFailureMessage("验证", ex)) }); }
        }

        public ValidationResult Validate<T>(T dto) where T : class
        {
            if (dto == null) return new ValidationResult(new[] { new ValidationFailure(typeof(T).Name, "验证对象不能为空") });
            try
            {
                var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;
                if (validator == null) { _logger.LogDebug("未找到类型 {TypeName} 的Validator，跳过验证", typeof(T).Name); return new ValidationResult(); }
                var result = validator.Validate(dto);
                _logger.LogDebug("验证 {TypeName}: {IsValid}, 错误数: {ErrorCount}", typeof(T).Name, result.IsValid, result.Errors.Count);
                return result;
            }
            catch (Exception ex) { _logger.LogError(ex, "验证 {TypeName} 时发生错误", typeof(T).Name); return new ValidationResult(new[] { new ValidationFailure(typeof(T).Name, ClientErrorMessageMapper.GetSafeOperationFailureMessage("验证", ex)) }); }
        }

        public bool IsValid<T>(T dto, out string errorMessage) where T : class
        {
            errorMessage = string.Empty;
            if (dto == null) { errorMessage = "验证对象不能为空"; return false; }
            try
            {
                var result = Validate(dto);
                if (result.IsValid) return true;
                errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                return false;
            }
            catch (Exception ex) { _logger.LogError(ex, "快速验证 {TypeName} 时发生错误", typeof(T).Name); errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("验证", ex); return false; }
        }
    }
}
