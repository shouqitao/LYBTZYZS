using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LYBT.Entities.Users;
using LYBT.LocalWebAPI.Auth;
using LYBT.LocalWebAPI.Commands;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.LocalWebAPI.Handlers;

public class LocalAutoLoginCommandHandler : IRequestHandler<LocalAutoLoginCommand, ApiResponse<LoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LocalJwtOptions _jwtOptions;
    private readonly ILogger<LocalAutoLoginCommandHandler> _logger;

    public LocalAutoLoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IOptions<LocalJwtOptions> jwtOptions,
        ILogger<LocalAutoLoginCommandHandler> logger)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LocalAutoLoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.AutoLoginToken))
            return ApiResponse<LoginResponse>.CreateFail("自动登录令牌不能为空",
                new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(request.AutoLoginToken, validationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken)
                return ApiResponse<LoginResponse>.CreateFail("无效的自动登录令牌格式",
                    new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<LoginResponse>.CreateFail("自动登录令牌缺少用户信息",
                    new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<LoginResponse>.CreateFail("用户不存在",
                    new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });

            var roles = await _userManager.GetRolesAsync(user);
            var token = LocalJwtConfig.GenerateToken(user, roles);
            var role = LocalAuthHelpers.ParseUserRole(roles);

            _logger.LogInformation("[AUTH] Local auto-login - UserName={UserName} Role={Role}",
                user.UserName, role);

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
            }, "自动登录成功");
        }
        catch (Exception)
        {
            return ApiResponse<LoginResponse>.CreateFail("自动登录令牌无效",
                new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() });
        }
    }


}
