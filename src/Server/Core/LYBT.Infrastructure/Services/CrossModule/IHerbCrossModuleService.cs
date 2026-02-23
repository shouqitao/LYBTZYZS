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
}
