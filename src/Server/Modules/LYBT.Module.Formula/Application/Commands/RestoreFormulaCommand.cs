using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 恢复已删除验方命令。
/// </summary>
public record RestoreFormulaCommand(
    Guid FormulaId,
    Guid OperatorId
) : IRequest<Result<FormulaDetailDto>>;
