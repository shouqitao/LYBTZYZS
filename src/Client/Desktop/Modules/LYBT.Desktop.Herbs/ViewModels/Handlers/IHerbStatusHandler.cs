using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.ViewModels.Handlers;

/// <summary>
/// 药材状态处理接口
/// </summary>
public interface IHerbStatusHandler
{
    /// <summary>
    /// 切换药材启用/禁用状态
    /// </summary>
    /// <param name="herb">药材信息</param>
    /// <returns>操作是否成功</returns>
    Task<bool> ToggleStatusAsync(HerbListDto herb);

    /// <summary>
    /// 恢复已删除的药材
    /// </summary>
    /// <param name="herb">药材信息</param>
    /// <returns>操作是否成功</returns>
    Task<bool> RestoreAsync(HerbListDto herb);
}
