using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 批量禁用药材命令。
/// </summary>
public record BatchDisableHerbsCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
