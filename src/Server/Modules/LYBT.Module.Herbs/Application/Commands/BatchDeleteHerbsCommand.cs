using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 批量删除药材命令（软删除）。
/// </summary>
public record BatchDeleteHerbsCommand(
    List<Guid> Ids,
    Guid CurrentUserId
) : IRequest<Result<BatchOperationResultDto>>;


