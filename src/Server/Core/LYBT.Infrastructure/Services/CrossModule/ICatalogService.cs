using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 药材目录域跨模块服务（A-31-C3b 由 IHerbCrossModuleService 改名，ISP D5-1）
/// 供 MedicalCase 等模块同步查询药材目录（P07 跨模块通道）
/// </summary>
public interface ICatalogService
{
    /// <summary>获取药材基本信息</summary>
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default);

    /// <summary>批量获取药材单价（用于处方项UnitPrice自动填充）</summary>
    Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);

    /// <summary>从给定的药材ID中筛选出已禁用的药材ID（AD-02: 禁用药材不可加入处方）</summary>
    Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);

    /// <summary>获取所有有效药材（用于批量导入时的名称/拼音匹配）</summary>
    Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default);
}
