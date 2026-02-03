namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// DataSource 基础接口 - 定义通用 CRUD 操作
/// OpenSpec: implement-local-mode
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IDataSourceBase<TEntity> where TEntity : class
{
    /// <summary>
    /// 根据 ID 获取实体
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 分页获取实体列表
    /// </summary>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>实体列表和总数</returns>
    Task<(List<TEntity> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default);

    /// <summary>
    /// 创建实体
    /// </summary>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// 删除实体（软删除）
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
