using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 删除验方命令（软删除）。
/// </summary>
public record DeleteFormulaCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result>;


