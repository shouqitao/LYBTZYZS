using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 查询服务基接口 - CQRS模式的Query端
    /// 所有只读操作都应该继承此接口
    /// </summary>
    public interface IQueryService<TDto>
        where TDto : class
    {
        /// <summary>
        /// 根据ID获取单个实体
        /// </summary>
        Task<ServiceResult<TDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有实体（谨慎使用，建议用分页）
        /// </summary>
        Task<ServiceResult<List<TDto>>> GetAllAsync();

        /// <summary>
        /// 分页查询
        /// </summary>
        Task<ServiceResult<PagedResult<TDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 搜索
        /// </summary>
        Task<ServiceResult<List<TDto>>> SearchAsync(string keyword);
    }
}
