using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// 职责：导入导出、批量操作、业务规则处理
    /// </summary>
    public interface IHerbBusinessService
    {
        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs);

        /// <summary>
        /// 批量更新状态
        /// </summary>
        Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto);

        /// <summary>
        /// 软删除药材
        /// </summary>
        Task<ServiceResult<bool>> SoftDeleteAsync(Guid id);

        /// <summary>
        /// 创建药材（带自动拼音码生成）
        /// </summary>
        Task<ServiceResult<HerbDto>> CreateHerbWithAutoCodeAsync(HerbCreateDto dto);

        /// <summary>
        /// 设置药材启用/禁用状态
        /// </summary>
        Task<ServiceResult<bool>> SetStatusAsync(Guid id, bool isActive);
    }
}
