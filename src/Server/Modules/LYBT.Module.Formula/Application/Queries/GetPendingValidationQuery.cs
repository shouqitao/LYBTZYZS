using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Formulas.Application.Queries;

/// <summary>
/// 获取待验证验方列表查询。
/// </summary>
public record GetPendingValidationQuery() : IRequest<Result<List<FormulaDetailDto>>>;
