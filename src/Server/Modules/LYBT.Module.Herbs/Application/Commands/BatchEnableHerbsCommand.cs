using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 批量启用药材命令。
/// </summary>
public record BatchEnableHerbsCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
