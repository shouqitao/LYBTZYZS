using FluentValidation.Results;

namespace LYBT.Desktop.Infrastructure.Interfaces.Components
{
    /// <summary>
    /// 组件验证器接口 - 组件化MVVM架构核心接口
    /// Issue #1776 Task 3: 组件化基础设施搭建
    ///
    /// 职责：
    /// 1. 集成FluentValidation Validators
    /// 2. 提供组件级验证接口
    /// 3. 统一验证结果处理
    ///
    /// 设计原则：
    /// - 依赖倒置：依赖IValidationService抽象
    /// - 单一职责：仅负责验证逻辑
    /// - 异步优先：支持异步验证规则
    ///
    /// 注意：此接口保留在Infrastructure中，因为依赖FluentValidation.Results
    /// </summary>
    public interface IComponentValidator
    {
        /// <summary>
        /// 异步验证当前数据
        /// </summary>
        /// <returns>FluentValidation验证结果</returns>
        Task<ValidationResult> ValidateAsync();

        /// <summary>
        /// 同步验证当前数据（快速验证）
        /// </summary>
        /// <param name="errorMessage">验证失败时的错误信息</param>
        /// <returns>验证是否通过</returns>
        bool IsValid(out string errorMessage);
    }
}
