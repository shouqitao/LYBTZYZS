using Asp.Versioning;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;
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
    public class DoctorsController : BaseController {
        private readonly IDoctorService _doctorService;
        public DoctorsController(
            IDoctorService doctorService,
            IMemoryCache cache,
            ILogger<DoctorsController> logger) 
            : base(logger, cache) {
            _doctorService = doctorService;
        }

        /// <summary>
        /// 分页查询医生列表
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PaginatedResult<DoctorDto>>> GetPaged([FromBody] DoctorQueryDto query) {
            var (_, _, operatorRole) = GetOperator();
            var result = await _doctorService.GetPagedAsync(query, operatorRole);
            return Ok(result);
        }

        /// <summary>
        /// RESTful 获取医生列表 (GET /doctors) - 支持多字段模糊查询
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<DoctorDto>>> GetDoctors(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? realName = null,
            [FromQuery] string? specialty = null,
            [FromQuery] string? licenseNumber = null,
            [FromQuery] string? phoneNumber = null,
            [FromQuery] DoctorTitle? title = null,
            [FromQuery] DoctorStatus? status = null,
            [FromQuery] DoctorWorkStatus? workStatus = null,
            [FromQuery] bool? isActive = null) {
            var (_, _, operatorRole) = GetOperator();
            var query = new DoctorQueryDto {
                Page = page,
                PageSize = pageSize,
                Keyword = keyword,
                IsActive = isActive
            };
            var result = await _doctorService.GetPagedAsync(query, operatorRole);
            return Ok(result);
        }

        /// <summary>
        /// 搜索医生
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<DoctorDto>>> Search([FromQuery] string keyword = "") {
            var (_, _, operatorRole) = GetOperator();

            // 缓存搜索结果
            var cacheKey = $"doctor_search:{keyword}:{operatorRole}";
            if (!_cache.TryGetValue(cacheKey, out List<DoctorDto>? result)) {
                result = await _doctorService.SearchAsync(keyword, operatorRole);
                if (result != null) {
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                }
            }

            if (result != null) {
                return Ok(result);
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "搜索失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 获取所有在职医生列表（不分页）
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<DoctorDto>>> GetActiveList() {
            // 缓存在职医生列表
            if (!_cache.TryGetValue("active_doctors", out List<DoctorDto>? result)) {
                result = await _doctorService.GetActiveDoctorsAsync();
                if (result != null) {
                    _cache.Set("active_doctors", result, TimeSpan.FromMinutes(10));
                }
            }

            if (result != null) {
                return Ok(result);
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "获取失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 根据ID获取医生详情
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDetailDto>> GetById(Guid id) {
            var validationResult = ValidateGuid(id, "医生ID");
            if (validationResult != null) return validationResult;

            var (_, _, operatorRole) = GetOperator();

            // 缓存医生详情
            var cacheKey = $"doctor_detail:{id}:{operatorRole}";
            if (!_cache.TryGetValue(cacheKey, out DoctorDetailDto? result)) {
                result = await _doctorService.GetByIdAsync(id, operatorRole);
                if (result != null) {
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
                }
            }

            if (result != null) {
                return Ok(result);
            } else {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "未找到指定的医生",
                    Status = 404
                });
            }
        }

        /// <summary>
        /// 根据用户ID获取医生详情
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<DoctorDetailDto>> GetByUserId(Guid userId) {
            var validationResult = ValidateGuid(userId, "用户ID");
            if (validationResult != null) return validationResult;

            var (_, _, operatorRole) = GetOperator();
            var result = await _doctorService.GetByUserIdAsync(userId, operatorRole);

            if (result != null) {
                return Ok(result);
            } else {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "未找到对应的医生",
                    Status = 404
                });
            }
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] DoctorDetailDto dto) {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, operatorRole) = GetOperator();
            var result = await _doctorService.AddAsync(dto, operatorRole);

            if (result != null) {
                // 清除相关缓存
                _cache.Remove("active_doctors");
                LogOperation("新增医生档案成功", result, result.Id);
                return Ok(result);
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "医生档案创建失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 更新医生信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<DoctorDto>> Update(Guid id, [FromBody] DoctorDetailDto dto) {
            dto.Id = id; // 确保ID一致
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, operatorRole) = GetOperator();
            var result = await _doctorService.UpdateAsync(dto, operatorRole, operatorId);

            if (result) {
                // 清除相关缓存
                _cache.Remove($"doctor_detail:{dto.Id}:Admin");
                _cache.Remove($"doctor_detail:{dto.Id}:DiagnosingDoctor");
                _cache.Remove($"doctor_detail:{dto.Id}:Staff");
                _cache.Remove("active_doctors");
                // 获取更新后的资源
                var updated = await _doctorService.GetByIdAsync(dto.Id, operatorRole);
                LogOperation("编辑医生信息成功", updated, dto.Id);
                return Ok(updated);
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "医生信息更新失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 禁用医生（软删除）
        /// </summary>
        [HttpPatch("{id}/disable")]
        public async Task<IActionResult> Disable(Guid id) {
            var validationResult = ValidateGuid(id, "医生ID");
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _doctorService.DisableAsync(id);

            if (result) {
                // 清除相关缓存
                ClearDoctorCache(id);
                LogOperation("禁用医生成功", null, id);
                return Ok(new { message = "医生已禁用" });
            } else {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "医生不存在",
                    Status = 404
                });
            }
        }

        /// <summary>
        /// 启用医生
        /// </summary>
        [HttpPatch("{id}/enable")]
        public async Task<IActionResult> Enable(Guid id) {
            var validationResult = ValidateGuid(id, "医生ID");
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _doctorService.EnableAsync(id);

            if (result) {
                // 清除相关缓存
                ClearDoctorCache(id);
                LogOperation("启用医生成功", null, id);
                return Ok(new { message = "医生已启用" });
            } else {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "医生不存在",
                    Status = 404
                });
            }
        }

        /// <summary>
        /// 切换医生状态（启用/禁用）
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id) {
            var validationResult = ValidateGuid(id, "医生ID");
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, operatorRole) = GetOperator();
            
            // 先获取医生当前状态
            var doctor = await _doctorService.GetByIdAsync(id, operatorRole);
            if (doctor == null) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "医生不存在",
                    Status = 404
                });
            }

            // 根据当前状态切换
            bool result;
            string message;
            if (doctor.Status == DoctorStatus.Active) {
                result = await _doctorService.DisableAsync(id);
                message = "医生已禁用";
            } else {
                result = await _doctorService.EnableAsync(id);
                message = "医生已启用";
            }

            if (result) {
                // 清除相关缓存
                ClearDoctorCache(id);
                LogOperation(message, null, id);
                return Ok(new { message });
            }
            
            return BadRequest(new ProblemDetails {
                Title = "操作失败",
                Detail = "状态切换失败",
                Status = 400
            });
        }

        /// <summary>
        /// 批量禁用医生
        /// </summary>
        [HttpPatch("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
            if (dto?.Ids == null || dto.Ids.Count == 0) {
                return BadRequest(new ProblemDetails {
                    Title = "参数验证失败",
                    Detail = "请选择要禁用的医生",
                    Status = 400
                });
            }

            var (operatorId, operatorName, _) = GetOperator();
            var count = await _doctorService.BatchDisableAsync(dto.Ids);

            // 清除所有医生相关缓存
            ClearAllDoctorCache();
            LogOperation("批量禁用医生成功", new { Count = count, Ids = dto.Ids }, null);
            return Ok(new { count = count, message = $"成功禁用 {count} 名医生" });
        }

        /// <summary>
        /// 批量启用医生
        /// </summary>
        [HttpPatch("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
            if (dto?.Ids == null || dto.Ids.Count == 0) {
                return BadRequest(new ProblemDetails {
                    Title = "参数验证失败",
                    Detail = "请选择要启用的医生",
                    Status = 400
                });
            }

            var (operatorId, operatorName, _) = GetOperator();
            var count = await _doctorService.BatchEnableAsync(dto.Ids);

            // 清除所有医生相关缓存
            ClearAllDoctorCache();
            LogOperation("批量启用医生成功", new { Count = count, Ids = dto.Ids }, null);
            return Ok(new { count = count, message = $"成功启用 {count} 名医生" });
        }

        /// <summary>
        /// 检查用户是否已关联医生档案
        /// </summary>
        [HttpGet("check-user-link/{userId}")]
        public async Task<ActionResult<bool>> CheckUserLink(Guid userId) {
            var validationResult = ValidateGuid(userId, "用户ID");
            if (validationResult != null) return validationResult;

            var result = await _doctorService.IsUserLinkedToDoctorAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// 获取用户角色枚举列表
        /// </summary>
        [HttpGet("roles")]
        public ActionResult<object> GetRoles() {
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
                        UserRole.RegistrationStaff => "前台",
                        _ => role.ToString()
                    }
                })
                .ToList();

            return Ok(roles);
        }

        // ======================== RESTful 标准接口 ========================

        // 注意：已有 GetActiveList() 作为获取医生列表的GET端点

        /// <summary>
        /// 创建新医生 (RESTful POST /Doctors)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDoctor([FromBody] DoctorDetailDto dto) {
            var (operatorId, operatorName, operatorRole) = GetOperator();
            var result = await _doctorService.AddAsync(dto, operatorRole);
            if (result != null) {
                ClearAllDoctorCache();
                LogOperation("创建医生成功", result, result.Id);
                return Ok(result);
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "医生创建失败",
                    Status = 400
                });
            }
        }

        // 注意：已有 Update(Guid id, DoctorDetailDto dto) 作为 PUT 端点

        /// <summary>
        /// 删除医生 (RESTful DELETE /Doctors/{id}) - 实际执行软删除
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(Guid id) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _doctorService.DisableAsync(id);
            if (result) {
                ClearDoctorCache(id);
                ClearAllDoctorCache();
                return Ok(new { message = "医生已禁用" });
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "禁用医生失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 清除指定医生的缓存
        /// </summary>
        private void ClearDoctorCache(Guid doctorId) {
            var roleKeys = new[] { "Admin", "DiagnosingDoctor", "RegistrationStaff" };
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