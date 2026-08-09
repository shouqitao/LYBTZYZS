using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 泛型 CRUD Service 接口 — 提供标准的增删改查、分页、搜索、状态切换契约。
/// </summary>
public interface ICrudService<TListDto, TDetailDto, TInputDto>
    where TListDto : class
    where TDetailDto : class
    where TInputDto : class
{
    Task<CommandResult<TDetailDto>> CreateAsync(TInputDto input, CancellationToken ct = default);
    Task<CommandResult<TDetailDto>> UpdateAsync(TInputDto input, CancellationToken ct = default);
    Task<CommandResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<CommandResult<TDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CommandResult<PagedResult<TListDto>>> GetPagedAsync(int page, int pageSize, string? keyword = null, CancellationToken ct = default);
    Task<CommandResult<List<TListDto>>> SearchAsync(string keyword, CancellationToken ct = default);
    Task<CommandResult<TDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default);
}
