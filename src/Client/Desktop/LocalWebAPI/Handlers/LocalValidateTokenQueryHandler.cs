using LYBT.Entities.Users;
using LYBT.LocalWebAPI.Commands;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LYBT.LocalWebAPI.Handlers;

public class LocalValidateTokenQueryHandler : IRequestHandler<LocalValidateTokenQuery, ApiResponse<ValidateTokenResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public LocalValidateTokenQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApiResponse<ValidateTokenResponse>> Handle(LocalValidateTokenQuery query, CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return ApiResponse<ValidateTokenResponse>.CreateSuccess(
                new ValidateTokenResponse { IsValid = false, ErrorMessage = "Token 无效" }, "Token 无效");

        var user = await _userManager.FindByIdAsync(query.UserId.ToString());
        if (user == null)
            return ApiResponse<ValidateTokenResponse>.CreateSuccess(
                new ValidateTokenResponse { IsValid = false, ErrorMessage = "用户不存在" }, "用户不存在");

        var roles = await _userManager.GetRolesAsync(user);
        var role = LocalAuthHelpers.ParseUserRole(roles);

        return ApiResponse<ValidateTokenResponse>.CreateSuccess(new ValidateTokenResponse
        {
            IsValid = true,
            UserId = (int)role,
            Username = user.UserName,
            Role = role.ToString(),
        }, "Token 验证成功");
    }


}
