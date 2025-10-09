using Asp.Versioning;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 用户管理控制器 - 简化版（仅CRUD）
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// 获取用户列表（分页）
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var result = await _userService.GetPagedAsync(page, pageSize, keyword);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<PagedResult<UserDto>>.CreateSuccess(result.Data));
                }
                return BadRequest(ApiResponse<PagedResult<UserDto>>.CreateFail(result.Message ?? "获取用户列表失败"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取用户列表失败");
                return StatusCode(500, ApiResponse<PagedResult<UserDto>>.CreateFail("获取用户列表失败"));
            }
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(ApiResponse<UserDto>.CreateFail("无法获取当前用户信息"));
                }

                // 特殊处理超级管理员
                if (userId == Guid.Empty)
                {
                    var username = User.Identity?.Name ?? "sysadmin";
                    var isSuperAdmin = User.FindFirst("IsSuperAdmin")?.Value == "true";
                    
                    if (isSuperAdmin)
                    {
                        // 返回超级管理员的虚拟用户信息
                        var superAdminDto = new UserDto
                        {
                            Id = Guid.Empty,
                            UserName = username,
                            RealName = "系统超级管理员",
                            Role = UserRole.Admin,
                            Email = "admin@lybt.com",
                            Status = CommonStatus.Enabled,
                            CreatedAt = DateTime.MinValue,
                            UpdatedAt = DateTime.Now
                        };
                        return Ok(ApiResponse<UserDto>.CreateSuccess(superAdminDto));
                    }
                }

                var result = await _userService.GetByIdAsync(userId);
                if (result.IsSuccess && result.Data != null)
                {
                    return Ok(ApiResponse<UserDto>.CreateSuccess(result.Data));
                }
                return NotFound(ApiResponse<UserDto>.CreateFail("用户不存在"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取当前用户信息失败");
                return StatusCode(500, ApiResponse<UserDto>.CreateFail("获取当前用户信息失败"));
            }
        }

        /// <summary>
        /// 获取单个用户
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(Guid id)
        {
            try
            {
                var result = await _userService.GetByIdAsync(id);

                if (result.IsSuccess && result.Data != null)
                {
                    return Ok(ApiResponse<UserDto>.CreateSuccess(result.Data));
                }
                return NotFound(ApiResponse<UserDto>.CreateFail("用户不存在"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取用户失败 {UserId}", id);
                return StatusCode(500, ApiResponse<UserDto>.CreateFail("获取用户失败"));
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
        {
            try
            {
                var result = await _userService.CreateAsync(dto);

                if (result.IsSuccess && result.Data != null)
                {
                    return CreatedAtAction(nameof(GetUser), new { id = result.Data.Id },
                        ApiResponse<UserDto>.CreateSuccess(result.Data));
                }
                return BadRequest(ApiResponse<UserDto>.CreateFail(result.Message ?? "创建用户失败"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建用户失败");
                return StatusCode(500, ApiResponse<UserDto>.CreateFail("创建用户失败"));
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                var result = await _userService.UpdateAsync(id, dto);

                if (result.IsSuccess && result.Data != null)
                {
                    return Ok(ApiResponse<UserDto>.CreateSuccess(result.Data));
                }
                return BadRequest(ApiResponse<UserDto>.CreateFail(result.Message ?? "更新用户失败"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新用户失败 {UserId}", id);
                return StatusCode(500, ApiResponse<UserDto>.CreateFail("更新用户失败"));
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var result = await _userService.DeleteAsync(id);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<object>.CreateSuccess(null, "删除成功"));
                }
                return BadRequest(ApiResponse<object>.CreateFail(result.Message ?? "删除用户失败"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "删除用户失败 {UserId}", id);
                return StatusCode(500, ApiResponse<object>.CreateFail("删除用户失败"));
            }
        }
    }
}
