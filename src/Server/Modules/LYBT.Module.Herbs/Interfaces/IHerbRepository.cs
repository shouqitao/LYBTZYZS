using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Domain;

namespace LYBT.Module.Herbs.Interfaces;

/// <summary>
/// 药材仓储接口。
/// </summary>
public interface IHerbRepository
{
    /// <summary>
    /// 根据ID获取药材。
    /// </summary>
    Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct);

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

    /// <summary>
    /// 按名称或拼音码查询药材。
    /// </summary>
    Task<Herb?> GetByNameOrPinyinAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// 新增药材。
    /// </summary>
    Task AddAsync(Herb herb, CancellationToken ct);

    /// <summary>
    /// 更新药材。
    /// </summary>
    Task UpdateAsync(Herb herb, CancellationToken ct);

    /// <summary>
    /// 获取所有药材。
    /// </summary>
    Task<List<Herb>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 按分类筛选药材。
    /// </summary>
    Task<List<Herb>> GetByCategoryAsync(string category, CancellationToken ct = default);
}


