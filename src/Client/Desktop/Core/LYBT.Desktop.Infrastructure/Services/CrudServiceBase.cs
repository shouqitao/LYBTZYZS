using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 泛型 CRUD Service 基类 — 封装 try-log-repository-log-return 模式。
/// 子类只需重写需要自定义的方法，其余使用默认实现。
/// </summary>
public abstract class CrudServiceBase<TListDto, TDetailDto, TInputDto>
    : ICrudService<TListDto, TDetailDto, TInputDto>
    where TListDto : class
    where TDetailDto : class
    where TInputDto : class
{
    protected readonly ILogger Logger;
    protected readonly string EntityName;

    protected CrudServiceBase(ILogger logger, string entityName)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EntityName = entityName;
    }

    public virtual async Task<CommandResult<TDetailDto>> CreateAsync(TInputDto input, CancellationToken ct = default)
    {
        return await ExecuteAsync<TDetailDto>($"{EntityName}.Create", async () =>
        {
            var result = await CreateCoreAsync(input, ct);
            return CommandResult<TDetailDto>.Succeeded(result);
        });
    }

    public virtual async Task<CommandResult<TDetailDto>> UpdateAsync(TInputDto input, CancellationToken ct = default)
    {
        return await ExecuteAsync<TDetailDto>($"{EntityName}.Update", async () =>
        {
            var result = await UpdateCoreAsync(input, ct);
            return CommandResult<TDetailDto>.Succeeded(result);
        });
    }

    public virtual async Task<CommandResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync<bool>($"{EntityName}.Delete", async () =>
        {
            await DeleteCoreAsync(id, ct);
            return CommandResult<bool>.Succeeded(true);
        });
    }

    public virtual async Task<CommandResult<TDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync<TDetailDto>($"{EntityName}.GetById", async () =>
        {
            var entity = await GetByIdCoreAsync(id, ct);
            if (entity == null)
                return CommandResult<TDetailDto>.NotFound($"{EntityName}不存在");
            return CommandResult<TDetailDto>.Succeeded(entity);
        });
    }

    public virtual async Task<CommandResult<PagedResult<TListDto>>> GetPagedAsync(
        int page, int pageSize, string? keyword = null, CancellationToken ct = default)
    {
        return await ExecuteAsync<PagedResult<TListDto>>($"{EntityName}.GetPaged", async () =>
        {
            var result = await GetPagedCoreAsync(page, pageSize, keyword, ct);
            return CommandResult<PagedResult<TListDto>>.Succeeded(result);
        });
    }

    public virtual async Task<CommandResult<List<TListDto>>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<TListDto>>($"{EntityName}.Search", async () =>
        {
            var result = await SearchCoreAsync(keyword, ct);
            return CommandResult<List<TListDto>>.Succeeded(result);
        });
    }

    [Obsolete("Use SetStatusAsync")]
    public virtual async Task<CommandResult<TDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync<TDetailDto>($"{EntityName}.ToggleStatus", async () =>
        {
            var entity = await ToggleStatusCoreAsync(id, ct);
            if (entity == null)
                return CommandResult<TDetailDto>.NotFound($"{EntityName}不存在");
            return CommandResult<TDetailDto>.Succeeded(entity);
        });
    }

    public virtual Task<CommandResult<TDetailDto>> SetStatusAsync(Guid id, CommonStatus status, CancellationToken ct = default)
        => ToggleStatusAsync(id, ct);

    [Obsolete("Use BatchSetStatusAsync")]
    public virtual Task<CommandResult<BatchOperationResultDto>> BatchEnableAsync(List<Guid> ids, CancellationToken ct = default)
        => BatchSetStatusAsync(ids, CommonStatus.Enabled, ct);

    [Obsolete("Use BatchSetStatusAsync")]
    public virtual Task<CommandResult<BatchOperationResultDto>> BatchDisableAsync(List<Guid> ids, CancellationToken ct = default)
        => BatchSetStatusAsync(ids, CommonStatus.Disabled, ct);

    public virtual Task<CommandResult<BatchOperationResultDto>> BatchSetStatusAsync(List<Guid> ids, CommonStatus status, CancellationToken ct = default)
        => Task.FromResult(CommandResult<BatchOperationResultDto>.Failed("BatchSetStatus not implemented for " + EntityName));

    public virtual async Task<CommandResult<List<TListDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<TListDto>>($"{EntityName}.GetAll", async () =>
        {
            var paged = await GetPagedCoreAsync(1, 10000, null, ct);
            return CommandResult<List<TListDto>>.Succeeded(paged.Items.ToList());
        });
    }

    #region Core 抽象方法 — 子类必须实现

    protected abstract Task<TDetailDto> CreateCoreAsync(TInputDto input, CancellationToken ct);
    protected abstract Task<TDetailDto> UpdateCoreAsync(TInputDto input, CancellationToken ct);
    protected abstract Task DeleteCoreAsync(Guid id, CancellationToken ct);
    protected abstract Task<TDetailDto?> GetByIdCoreAsync(Guid id, CancellationToken ct);
    protected abstract Task<PagedResult<TListDto>> GetPagedCoreAsync(int page, int pageSize, string? keyword, CancellationToken ct);
    protected abstract Task<List<TListDto>> SearchCoreAsync(string keyword, CancellationToken ct);
    protected abstract Task<TDetailDto?> ToggleStatusCoreAsync(Guid id, CancellationToken ct);

    #endregion

    #region 辅助方法

    protected async Task<CommandResult<T>> ExecuteAsync<T>(string operation, Func<Task<CommandResult<T>>> action)
    {
        try
        {
            Logger.LogInformation("[SVC] {Operation} started", operation);
            var result = await action();
            Logger.LogInformation("[SVC] {Operation} completed", operation);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SVC] {Operation} failed", operation);
            return CommandResult<T>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage(operation, ex));
        }
    }

    #endregion
}
