using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Validators.BusinessRules
{
    /// <summary>
    /// 业务规则验证器抽象基类
    /// Phase 3 Task 3.4: 统一业务规则验证框架
    /// </summary>
    public abstract class BaseBusinessRuleValidator : IBusinessRuleValidator
    {
        protected readonly ILogger _logger;

        protected BaseBusinessRuleValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public abstract string ValidatorName { get; }
        public abstract string Description { get; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        protected ValidationResult Success()
        {
            return ValidationResult.Success();
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        protected ValidationResult Failure(string message)
        {
            _logger.LogWarning("业务规则验证失败: {ValidatorName} - {Message}", ValidatorName, message);
            return ValidationResult.Failure(message);
        }

        /// <summary>
        /// 记录验证成功日志
        /// </summary>
        protected void LogSuccess(string operation, object? details = null)
        {
            if (details != null)
            {
                _logger.LogInformation("业务规则验证成功: {ValidatorName} - {Operation} - {Details}",
                    ValidatorName, operation, details);
            }
            else
            {
                _logger.LogInformation("业务规则验证成功: {ValidatorName} - {Operation}", ValidatorName, operation);
            }
        }
    }

    /// <summary>
    /// 业务规则验证器泛型抽象基类
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public abstract class BaseBusinessRuleValidator<TEntity> : BaseBusinessRuleValidator, IBusinessRuleValidator<TEntity>
    {
        protected BaseBusinessRuleValidator(ILogger logger) : base(logger) { }

        public abstract Task<ValidationResult> ValidateAsync(TEntity entity, ValidationContext? context = null);

        public virtual async Task<ValidationResult> ValidateAsync(IEnumerable<TEntity> entities, ValidationContext? context = null)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return Success();
            }

            foreach (var entity in entityList)
            {
                var result = await ValidateAsync(entity, context);
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return Success();
        }
    }

    /// <summary>
    /// 业务操作验证器泛型抽象基类
    /// </summary>
    /// <typeparam name="TInput">输入DTO类型</typeparam>
    public abstract class BaseBusinessOperationValidator<TInput> : BaseBusinessRuleValidator, IBusinessOperationValidator<TInput>
    {
        protected BaseBusinessOperationValidator(ILogger logger) : base(logger) { }

        public abstract Task<ValidationResult> ValidateAsync(TInput input, ValidationContext? context = null);
    }
}