using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 根据ID获取用户详情查询处理器。
/// </summary>
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDetailDto>> Handle(
        GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, "用户不存在");

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}


