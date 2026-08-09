using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using MediatR;

namespace LYBT.Module.Catalog.Application.Queries;

/// <summary>
/// 获取待验证验方列表查询。
/// </summary>
public record GetPendingValidationQuery() : IRequest<Result<List<FormulaDetailDto>>>;
