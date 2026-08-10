using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量禁用药方命令。
/// </summary>
public record BatchDisableFormulasCommand(List<Guid> Ids) : IRequest<Result<BatchOperationResultDto>>, IBatchIdsCommand;
