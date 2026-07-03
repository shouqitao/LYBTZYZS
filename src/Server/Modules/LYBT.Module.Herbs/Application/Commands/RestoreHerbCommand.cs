using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 恢复已删除药材命令。
/// </summary>
public record RestoreHerbCommand(
    Guid HerbId,
    Guid OperatorId
) : IRequest<Result<HerbDetailDto>>;
