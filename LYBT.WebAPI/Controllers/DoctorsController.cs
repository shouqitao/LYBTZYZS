using LYBT.Common.Enums.Users;
using LYBT.Common.Models;
using LYBT.Common.Responses;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 医生管理接口
    /// 实现软删除策略：医生只能禁用/启用，不提供删除接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class DoctorsController : ControllerBase {
        private readonly IDoctorService _doctorService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DoctorsController> _logger;

        public DoctorsController(
            IDoctorService doctorService,
            IMemoryCache cache,
            ILogger<DoctorsController> logger) {
            _doctorService = doctorService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前操作者信息
        /// </summary>
        private (Guid operatorId, string operatorName, UserRole operatorRole) GetOperator() {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.Identity?.Name;
            var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName)) {
                var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.Staff;
                return (opId, userName, role);
            }
            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        /// <summary>
        /// 分页查询医生列表
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<ApiResponse<PagedResultDto<DoctorDto>>>> GetPaged([FromBody] DoctorQueryDto query) {
            try {
                var (_, _, operatorRole) = GetOperator();
                var result = await _doctorService.GetPagedAsync(query, operatorRole);

                if (result.IsSuccess) {
                    return Ok(result);
                } else {
                    return BadRequest(ApiResponse<object>.Fail(result.Message));
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询医生失败");
                return StatusCode(500, ApiResponse<object>.Fail("分页查询医生失败"));
            }
        }

        /// <summary>
        /// 搜索医生
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<DoctorDto>>>> Search([FromQuery] string keyword = "") {
            try {
                var (_, _, operatorRole) = GetOperator();

                // 缓存搜索结果
                var cacheKey = $"doctor_search:{keyword}:{operatorRole}";
                if (!_cache.TryGetValue(cacheKey, out ApiResponse<List<DoctorDto>>? result)) {
                    result = await _doctorService.SearchAsync(keyword, operatorRole);
                    if (result.IsSuccess) {
                        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                    }
                }

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "搜索医生失败，关键词: {Keyword}", keyword);
                return StatusCode(500, ApiResponse<object>.Fail("搜索医生失败"));
            }
        }

        /// <summary>
        /// 获取所有在职医生列表（不分页）
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<List<DoctorDto>>>> GetActiveList() {
            try {
                // 缓存在职医生列表
                if (!_cache.TryGetValue("active_doctors", out ApiResponse<List<DoctorDto>>? result)) {
                    result = await _doctorService.GetActiveDoctorsAsync();
                    if (result.IsSuccess) {
                        _cache.Set("active_doctors", result, TimeSpan.FromMinutes(10));
                    }
                }

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取在职医生列表失败");
                return StatusCode(500, ApiResponse<object>.Fail("获取在职医生列表失败"));
            }
        }

        /// <summary>
        /// 根据ID获取医生详情
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("医生ID不能为空"));
                }

                var (_, _, operatorRole) = GetOperator();

                // 缓存医生详情
                var cacheKey = $"doctor_detail:{id}:{operatorRole}";
                if (!_cache.TryGetValue(cacheKey, out ApiResponse<DoctorDetailDto>? result)) {
                    result = await _doctorService.GetByIdAsync(id, operatorRole);
                    if (result.IsSuccess) {
                        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
                    }
                }

                return result.IsSuccess ? Ok(result) : NotFound(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取医生详情失败，ID: {DoctorId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("获取医生详情失败"));
            }
        }

        /// <summary>
        /// 根据用户ID获取医生详情
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> GetByUserId(Guid userId) {
            try {
                if (userId == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("用户ID不能为空"));
                }

                var (_, _, operatorRole) = GetOperator();
                var result = await _doctorService.GetByUserIdAsync(userId, operatorRole);

                return result.IsSuccess ? Ok(result) : NotFound(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "根据用户ID获取医生失败，用户ID: {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.Fail("根据用户ID获取医生失败"));
            }
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<bool>>> Add([FromBody] DoctorDetailDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{string.Join("; ", errors)}"));
                }

                var (operatorId, operatorName, operatorRole) = GetOperator();
                var result = await _doctorService.AddAsync(dto, operatorRole);

                if (result.IsSuccess) {
                    // 清除相关缓存
                    _cache.Remove("active_doctors");
                    _logger.LogInformation("医生档案创建成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                }

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "新增医生失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增医生失败"));
            }
        }

        /// <summary>
        /// 更新医生信息
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] DoctorDetailDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{string.Join("; ", errors)}"));
                }

                var (operatorId, operatorName, operatorRole) = GetOperator();
                var result = await _doctorService.UpdateAsync(dto, operatorRole, operatorId);

                if (result.IsSuccess) {
                    // 清除相关缓存
                    _cache.Remove($"doctor_detail:{dto.Id}:Admin");
                    _cache.Remove($"doctor_detail:{dto.Id}:DiagnosingDoctor");
                    _cache.Remove($"doctor_detail:{dto.Id}:Staff");
                    _cache.Remove("active_doctors");
                    _logger.LogInformation("医生信息更新成功，医生ID: {DoctorId}，操作者: {OperatorName}({OperatorId})",
                        dto.Id, operatorName, operatorId);
                }

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "更新医生信息失败，医生ID: {DoctorId}", dto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("更新医生信息失败"));
            }
        }

        /// <summary>
        /// 禁用医生（软删除）
        /// </summary>
        [HttpPatch("{id}/disable")]
        public async Task<ActionResult<ApiResponse<bool>>> Disable(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("医生ID不能为空"));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _doctorService.DisableAsync(id);

                if (result.IsSuccess) {
                    // 清除相关缓存
                    ClearDoctorCache(id);
                    _logger.LogInformation("医生已禁用，医生ID: {DoctorId}，操作者: {OperatorName}({OperatorId})",
                        id, operatorName, operatorId);
                }

                return result.IsSuccess ? Ok(result) : NotFound(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "禁用医生失败，医生ID: {DoctorId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("禁用医生失败"));
            }
        }

        /// <summary>
        /// 启用医生
        /// </summary>
        [HttpPatch("{id}/enable")]
        public async Task<ActionResult<ApiResponse<bool>>> Enable(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("医生ID不能为空"));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _doctorService.EnableAsync(id);

                if (result.IsSuccess) {
                    // 清除相关缓存
                    ClearDoctorCache(id);
                    _logger.LogInformation("医生已启用，医生ID: {DoctorId}，操作者: {OperatorName}({OperatorId})",
                        id, operatorName, operatorId);
                }

                return result.IsSuccess ? Ok(result) : NotFound(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "启用医生失败，医生ID: {DoctorId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("启用医生失败"));
            }
        }

        /// <summary>
        /// 批量禁用医生
        /// </summary>
        [HttpPatch("batch-disable")]
        public async Task<ActionResult<ApiResponse<int>>> BatchDisable([FromBody] BatchIdsDto dto) {
            try {
                if (dto?.Ids == null || dto.Ids.Count == 0) {
                    return BadRequest(ApiResponse<object>.Fail("请选择要禁用的医生"));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _doctorService.BatchDisableAsync(dto.Ids);

                if (result.IsSuccess) {
                    // 清除所有医生相关缓存
                    ClearAllDoctorCache();
                    _logger.LogInformation("批量禁用医生成功，数量: {Count}，操作者: {OperatorName}({OperatorId})",
                        result.Data, operatorName, operatorId);
                }

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "批量禁用医生失败");
                return StatusCode(500, ApiResponse<object>.Fail("批量禁用医生失败"));
            }
        }

        /// <summary>
        /// 批量启用医生
        /// </summary>
        [HttpPatch("batch-enable")]
        public async Task<ActionResult<ApiResponse<int>>> BatchEnable([FromBody] BatchIdsDto dto) {
            try {
                if (dto?.Ids == null || dto.Ids.Count == 0) {
                    return BadRequest(ApiResponse<object>.Fail("请选择要启用的医生"));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _doctorService.BatchEnableAsync(dto.Ids);

                if (result.IsSuccess) {
                    // 清除所有医生相关缓存
                    ClearAllDoctorCache();
                    _logger.LogInformation("批量启用医生成功，数量: {Count}，操作者: {OperatorName}({OperatorId})",
                        result.Data, operatorName, operatorId);
                }

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "批量启用医生失败");
                return StatusCode(500, ApiResponse<object>.Fail("批量启用医生失败"));
            }
        }

        /// <summary>
        /// 检查用户是否已关联医生档案
        /// </summary>
        [HttpGet("check-user-link/{userId}")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckUserLink(Guid userId) {
            try {
                if (userId == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("用户ID不能为空"));
                }

                var result = await _doctorService.IsUserLinkedToDoctorAsync(userId);
                return Ok(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "检查用户关联状态失败，用户ID: {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.Fail("检查用户关联状态失败"));
            }
        }

        /// <summary>
        /// 获取用户角色枚举列表
        /// </summary>
        [HttpGet("roles")]
        public ActionResult<ApiResponse<object>> GetRoles() {
            try {
                var roles = Enum.GetValues<UserRole>()
                    .Select(role => new {
                        value = (int)role,
                        name = role.ToString(),
                        description = role switch {
                            UserRole.Admin => "管理员",
                            UserRole.DiagnosingDoctor => "医生",
                            UserRole.PharmacyStaff => "药剂师",
                            UserRole.PhysiotherapyStaff => "理疗师",
                            UserRole.CashierStaff => "收银员",
                            UserRole.Staff => "前台",
                            _ => role.ToString()
                        }
                    })
                    .ToList();

                return Ok(ApiResponse<object>.Success(roles));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取角色列表失败");
                return StatusCode(500, ApiResponse<object>.Fail("获取角色列表失败"));
            }
        }

        /// <summary>
        /// 清除指定医生的缓存
        /// </summary>
        private void ClearDoctorCache(Guid doctorId) {
            var roleKeys = new[] { "Admin", "DiagnosingDoctor", "Staff" };
            foreach (var role in roleKeys) {
                _cache.Remove($"doctor_detail:{doctorId}:{role}");
            }
            _cache.Remove("active_doctors");
        }

        /// <summary>
        /// 清除所有医生相关缓存
        /// </summary>
        private void ClearAllDoctorCache() {
            // 简单方式：移除已知的缓存键
            _cache.Remove("active_doctors");

            // 在生产环境中，可能需要实现更复杂的缓存清理策略
            // 例如使用缓存标签或者缓存前缀来批量清理
        }
    }
}