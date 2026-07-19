using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 创建药材命令。
/// </summary>
public record CreateHerbCommand(
    HerbInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<HerbDetailDto>>;


