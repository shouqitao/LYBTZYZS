using MediatR;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 获取用户分页列表查询。
/// </summary>
public record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    UserRole? Role = null,
    CommonStatus? Status = null
) : IRequest<Result<PagedResult<UserListDto>>>;


