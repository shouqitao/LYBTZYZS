using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 创建验方命令。
/// </summary>
public record CreateFormulaCommand(
    FormulaInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<FormulaDetailDto>>;


