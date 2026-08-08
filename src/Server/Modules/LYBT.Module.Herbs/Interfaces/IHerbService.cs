using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces;

/// <summary>
/// 药材服务接口 — 封装简单 CRUD 操作，供 Controller 直接注入。
/// </summary>
public interface IHerbService
{
    Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);
    Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
}
