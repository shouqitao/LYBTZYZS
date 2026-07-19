using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 切换药材状态命令（启用/禁用）。
/// </summary>
public record ToggleHerbStatusCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result<HerbDetailDto>>;


