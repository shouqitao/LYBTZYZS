using Asp.Versioning;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 挂号管理 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class RegistrationController : ControllerBase {
        private readonly IRegistrationService _registrationService;
        private readonly ILogger<RegistrationController> _logger;

        /// <summary>
        /// 构造方法，注入挂号服务
        /// </summary>
        public RegistrationController(IRegistrationService registrationService, ILogger<RegistrationController> logger) {
            _registrationService = registrationService;
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
        /// 获取挂号列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<RegistrationDto>>>> GetList() {
            try {
                var list = await _registrationService.GetListAsync();
                return Ok(ApiResponse<List<RegistrationDto>>.Success(list));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取挂号列表失败");
                return StatusCode(500, ApiResponse<List<RegistrationDto>>.Fail("获取挂号列表失败", 500));
            }
        }

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RegistrationDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<RegistrationDetailDto>.Fail("挂号ID不能为空", 400));
                }

                var detail = await _registrationService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(ApiResponse<RegistrationDetailDto>.Fail("挂号记录不存在", 404));
                }
                return Ok(ApiResponse<RegistrationDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取挂号详情失败，ID: {RegistrationId}", id);
                return StatusCode(500, ApiResponse<RegistrationDetailDto>.Fail("获取挂号详情失败", 500));
            }
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] RegistrationCreateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.AddAsync(dto);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("新增挂号失败", 400));
                }

                _logger.LogInformation("新增挂号成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "新增挂号成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增挂号失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增挂号失败", 500));
            }
        }

        /// <summary>
        /// 编辑挂号
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] RegistrationEditDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.UpdateAsync(dto);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("编辑挂号失败", 400));
                }

                _logger.LogInformation("编辑挂号成功，挂号ID: {RegistrationId}，操作者: {OperatorName}({OperatorId})", dto.Id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "编辑挂号成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑挂号失败，挂号ID: {RegistrationId}", dto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑挂号失败", 500));
            }
        }

        /// <summary>
        /// 删除挂号
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("挂号ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.DeleteAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("挂号记录不存在", 404));
                }

                _logger.LogInformation("删除挂号成功，挂号ID: {RegistrationId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "删除挂号成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除挂号失败，挂号ID: {RegistrationId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除挂号失败", 500));
            }
        }

        /// <summary>
        /// 取消挂号（软删除）
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("挂号ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.CancelAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("挂号记录不存在", 404));
                }

                _logger.LogInformation("取消挂号成功，挂号ID: {RegistrationId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "取消挂号成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "取消挂号失败，挂号ID: {RegistrationId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("取消挂号失败", 500));
            }
        }

        /// <summary>
        /// 分页查询挂号列表
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<RegistrationDto>>>> GetPaged([FromBody] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var (_, _, operatorRole) = GetOperator();
                var result = await _registrationService.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<RegistrationDto>>.Success(result));
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询挂号失败");
                return StatusCode(500, ApiResponse<PaginatedResult<RegistrationDto>>.Fail("分页查询挂号失败", 500));
            }
        }
    }
}