using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 恢复已删除药材命令。
/// </summary>
public record RestoreHerbCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result<HerbDetailDto>>;
