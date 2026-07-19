using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 获取用户分页列表查询处理器。
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserListDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<UserListDto>>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _userRepository.GetPagedAsync(
            request.Page, request.PageSize, request.Keyword,
            request.Role, request.Status, cancellationToken);

        var dtos = result.Items.Select(UserMapper.ToListDto).ToList();

        var pagedResult = new PagedResult<UserListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };

        return Result<PagedResult<UserListDto>>.Success(pagedResult);
    }
}


