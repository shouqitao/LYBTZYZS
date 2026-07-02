using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.SharedKernel.Events;
using LYBT.Entities.Users;
using LYBT.Module.Users.Domain.Events;
using LYBT.Module.Users.Application.Mappers;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 创建用户命令处理器。
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDetailDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        IDomainEventDispatcher eventDispatcher)
    {
        _userManager = userManager;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result<UserDetailDto>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (!request.IsAdmin)
            return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "无权创建用户");

        if (await _userManager.FindByNameAsync(dto.UserName!) != null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNameExists, "用户名已存在");

        var user = ApplicationUser.Create(
            dto.UserName!,
            dto.RealName!,
            dto.Role ?? Shared.Models.Enums.UserRole.Doctor,
            dto.PhoneNumber,
            dto.Email,
            dto.Remark,
            request.CurrentUserId);

        // 使用Identity的密码哈希
        var password = dto.Password ?? "User@123456";
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, $"创建用户失败: {errors}");
        }

        await _eventDispatcher.DispatchAsync(new[]
        {
            new UserCreatedEvent(user.Id, user.UserName, user.RealName, user.Role, request.CurrentUserId)
        }, cancellationToken);

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}


