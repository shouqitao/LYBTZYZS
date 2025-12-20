using FluentValidation.Results;

namespace LYBT.Desktop.Infrastructure.Interfaces.Components
{
    /// <summary>
    /// 验证服务接口 - 组件化MVVM架构核心接口
    /// Issue #1776 Task 3: 组件化基础设施搭建
    ///
    /// 职责：
    /// 1. 提供统一的验证入口
    /// 2. 集成FluentValidation验证器
    /// 3. 支持泛型DTO验证
    ///
    /// 设计原则：
    /// - 泛型设计：支持任意DTO类型验证
    /// - 异步优先：支持异步验证规则
    /// - 依赖注入：通过DI容器获取对应的Validator
    ///
    /// 注意：此接口保留在Infrastructure中，因为依赖FluentValidation.Results
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// 异步验证DTO对象
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <param name="dto">待验证的DTO对象</param>
        /// <returns>FluentValidation验证结果</returns>
        Task<ValidationResult> ValidateAsync<T>(T dto) where T : class;

        /// <summary>
        /// 同步验证DTO对象
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <param name="dto">待验证的DTO对象</param>
        /// <returns>FluentValidation验证结果</returns>
        ValidationResult Validate<T>(T dto) where T : class;

        /// <summary>
        /// 快速验证DTO对象（简化版本）
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <param name="dto">待验证的DTO对象</param>
        /// <param name="errorMessage">验证失败时的错误信息</param>
        /// <returns>验证是否通过</returns>
        bool IsValid<T>(T dto, out string errorMessage) where T : class;
    }
}
