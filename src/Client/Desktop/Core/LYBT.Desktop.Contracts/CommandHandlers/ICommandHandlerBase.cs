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

// OpenSpec: cleanup-patient-dead-code - 已删除重复的PagedResult<T>和未使用的IPagedCommandHandler
// PagedResult<T>统一使用LYBT.Shared.Models.Contracts.Common.PagedResult<T>
