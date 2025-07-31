using Asp.Versioning;
using LYBT.Common.Responses;
using LYBT.Common.Helpers;
using LYBT.Infrastructure.Authentication;
using LYBT.Models.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 认证相关接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize] // 默认需要认证
    public class AuthController : BaseController {
        private readonly IAuthService _authService;
        private readonly IJwtAuthenticationService _jwtService;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthController(
            IAuthService authService,
            IJwtAuthenticationService jwtService,
            SysAdminHandler sysAdminHandler,
            ILogger<AuthController> logger,
            IMemoryCache cache)
            : base(logger, cache) {
            _authService = authService;
            _jwtService = jwtService;
            _sysAdminHandler = sysAdminHandler;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous] // 登录不需要认证
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto dto) {
            try {
                var validationResult = ValidateModel<LoginResponseDto>();
                if (validationResult != null)
                    return validationResult;

                // 获取客户端IP和UserAgent
                dto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                dto.UserAgent = Request.Headers["User-Agent"].ToString();

                var user = await _authService.LoginAsync(dto);
                if (user == null) {
                    return Unauthorized(ApiResponse<LoginResponseDto>.Fail("用户名或密码错误", 401));
                }

                var token = _jwtService.GenerateToken(user.Id.ToString(), user.UserName, new[] { user.Role.ToString() }, dto.RememberMe);
                var response = new LoginResponseDto { Token = token, User = user };

                LogOperation("用户登录成功", new { UserId = user.Id, UserName = user.UserName });

                return Ok(ApiResponse<LoginResponseDto>.Success(response));
            } catch (Exception ex) {
                return HandleException<LoginResponseDto>(ex, "用户登录", new { dto.Username });
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequestDto dto) {
            try {
                var validationResult = ValidateModel<object>();
                if (validationResult != null)
                    return validationResult;

                await _authService.LogoutAsync(dto);

                LogOperation("用户登出", dto);

                return Ok(ApiResponse<object>.Success(new { }, "登出成功"));
            } catch (Exception ex) {
                return HandleException<object>(ex, "用户登出", dto);
            }
        }

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        [HttpPost("changeSysAdminPassword")]
        public async Task<ActionResult<ApiResponse<object>>> ChangeSysAdminPassword([FromBody] ChangeSysAdminPasswordDto dto) {
            try {
                var validationResult = ValidateModel<object>();
                if (validationResult != null)
                    return validationResult;

                var success = await _authService.ChangeSysAdminPasswordAsync(dto);
                if (!success) {
                    return BadRequest(ApiResponse<object>.Fail("修改密码失败，请检查当前密码是否正确", 400));
                }

                LogOperation("管理员密码修改", "密码修改请求");

                return Ok(ApiResponse<object>.Success(new { }, "密码修改成功"));
            } catch (Exception ex) {
                return HandleException<object>(ex, "修改管理员密码", "密码修改请求");
            }
        }

        /// <summary>
        /// 生成密码哈希 - 测试用
        /// </summary>
        [HttpGet("hashPassword")]
        [AllowAnonymous] // 测试端点
        public ActionResult<ApiResponse<object>> HashPassword([FromQuery] string password)
        {
            try
            {
                var hash = PasswordHelper.Hash(password);
                return Ok(ApiResponse<object>.Success(new { Password = password, Hash = hash }));
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "生成密码哈希", new { password });
            }
        }

        /// <summary>
        /// 验证密码哈希 - 测试用
        /// </summary>
        [HttpPost("verifyPassword")]
        [AllowAnonymous] // 测试端点
        public ActionResult<ApiResponse<object>> VerifyPassword([FromBody] VerifyPasswordRequest request)
        {
            try
            {
                var isValid = PasswordHelper.Verify(request.Hash, request.Password);
                return Ok(ApiResponse<object>.Success(new { 
                    Password = request.Password, 
                    Hash = request.Hash, 
                    IsValid = isValid 
                }));
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "验证密码哈希", request);
            }
        }

        /// <summary>
        /// 详细测试密码哈希和验证 - 调试用
        /// </summary>
        [HttpPost("debugPasswordVerification")]
        [AllowAnonymous] // 调试端点
        public ActionResult<ApiResponse<object>> DebugPasswordVerification([FromBody] DebugPasswordRequest request)
        {
            try
            {
                // 步骤1：生成新哈希
                var newHash = PasswordHelper.Hash(request.Password);
                
                // 步骤2：验证新生成的哈希
                var verifyNewHash = PasswordHelper.Verify(newHash, request.Password);
                
                // 步骤3：验证提供的哈希（如果有）
                var verifyProvidedHash = !string.IsNullOrEmpty(request.ProvidedHash) ? 
                    PasswordHelper.Verify(request.ProvidedHash, request.Password) : false;
                
                // 步骤4：直接使用PasswordHasher测试
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
                var directHash = hasher.HashPassword(null, request.Password);
                var directVerify = hasher.VerifyHashedPassword(null, directHash, request.Password);
                
                return Ok(ApiResponse<object>.Success(new { 
                    InputPassword = request.Password,
                    InputPasswordLength = request.Password.Length,
                    ProvidedHash = request.ProvidedHash,
                    
                    NewGeneratedHash = newHash,
                    NewHashVerification = verifyNewHash,
                    
                    ProvidedHashVerification = verifyProvidedHash,
                    
                    DirectHash = directHash,
                    DirectVerification = directVerify.ToString(),
                    
                    HashesAreEqual = newHash == directHash,
                    ProvidedHashEqualsNew = request.ProvidedHash == newHash,
                    ProvidedHashEqualsDirect = request.ProvidedHash == directHash
                }));
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "调试密码验证", request);
            }
        }

        /// <summary>
        /// 测试从数据库获取管理员密码哈希并验证
        /// </summary>
        [HttpPost("testAdminLogin")]
        [AllowAnonymous] // 测试端点
        public async Task<ActionResult<ApiResponse<object>>> TestAdminLogin([FromBody] TestAdminLoginRequest request)
        {
            try
            {
                var storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                var isValid = PasswordHelper.Verify(storedHash ?? "", request.Password);
                
                return Ok(ApiResponse<object>.Success(new { 
                    Username = request.Username,
                    Password = request.Password, 
                    StoredHash = storedHash,
                    HashLength = storedHash?.Length ?? 0,
                    IsValid = isValid 
                }));
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "测试管理员登录", request);
            }
        }

        /// <summary>
        /// 完整登录流程调试端点
        /// </summary>
        [HttpPost("debugFullLogin")]
        [AllowAnonymous] // 调试端点
        public async Task<ActionResult<ApiResponse<object>>> DebugFullLogin([FromBody] LoginRequestDto dto)
        {
            try
            {
                // 步骤1：验证登录类型
                var result = new { Step = 1, Message = "开始登录流程", Success = true, Data = (object?)null };
                
                // 步骤2：获取用户
                var user = await _sysAdminHandler.GetSysAdminUserAsync(dto.Username);
                if (user == null)
                {
                    return Ok(ApiResponse<object>.Success(new { Step = 2, Message = "获取用户失败", Success = false, User = (object?)null }));
                }
                
                // 步骤3：获取密码哈希并验证
                var storedHash = await _sysAdminHandler.GetSysAdminPasswordHashAsync();
                var isPasswordValid = PasswordHelper.Verify(storedHash ?? "", dto.Password);
                
                if (!isPasswordValid)
                {
                    return Ok(ApiResponse<object>.Success(new { Step = 3, Message = "密码验证失败", Success = false, StoredHash = storedHash, IsPasswordValid = isPasswordValid }));
                }
                
                // 步骤4：生成JWT令牌
                var token = _jwtService.GenerateToken(user.Id.ToString(), user.UserName, new[] { user.Role.ToString() });
                
                return Ok(ApiResponse<object>.Success(new { 
                    Step = 4, 
                    Message = "登录成功", 
                    Success = true, 
                    User = new { user.Id, user.UserName, user.RealName, user.Role },
                    Token = token,
                    StoredHash = storedHash,
                    IsPasswordValid = isPasswordValid
                }));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Success(new { 
                    Step = -1, 
                    Message = $"异常: {ex.Message}", 
                    Success = false, 
                    Exception = ex.ToString() 
                }));
            }
        }
    }

    public class VerifyPasswordRequest
    {
        public string Hash { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class TestAdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class DebugPasswordRequest
    {
        public string Password { get; set; } = string.Empty;
        public string ProvidedHash { get; set; } = string.Empty;
    }
}