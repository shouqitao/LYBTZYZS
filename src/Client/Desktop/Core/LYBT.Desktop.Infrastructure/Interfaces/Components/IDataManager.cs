using FluentValidation.Results;

namespace LYBT.Desktop.Infrastructure.Interfaces.Components
{
    /// <summary>
    /// 数据管理器接口 - 组件化MVVM架构核心接口
    /// Issue #1776 Task 3: 组件化基础设施搭建
    ///
    /// 职责：
    /// 1. 实体数据的CRUD操作
    /// 2. 数据状态管理（变更检测）
    /// 3. 数据加载和保存逻辑封装
    ///
    /// 设计原则：
    /// - 单一职责：仅负责数据管理，不涉及UI逻辑
    /// - 泛型设计：支持任意DTO类型
    /// - 异步优先：所有I/O操作使用async/await
    /// </summary>
    /// <typeparam name="TDto">实体DTO类型</typeparam>
    public interface IDataManager<TDto> where TDto : class
    {
        /// <summary>
        /// 当前实体数据
        /// </summary>
        TDto? CurrentEntity { get; }

        /// <summary>
        /// 是否有未保存的变更
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 初始化数据（加载现有数据或创建新数据）
        /// </summary>
        /// <param name="entityId">实体ID（Guid.Empty表示创建新实体）</param>
        Task InitializeAsync(Guid entityId);

        /// <summary>
        /// 保存数据（创建或更新）
        /// </summary>
        /// <returns>保存是否成功</returns>
        Task<bool> SaveAsync();

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <returns>删除是否成功</returns>
        Task<bool> DeleteAsync();

        /// <summary>
        /// 重置数据到原始状态（撤销所有变更）
        /// </summary>
        void Reset();

        /// <summary>
        /// 标记数据已变更
        /// </summary>
        void MarkAsChanged();
    }
}
