using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{

    /// <summary>
    /// 中药材业务服务接口 - UltraThink双层架构Business层抽象
    /// 职责：业务逻辑处理、批量操作、状态管理等业务功能
    /// </summary>
    public interface IHerbBusinessService
    {

        /// <summary>
        /// 批量导入中药材
        /// </summary>
        Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs);

        /// <summary>
        /// 批量更新状态
        /// </summary>
        Task<ServiceResult<bool>> BatchUpdateStatusAsync(List<Guid> ids, bool status, string? reason = null);

        /// <summary>
        /// 软删除中药材
        /// </summary>
        Task<ServiceResult<bool>> SoftDeleteAsync(Guid id);

        /// <summary>
        /// 创建中药材（自动生成编码）
        /// </summary>
        Task<ServiceResult<HerbDto>> CreateHerbWithAutoCodeAsync(HerbCreateDto dto);

        /// <summary>
        /// 更新中药材信息
        /// </summary>
        Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);

        /// <summary>
        /// 设置中药材状态
        /// </summary>
        Task<ServiceResult<bool>> SetStatusAsync(Guid id, bool isActive);
    }
}