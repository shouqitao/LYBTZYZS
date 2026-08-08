using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Herbs.Interfaces;

/// <summary>
/// 药材仓储接口。
/// </summary>
public interface IHerbRepository : IRepository<Herb>
{
    /// <summary>
    /// 根据ID获取药材（包括已软删除的）。
    /// </summary>
    Task<Herb?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 分页查询药材（支持关键字 + 分类筛选）。
    /// </summary>
    Task<PagedResult<Herb>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, CancellationToken ct);

    /// <summary>
    /// 检查药材名称是否已存在。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// 根据名称精确获取药材。
    /// </summary>
    Task<Herb?> GetByNameAsync(string name, CancellationToken ct = default);
}
