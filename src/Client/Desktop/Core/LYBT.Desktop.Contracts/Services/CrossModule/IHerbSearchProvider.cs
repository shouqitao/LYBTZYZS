using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Services.CrossModule;

/// <summary>
/// 药材搜索提供者 (D5-3)
/// 供 MedicalCase 模块加载药材列表，解耦对 LYBT.Desktop.Herbs 的编译期依赖
/// </summary>
public interface IHerbSearchProvider
{
    /// <summary>搜索药材列表 (keyword 为空时返回全部启用药材)</summary>
    Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword);
}
