using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 更新药材命令。
/// </summary>
public record UpdateHerbCommand(
    Guid Id,
    HerbInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<HerbDetailDto>>;


