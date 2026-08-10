using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量启用药材命令。
/// </summary>
public record BatchEnableHerbsCommand(List<Guid> Ids) : IRequest<Result<BatchOperationResultDto>>, IBatchIdsCommand;
