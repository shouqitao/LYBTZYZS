using LYBT.Module.Users.Application.Mappers;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Users.Services;

/// <summary>
/// 用户服务实现 — 封装简单 CRUD 操作，替代 trivial MediatR Handler。
/// </summary>
internal class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)
    {
        var result = await _userRepository.GetPagedAsync(page, pageSize, keyword, null, null, ct);
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

    public async Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, "用户不存在");
        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }

    public async Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "无法获取当前用户信息");

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, "用户不存在");

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }

    public async Task<Result<UserDetailDto>> UpdateAsync(Guid id, UserInputDto dto, Guid operatorId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, "用户不存在");

        user.UpdateProfile(
            dto.RealName!,
            dto.PhoneNumber,
            dto.Email,
            dto.Remark,
            operatorId);

        await _userRepository.UpdateAsync(user, ct);
        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }

    public async Task<Result<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto dto, Guid currentUserId, CancellationToken ct)
    {
        if (id != currentUserId)
            return Result<UserDetailDto>.Failure(ErrorCode.Forbidden, "只能修改自己的个人资料");

        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, "用户不存在");

        user.UpdateProfile(
            dto.RealName,
            dto.PhoneNumber,
            dto.Email,
            user.Remark,
            currentUserId);

        await _userRepository.UpdateAsync(user, ct);
        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}
