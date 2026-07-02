using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 验证验方药材命令 - 手动绑定药材到系统药材库。
/// </summary>
public record ValidateFormulaHerbCommand(
    Guid FormulaId,
    Guid HerbItemId,
    Guid SelectedHerbId
) : IRequest<Result>;
