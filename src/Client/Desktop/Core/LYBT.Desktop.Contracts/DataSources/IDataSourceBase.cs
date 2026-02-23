namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// DataSource 基础接口 - 定义通用 CRUD 操作
/// </summary>
/// <typeparam name="TDetail">详情 DTO 类型 (查询返回)</typeparam>
/// <typeparam name="TInput">输入 DTO 类型 (创建/更新)</typeparam>
public interface IDataSourceBase<TDetail, TInput>
    where TDetail : class
    where TInput : class
{
    /// <summary>
    /// 根据 ID 获取详情
    /// </summary>
    Task<TDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 分页获取列表
    /// </summary>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>详情列表和总数</returns>
    Task<(List<TDetail> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default);

    /// <summary>
    /// 创建实体
    /// </summary>
    Task<TDetail> CreateAsync(TInput input, CancellationToken ct = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<TDetail> UpdateAsync(TInput input, CancellationToken ct = default);

    /// <summary>
    /// 删除实体（软删除）
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
