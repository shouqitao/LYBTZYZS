using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Queries;

public record GetCurrentUserQuery(
    Guid UserId
) : IRequest<Result<UserDetailDto>>;


