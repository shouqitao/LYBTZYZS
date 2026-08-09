using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量删除药材命令。
/// </summary>
public record BatchDeleteHerbsCommand(
    List<Guid> Ids,
    Guid CurrentUserId
) : IRequest<Result<BatchOperationResultDto>>;
