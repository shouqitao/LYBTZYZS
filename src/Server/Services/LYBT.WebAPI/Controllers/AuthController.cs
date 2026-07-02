using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Auth.Application.Commands;
using LYBT.Module.Auth.Application.Queries;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AuthController : BaseApiController
    {
        private readonly ISender _sender;
        private readonly IJwtService _jwtService;

        public AuthController(
            ISender sender,
            IJwtService jwtService,
            ILogger<AuthController> logger)
            : base(logger)
        {
            _sender = sender;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("Login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            if (ValidateModel() is { } modelError) return modelError;

            if (request == null)
                return ValidationFail("登录请求不能为空");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return ValidationFail("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.Password))
                return ValidationFail("密码不能为空");

            var result = await _sender.Send(new LoginCommand(request));
            if (result.IsSuccess)
                return Success(result.Value!, "登录成功");

            var httpStatus = result.ErrorCode.ToHttpStatusCode();
            var response = ApiResponse<LoginResponse>.CreateFail(result.Error ?? "登录失败");
            response.RequestId = GetRequestId();
            return httpStatus switch
            {
                401 => Unauthorized(response),
                _ => StatusCode(httpStatus, response)
            };
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request)
        {
            var result = await _sender.Send(new LogoutCommand(request));
            if (!result.IsSuccess)
            {
                var response = ApiResponse.CreateFail(result.Error ?? "登出失败");
                response.RequestId = GetRequestId();
                return StatusCode(result.ErrorCode.ToHttpStatusCode(), response);
            }

            _logger.LogInformation("[AUTH] Logout - UserName={UserName}", request?.UserName ?? "(unknown)");
            return Success("登出成功");
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            if (ValidateModel() is { } modelError) return modelError;

            var result = await _sender.Send(new RefreshTokenCommand(request.RefreshToken));
            if (result.IsSuccess)
                return Success(result.Value!, "Token刷新成功");

            var httpStatus = result.ErrorCode.ToHttpStatusCode();
            var response = ApiResponse<LoginResponse>.CreateFail(result.Error ?? "Token刷新失败");
            response.RequestId = GetRequestId();
            return httpStatus switch
            {
                401 => Unauthorized(response),
                _ => StatusCode(httpStatus, response)
            };
        }

        [HttpPost("auto-login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public IActionResult AutoLoginAsync([FromBody] AutoLoginRequest request)
        {
            if (ValidateModel() is { } modelError) return modelError;

            var result = _jwtService.ValidateAutoLoginToken(request.AutoLoginToken);
            return HandleResult(result, "自动登录成功", useAuthMapping: true);
        }

        [HttpGet("validate")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ValidateTokenFromHeaderAsync()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Missing Authorization header", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Invalid Authorization header format", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Missing token in Authorization header", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
            }

            var result = await _sender.Send(new ValidateTokenQuery(token));
            if (result.IsSuccess && result.Value)
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal != null)
                {
                    object response = new
                    {
                        valid = true,
                        sub = new
                        {
                            UserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                            UserName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                            Role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                        },
                        message = "Token is valid"
                    };
                    return Success(response, "Token验证成功");
                }
            }

            return Unauthorized(ApiResponse<object>.CreateFail("Token is invalid", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
        }

        [HttpGet]
        public IActionResult Get()
        {
            return BusinessFail("方法不允许");
        }
    }
}


