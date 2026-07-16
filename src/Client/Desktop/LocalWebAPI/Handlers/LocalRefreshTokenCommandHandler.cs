using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Entities.Users;
using LYBT.LocalWebAPI.Auth;
using LYBT.LocalWebAPI.Commands;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LYBT.LocalWebAPI.Handlers;

public class LocalRefreshTokenCommandHandler : IRequestHandler<LocalRefreshTokenCommand, ApiResponse<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LocalRefreshTokenCommandHandler> _logger;

    public LocalRefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<LocalRefreshTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LocalRefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ApiResponse<LoginResponse>.CreateFail("令牌不能为空",
                new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(request.RefreshToken);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<LoginResponse>.CreateFail("无效的令牌",
                    new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<LoginResponse>.CreateFail("用户不存在",
                    new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });

            var roles = await _userManager.GetRolesAsync(user);
            var token = LocalJwtConfig.GenerateToken(user, roles);
            var role = LocalAuthHelpers.ParseUserRole(roles);

            _logger.LogInformation("[AUTH] Local token refresh - UserName={UserName}", user.UserName);

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
            }, "Token刷新成功");
        }
        catch (Exception)
        {
            return ApiResponse<LoginResponse>.CreateFail("无效的令牌",
                new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });
        }
    }


}
