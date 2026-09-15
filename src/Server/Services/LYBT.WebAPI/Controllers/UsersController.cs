using Asp.Versioning;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Web;
using LYBT.Module.Identity.Application.Commands;
using LYBT.Module.Identity.Application.Queries;
using LYBT.Module.Identity.Controllers;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 用户管理 API（N-01 2026-08-14: IdentityController → UsersController——命名对齐类职责；A-31-C3a 合并 Auth+Users）。
    /// 路由保持：/api/v1/auth/*（认证）+ /api/v1/users/*（用户管理）。
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/users")]
    [Authorize]
    public class UsersController : BaseUsersController
    {
        private readonly ISender _sender;

        public UsersController(
            ISender sender,
            ILogger<UsersController> logger,
            IUserCrossModuleService userService)
            : base(sender, logger, userService)
        {
            _sender = sender;
        }

        #region 认证端点（原 AuthController，路由 /api/v1/auth/*）

        /// <summary>
        /// 用户登录（匿名——返回访问令牌 + 刷新令牌；Login 限流策略）
        /// </summary>
        /// <param name="request">登录请求（用户名 + 密码）</param>
        [HttpPost("/api/v{version:apiVersion}/auth/login")]
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
            return StatusCode(httpStatus, response);
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/auth/logout")]
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

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/auth/refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("Login")]
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
            return StatusCode(httpStatus, response);
        }

        /// <summary>
        /// 自动登录（免密登录）
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/auth/auto-login")]
        [AllowAnonymous]
        [EnableRateLimiting("Login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
        public async Task<IActionResult> AutoLoginAsync([FromBody] AutoLoginRequest request)
        {
            if (ValidateModel() is { } modelError) return modelError;

            var result = await _sender.Send(new AutoLoginCommand(
                request.AutoLoginToken,
                Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()));
            return HandleResult(result, "自动登录成功", useAuthMapping: true);
        }

        /// <summary>
        /// 校验 Authorization 头中的访问令牌（有效时返回用户标识与角色）
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/auth/validate")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ValidateTokenFromHeaderAsync()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("缺少 Authorization 头", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Authorization 头格式无效", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(ApiResponse<object>.CreateFail("Authorization 头中缺少 Token", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
            }

            var result = await _sender.Send(new ValidateTokenQuery(token));
            if (result.IsSuccess && result.Value?.IsValid == true)
            {
                object response = new
                {
                    valid = true,
                    sub = new
                    {
                        result.Value.UserId,
                        result.Value.UserName,
                        result.Value.Role
                    },
                    message = "Token 有效"
                };
                return Success(response, "Token验证成功");
            }

            return Unauthorized(ApiResponse<object>.CreateFail("Token 无效", new { code = ErrorCode.AuthTokenInvalid.ToFormattedString() }));
        }

        #endregion
    }
}
