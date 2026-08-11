using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using MediatR;

namespace LYBT.Module.Catalog.Application.Queries;

/// <summary>
/// 获取待验证验方列表查询（P3: 支持分页——US-FORM-007）
/// </summary>
public record GetPendingValidationQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<FormulaDetailDto>>>;
