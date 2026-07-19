using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 根据ID获取用户详情查询。
/// </summary>
public record GetUserQuery(Guid Id) : IRequest<Result<UserDetailDto>>;


