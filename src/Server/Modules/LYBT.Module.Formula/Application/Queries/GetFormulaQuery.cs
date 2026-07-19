using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Queries;

/// <summary>
/// 根据ID获取验方详情查询。
/// </summary>
public record GetFormulaQuery(Guid Id) : IRequest<Result<FormulaDetailDto>>;


