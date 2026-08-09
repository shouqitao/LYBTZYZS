using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 修改个人资料命令。
/// </summary>
public record ChangeProfileCommand(
    Guid Id,
    ChangeProfileDto Dto,
    Guid CurrentUserId
) : IRequest<Result<UserDetailDto>>;
