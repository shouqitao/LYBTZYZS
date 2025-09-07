using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Interfaces
{

    /// <summary>
    /// 基础服务接口 - Solution级架构标准化
    /// 定义所有模块服务的通用规范，确保架构一致性
    /// </summary>
    /// <typeparam name="TModel">实体模型类型</typeparam>
    /// <typeparam name="TDto">数据传输对象类型</typeparam>
    /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
    /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
    /// <typeparam name="TQueryDto">查询DTO类型</typeparam>
    public interface IBaseService<TModel, TDto, TCreateDto, TUpdateDto, TQueryDto>
        where TModel : class
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
        where TQueryDto : class
    {

        /// <summary>
        /// 获取分页数据
        /// </summary>
        Task<PagedResult<TDto>> GetPagedAsync(TQueryDto query);

        /// <summary>
        /// 根据ID获取详情
        /// </summary>
        Task<TDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建实体
        /// </summary>
        Task<TDto> CreateAsync(TCreateDto createDto);

        /// <summary>
        /// 更新实体
        /// </summary>
        Task<TDto> UpdateAsync(Guid id, TUpdateDto updateDto);

        /// <summary>
        /// 删除实体
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除
        /// </summary>
        Task<int> DeleteBatchAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// 检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// 获取实体总数
        /// </summary>
        Task<long> GetCountAsync();
    }

    /// <summary>
    /// 简化的基础服务接口 - 用于不需要完整CRUD的服务
    /// </summary>
    public interface IBaseReadOnlyService<TDto, TQueryDto>
        where TDto : class
        where TQueryDto : class
    {

        /// <summary>
        /// 获取分页数据
        /// </summary>
        Task<PagedResult<TDto>> GetPagedAsync(TQueryDto query);

        /// <summary>
        /// 根据ID获取详情
        /// </summary>
        Task<TDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取实体总数
        /// </summary>
        Task<long> GetCountAsync();
    }
}
