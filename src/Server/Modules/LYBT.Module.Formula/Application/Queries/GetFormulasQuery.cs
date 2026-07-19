using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Queries;

/// <summary>
/// 获取验方分页列表查询。
/// </summary>
public record GetFormulasQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    string? Category = null
) : IRequest<Result<PagedResult<FormulaListDto>>>;


