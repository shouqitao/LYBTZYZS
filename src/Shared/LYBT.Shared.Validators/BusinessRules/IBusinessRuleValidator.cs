using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Validators.BusinessRules
{
    /// <summary>
    /// 业务规则验证器统一接口
    /// Phase 3 Task 3.4: 统一业务规则验证框架
    /// </summary>
    public interface IBusinessRuleValidator
    {
        /// <summary>
        /// 验证器名称（用于日志记录和调试）
        /// </summary>
        string ValidatorName { get; }

        /// <summary>
        /// 验证规则描述
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// 业务规则验证器泛型接口
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public interface IBusinessRuleValidator<TEntity> : IBusinessRuleValidator
    {
        /// <summary>
        /// 验证单个实体的业务规则
        /// </summary>
        /// <param name="entity">要验证的实体</param>
        /// <param name="context">验证上下文（如当前用户、操作类型等）</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateAsync(TEntity entity, ValidationContext? context = null);

        /// <summary>
        /// 验证多个实体的业务规则
        /// </summary>
        /// <param name="entities">要验证的实体列表</param>
        /// <param name="context">验证上下文</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateAsync(IEnumerable<TEntity> entities, ValidationContext? context = null);
    }

    /// <summary>
    /// 业务操作验证器泛型接口
    /// </summary>
    /// <typeparam name="TInput">输入DTO类型</typeparam>
    public interface IBusinessOperationValidator<TInput> : IBusinessRuleValidator
    {
        /// <summary>
        /// 验证业务操作
        /// </summary>
        /// <param name="input">输入数据</param>
        /// <param name="context">验证上下文</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateAsync(TInput input, ValidationContext? context = null);
    }
}