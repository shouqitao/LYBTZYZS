using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 切换验方状态（启用/禁用）命令。
/// </summary>
public record ToggleFormulaStatusCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result<FormulaDetailDto>>;
