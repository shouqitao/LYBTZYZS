using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 药材域跨模块服务 (ISP: D5-1)
/// 供 Sync 模块使用
/// </summary>
public interface IHerbCrossModuleService
{
    /// <summary>获取药材基本信息</summary>
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId);

    /// <summary>按名称或拼音查找药材</summary>
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin);

    /// <summary>检查药材引用关系 (处方引用数)</summary>
    Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId);

    /// <summary>批量获取药材单价（用于处方项UnitPrice自动填充）</summary>
    Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds);

    /// <summary>从给定的药材ID中筛选出已禁用的药材ID（AD-02: 禁用药材不可加入处方）</summary>
    Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds);
}
