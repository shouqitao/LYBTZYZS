using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formulas.Interfaces;

/// <summary>
/// 验方服务接口 — 封装简单 CRUD 操作，供 Controller 直接注入。
/// </summary>
public interface IFormulaService
{
    Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);
    Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
}
