using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using LYBT.Module.Identity.Application.Mappers;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 创建用户命令处理器。
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDetailDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserCommandHandler(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDetailDto>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (!request.IsAdmin)
            return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "无权创建用户");

        if (string.IsNullOrWhiteSpace(dto.RealName))
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, "真实姓名不能为空");
        if (string.IsNullOrWhiteSpace(dto.UserName))
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, "用户名不能为空");

        if (await _userManager.FindByNameAsync(dto.UserName!) != null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNameExists, ErrorMessages.Get(ErrorCode.UserNameExists));

        // T4 P1#10: 保留用户名双保险（绕过 FluentValidation 的直接调用兜底）
        if (UserReservedNameHelper.IsReserved(dto.UserName))
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, "该用户名已保留，不可使用");

        var user = ApplicationUser.Create(
            dto.UserName!,
            dto.RealName!,
            dto.Role ?? Shared.Models.Enums.UserRole.Doctor,
            dto.PhoneNumber,
            dto.Email,
            dto.Remark,
            request.CurrentUserId,
            dto.RegistrationFee);

        var password = dto.Password ?? Guid.NewGuid().ToString("N")[..12];
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, $"创建用户失败: {errors}");
        }

        return Result<UserDetailDto>.Success(IdentityMapper.ToDetailDto(user));
    }
}
