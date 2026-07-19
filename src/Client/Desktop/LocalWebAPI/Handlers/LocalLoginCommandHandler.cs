using System.IdentityModel.Tokens.Jwt;
using LYBT.Entities.Users;
using LYBT.LocalWebAPI.Auth;
using LYBT.LocalWebAPI.Commands;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LYBT.LocalWebAPI.Handlers;

public class LocalLoginCommandHandler : IRequestHandler<LocalLoginCommand, ApiResponse<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LocalLoginCommandHandler> _logger;

    public LocalLoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<LocalLoginCommandHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LocalLoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (request is null)
            return ApiResponse<LoginResponse>.CreateFail("请求不能为空");

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return ApiResponse<LoginResponse>.CreateFail("用户名或密码错误",
                new { code = ErrorCode.AuthInvalidCredentials.ToFormattedString() });

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            return ApiResponse<LoginResponse>.CreateFail("用户名或密码错误",
                new { code = ErrorCode.AuthInvalidCredentials.ToFormattedString() });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
            return ApiResponse<LoginResponse>.CreateFail("用户名或密码错误",
                new { code = ErrorCode.AuthInvalidCredentials.ToFormattedString() });

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var role = LocalAuthHelpers.ParseUserRole(roles);

        var token = LocalJwtConfig.GenerateToken(user, roles);

        _logger.LogInformation("[AUTH] Local login succeeded - UserName={UserName} Role={Role}",
            request.UserName, role);

        return ApiResponse<LoginResponse>.CreateSuccess(new LoginResponse
        {
            Token = token,
            User = new UserDetailDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                RealName = user.RealName,
                Role = role,
                Status = CommonStatus.Enabled,
                PhoneNumber = user.PhoneNumber,
            },
            ExpiresAt = DateTime.UtcNow.AddDays(LocalJwtConfig.ExpirationDays)
        }, "登录成功");
    }


}
