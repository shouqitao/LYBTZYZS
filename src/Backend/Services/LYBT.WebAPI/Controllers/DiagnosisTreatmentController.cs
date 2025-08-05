using Asp.Versioning;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 诊疗 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class DiagnosisTreatmentController : ControllerBase {
        private readonly IDiagnosisTreatmentService _diagnosisTreatmentService;
        private readonly ILogger<DiagnosisTreatmentController> _logger;

        /// <summary>
        /// 构造方法，注入诊疗服务
        /// </summary>
        public DiagnosisTreatmentController(IDiagnosisTreatmentService diagnosisTreatmentService, ILogger<DiagnosisTreatmentController> logger) {
            _diagnosisTreatmentService = diagnosisTreatmentService;
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
                var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.RegistrationStaff;
                return (opId, userName, role);
            }
            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        /// <summary>
        /// 获取诊疗列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DiagnosisTreatmentDto>>> GetList() {
            try {
                var list = await _diagnosisTreatmentService.GetListAsync();
                return Ok(list);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取诊疗列表失败");
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "获取诊疗列表失败" });
            }
        }

        /// <summary>
        /// 分页获取诊疗列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<DiagnosisTreatmentDto>>> GetPagedList([FromQuery] PaginationRequest query) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ProblemDetails { Title = "参数验证失败", Detail = errors });
                }

                var (_, _, operatorRole) = GetOperator();
                var result = await _diagnosisTreatmentService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "分页获取诊疗列表失败");
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "分页获取诊疗列表失败" });
            }
        }

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DiagnosisTreatmentDetailDto>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "诊疗ID不能为空" });
                }

                var detail = await _diagnosisTreatmentService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "诊疗记录不存在" });
                }
                return Ok(detail);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取诊疗详情失败，ID: {DiagnosisTreatmentId}", id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "获取诊疗详情失败" });
            }
        }

        /// <summary>
        /// 新增诊疗
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> Add([FromBody] DiagnosisTreatmentCreateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ProblemDetails { Title = "参数验证失败", Detail = errors });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _diagnosisTreatmentService.AddAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增诊疗失败" });
                }

                _logger.LogInformation("新增诊疗成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return Ok(new { message = "新增诊疗成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "新增诊疗失败");
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "新增诊疗失败" });
            }
        }

        /// <summary>
        /// 编辑诊疗
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<object>> Update([FromBody] DiagnosisTreatmentEditDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ProblemDetails { Title = "参数验证失败", Detail = errors });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _diagnosisTreatmentService.UpdateAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "编辑诊疗失败" });
                }

                _logger.LogInformation("编辑诊疗成功，诊疗ID: {DiagnosisTreatmentId}，操作者: {OperatorName}({OperatorId})", dto.Id, operatorName, operatorId);
                return Ok(new { message = "编辑诊疗成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑诊疗失败，诊疗ID: {DiagnosisTreatmentId}", dto.Id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "编辑诊疗失败" });
            }
        }

        /// <summary>
        /// 删除诊疗
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "诊疗ID不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _diagnosisTreatmentService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "诊疗记录不存在" });
                }

                _logger.LogInformation("删除诊疗成功，诊疗ID: {DiagnosisTreatmentId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(new { message = "删除诊疗成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "删除诊疗失败，诊疗ID: {DiagnosisTreatmentId}", id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "删除诊疗失败" });
            }
        }
    }
}