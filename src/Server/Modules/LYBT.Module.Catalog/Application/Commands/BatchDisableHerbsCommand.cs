using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量禁用药材命令。
/// </summary>
public record BatchDisableHerbsCommand(List<Guid> Ids) : IRequest<Result<BatchOperationResultDto>>, IBatchIdsCommand;
