using LYBT.Entities.Formulas;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 验方数据源接口
/// OpenSpec: implement-local-mode
/// </summary>
public interface IFormulaDataSource : IDataSourceBase<Formula>
{
    /// <summary>
    /// 克隆验方（深拷贝，包含药材组成）
    /// </summary>
    Task<Formula?> CloneAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 切换验方状态（启用/禁用）
    /// </summary>
    Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 恢复已删除的验方
    /// </summary>
    Task<Formula?> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 获取验方详情（包含药材组成）
    /// </summary>
    Task<Formula?> GetWithHerbsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 分页获取验方列表（带分类过滤）
    /// </summary>
    Task<(List<Formula> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? category,
        CancellationToken ct = default);
}
