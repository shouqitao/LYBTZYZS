using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

public record ChangeProfileCommand(
    Guid Id,
    ChangeProfileDto Dto,
    Guid CurrentUserId
) : IRequest<Result<UserDetailDto>>;


