using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量删除验方命令。
/// </summary>
public record BatchDeleteFormulasCommand(
    List<Guid> Ids,
    Guid OperatorId
) : IRequest<Result<BatchOperationResultDto>>;
