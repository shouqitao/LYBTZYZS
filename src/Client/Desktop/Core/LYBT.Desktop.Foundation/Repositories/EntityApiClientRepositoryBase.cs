using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Repositories;

/// <summary>
/// 实体 CRUD API 客户端仓储基类 — 通过泛型 API 段实现标准 CRUD（GetPaged/GetById/Create/Update/Delete），
/// 统一日志与异常处理：失败抛 <see cref="InvalidOperationException"/>（message 为空时用默认文案）；
/// GetPaged 的 Data==null 返回空分页。
/// 仅适用于标准 CRUD 形状一致的实体；Registration/MedicalCase 继续使用
/// <see cref="ApiClientRepositoryBase{TListDto,TDetailDto}"/>。
/// </summary>
/// <typeparam name="TListDto">列表 DTO 类型</typeparam>
/// <typeparam name="TDetailDto">详情 DTO 类型</typeparam>
/// <typeparam name="TInputDto">输入 DTO 类型（需实现 <see cref="IEntityInputDto"/> 以提取更新用 ID）</typeparam>
public abstract class EntityApiClientRepositoryBase<TListDto, TDetailDto, TInputDto>
    : ApiClientRepositoryBase<TListDto, TDetailDto>
    where TInputDto : IEntityInputDto
{
    /// <summary>
    /// 泛型 API 段 — 标准 CRUD 的调用入口。
    /// </summary>
    protected IEntityApiSegment<TListDto, TDetailDto, TInputDto> Api { get; }

    /// <summary>
    /// 初始化 <see cref="EntityApiClientRepositoryBase{TListDto,TDetailDto,TInputDto}"/> 类的新实例。
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="api">泛型 API 段</param>
    protected EntityApiClientRepositoryBase(
        ILogger logger,
        IEntityApiSegment<TListDto, TDetailDto, TInputDto> api)
        : base(logger)
    {
        Api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// 分页获取实体列表；服务端判定失败（Success=false）抛 <see cref="InvalidOperationException"/>，
    /// Data==null 时返回空分页（合法的「本页无数据」）。
    /// </summary>
    public virtual async Task<PagedResult<TListDto>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var response = await Api.GetPagedAsync(page, pageSize, keyword, category, ct);
            if (!response.Success)
                throw new InvalidOperationException(response.Message ?? "查询失败");

            if (response.Data == null)
                return new PagedResult<TListDto> { Items = [], TotalCount = 0, CurrentPage = page, PageSize = pageSize };

            return new PagedResult<TListDto>
            {
                Items = response.Data.Items,
                TotalCount = response.Data.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }, nameof(GetPagedAsync), "[REPO] {0}.{1} - Page={2} PageSize={3} Keyword={4} Category={5}",
           [LogPrefix, nameof(GetPagedAsync), page, pageSize, keyword, category]);
    }

    /// <summary>
    /// 按 ID 获取实体详情；服务端判定失败（Success=false）抛 <see cref="InvalidOperationException"/>，
    /// Data==null 表示实体不存在（返回 null）。
    /// </summary>
    public virtual Task<TDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return ExecuteAsync(async () =>
        {
            var response = await Api.GetByIdAsync(id, ct);
            if (!response.Success)
                throw new InvalidOperationException(response.Message ?? "查询失败");

            return response.Data;
        }, nameof(GetByIdAsync));
    }

    /// <summary>
    /// 创建实体；失败抛 <see cref="InvalidOperationException"/>。
    /// </summary>
    public virtual Task<TDetailDto> CreateAsync(TInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return ExecuteAsync(async () =>
        {
            var response = await Api.CreateAsync(dto, ct);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "创建失败");
            return response.Data;
        }, nameof(CreateAsync), LogLevel.Information);
    }

    /// <summary>
    /// 更新实体（DTO 内嵌 ID）；失败抛 <see cref="InvalidOperationException"/>。
    /// </summary>
    public virtual Task<TDetailDto> UpdateAsync(TInputDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id is null || dto.Id == Guid.Empty)
            throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        return ExecuteAsync(async () =>
        {
            var response = await Api.UpdateAsync(dto.Id.Value, dto, ct);
            if (!response.Success || response.Data == null)
                throw new InvalidOperationException(response.Message ?? "更新失败");

            Logger.LogInformation("[REPO] {0}.Update completed - Id={1}", LogPrefix, dto.Id);
            return response.Data;
        }, nameof(UpdateAsync), LogLevel.Information);
    }

    /// <summary>
    /// 删除实体（软删除）；失败抛 <see cref="InvalidOperationException"/>。
    /// </summary>
    public virtual Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return ExecuteAsync(
            async () =>
            {
                var response = await Api.DeleteAsync(id, ct);
                if (!response.Success)
                    throw new InvalidOperationException(response.Message ?? "删除失败");

                Logger.LogInformation("[REPO] {0}.Delete completed - Id={1}", LogPrefix, id);
            },
            nameof(DeleteAsync),
            LogLevel.Information);
    }
}
