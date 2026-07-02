using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

public record ChangePasswordCommand(
    Guid Id,
    string OldPassword,
    string NewPassword,
    Guid CurrentUserId
) : IRequest<Result>;


