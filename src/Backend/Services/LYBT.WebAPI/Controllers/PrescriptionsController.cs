using Asp.Versioning;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 处方管理 API
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PrescriptionsController : ControllerBase {
        private readonly IPrescriptionService _service;
        private readonly ILogger<PrescriptionsController> _logger;

        public PrescriptionsController(IPrescriptionService service, ILogger<PrescriptionsController> logger) {
            _service = service;
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
        /// 获取处方列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetList() {
            try {
                var list = await _service.GetAllAsync();
                return Ok(ApiResponse<List<PrescriptionDto>>.Success(list));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取处方列表失败");
                return StatusCode(500, ApiResponse<List<PrescriptionDto>>.Fail("获取处方列表失败", 500));
            }
        }

        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<PrescriptionDto>>>> GetPagedList([FromQuery] PaginationRequest query) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<PaginatedResult<PrescriptionDto>>.Fail($"参数验证失败：{errors}", 400));
                }

                var (_, _, operatorRole) = GetOperator();
                var result = await _service.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<PrescriptionDto>>.Success(result));
            } catch (Exception ex) {
                _logger.LogError(ex, "分页获取处方列表失败");
                return StatusCode(500, ApiResponse<PaginatedResult<PrescriptionDto>>.Fail("分页获取处方列表失败", 500));
            }
        }

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<PrescriptionDetailDto>.Fail("处方ID不能为空", 400));
                }

                var detail = await _service.GetByIdAsync(id.ToString());
                if (detail == null) {
                    return NotFound(ApiResponse<PrescriptionDetailDto>.Fail("处方不存在", 404));
                }
                return Ok(ApiResponse<PrescriptionDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取处方详情失败，ID: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse<PrescriptionDetailDto>.Fail("获取处方详情失败", 500));
            }
        }

        /// <summary>
        /// 新增处方
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] PrescriptionCreateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CreateAsync(dto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("新增处方失败", 400));
                }

                _logger.LogInformation("新增处方成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "新增处方成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增处方失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增处方失败", 500));
            }
        }

        /// <summary>
        /// 编辑处方
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] PrescriptionEditDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.UpdateAsync(dto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("编辑处方失败", 400));
                }

                _logger.LogInformation("编辑处方成功，处方ID: {PrescriptionId}，操作者: {OperatorName}({OperatorId})", dto.Id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "编辑处方成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑处方失败，处方ID: {PrescriptionId}", dto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑处方失败", 500));
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("处方ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.DeleteAsync(id.ToString(), operatorId, operatorName);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("处方不存在", 404));
                }

                _logger.LogInformation("删除处方成功，处方ID: {PrescriptionId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "删除处方成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除处方失败，处方ID: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除处方失败", 500));
            }
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        [HttpPost("void/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("处方ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CancelAsync(id.ToString(), operatorId, operatorName);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("处方不存在", 404));
                }

                _logger.LogInformation("作废处方成功，处方ID: {PrescriptionId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "作废处方成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "作废处方失败，处方ID: {PrescriptionId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("作废处方失败", 500));
            }
        }
    }
}