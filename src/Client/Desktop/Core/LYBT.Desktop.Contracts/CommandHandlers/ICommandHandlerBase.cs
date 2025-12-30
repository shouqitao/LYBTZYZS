namespace LYBT.Desktop.Contracts.CommandHandlers;

/// <summary>
/// CommandHandler基础接口模板
/// OpenSpec: unify-desktop-architecture (Phase 1.4)
/// 所有模块的CommandHandler应实现此接口，确保CRUD操作规范一致
/// </summary>
/// <typeparam name="TListDto">列表DTO类型</typeparam>
/// <typeparam name="TDetailDto">详情DTO类型</typeparam>
/// <typeparam name="TInputDto">输入DTO类型</typeparam>
public interface ICommandHandlerBase<TListDto, TDetailDto, TInputDto>
    where TListDto : class
    where TDetailDto : class
    where TInputDto : class
{
    /// <summary>
    /// 获取列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>列表数据</returns>
    Task<CommandResult<List<TListDto>>> GetListAsync(QueryParams? query = null);

    /// <summary>
    /// 获取详情
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>详情数据</returns>
    Task<CommandResult<TDetailDto>> GetDetailAsync(Guid id);

    /// <summary>
    /// 保存（创建或更新）
    /// </summary>
    /// <param name="input">输入数据</param>
    /// <returns>保存后的详情数据</returns>
    Task<CommandResult<TDetailDto>> SaveAsync(TInputDto input);

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>删除结果</returns>
    Task<CommandResult<bool>> DeleteAsync(Guid id);
}

/// <summary>
/// 只读CommandHandler接口
/// 用于不需要写操作的场景
/// </summary>
/// <typeparam name="TListDto">列表DTO类型</typeparam>
/// <typeparam name="TDetailDto">详情DTO类型</typeparam>
public interface IReadOnlyCommandHandler<TListDto, TDetailDto>
    where TListDto : class
    where TDetailDto : class
{
    /// <summary>
    /// 获取列表
    /// </summary>
    Task<CommandResult<List<TListDto>>> GetListAsync(QueryParams? query = null);

    /// <summary>
    /// 获取详情
    /// </summary>
    Task<CommandResult<TDetailDto>> GetDetailAsync(Guid id);
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public record PagedResult<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public required List<T> Items { get; init; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// 支持分页的CommandHandler接口
/// </summary>
/// <typeparam name="TListDto">列表DTO类型</typeparam>
/// <typeparam name="TDetailDto">详情DTO类型</typeparam>
/// <typeparam name="TInputDto">输入DTO类型</typeparam>
public interface IPagedCommandHandler<TListDto, TDetailDto, TInputDto> : ICommandHandlerBase<TListDto, TDetailDto, TInputDto>
    where TListDto : class
    where TDetailDto : class
    where TInputDto : class
{
    /// <summary>
    /// 获取分页列表
    /// </summary>
    Task<CommandResult<PagedResult<TListDto>>> GetPagedListAsync(QueryParams? query = null);
}
