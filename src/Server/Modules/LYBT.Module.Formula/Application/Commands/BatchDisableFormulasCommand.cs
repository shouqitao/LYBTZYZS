using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 批量禁用验方命令。
/// </summary>
public record BatchDisableFormulasCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
