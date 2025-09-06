using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces;

/// <summary>
/// 药材业务服务接口 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public interface IHerbBusinessService {

    #region 基础业务操作

    /// <summary>
    /// 创建药材
    /// </summary>
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto);

    /// <summary>
    /// 更新药材
    /// </summary>
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto);

    /// <summary>
    /// 启用药材
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid herbId);

    /// <summary>
    /// 禁用药材
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid herbId);

    /// <summary>
    /// 删除药材
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid herbId);

    /// <summary>
    /// 批量导入药材
    /// </summary>
    Task<ServiceResult<object>> ImportHerbsAsync(List<HerbCreateDto> herbs);

    /// <summary>
    /// 导出药材数据
    /// </summary>
    Task<ServiceResult<byte[]>> ExportHerbsAsync(PagedQueryBaseDto query);

    #endregion 基础业务操作
}
