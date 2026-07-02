using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 更新验方命令。
/// </summary>
public record UpdateFormulaCommand(
    Guid Id,
    FormulaInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<FormulaDetailDto>>;


