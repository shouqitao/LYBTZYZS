using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 命令服务基接口 - CQRS模式的Command端
    /// 所有写操作都应该继承此接口
    /// </summary>
    public interface ICommandService<TDto, TCreateDto, TUpdateDto>
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        /// <summary>
        /// 创建实体
        /// </summary>
        Task<ServiceResult<TDto>> CreateAsync(TCreateDto dto);

        /// <summary>
        /// 更新实体
        /// </summary>
        Task<ServiceResult<TDto>> UpdateAsync(Guid id, TUpdateDto dto);

        /// <summary>
        /// 删除实体
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除
        /// </summary>
        Task<ServiceResult<bool>> DeleteBatchAsync(List<Guid> ids);

        /// <summary>
        /// 验证实体
        /// </summary>
        Task<ServiceResult<bool>> ValidateAsync(TCreateDto dto);
    }
}