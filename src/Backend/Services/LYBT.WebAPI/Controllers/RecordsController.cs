using Asp.Versioning;
using LYBT.Module.Records.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 病历 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Records")]
    [Authorize]
    public class RecordsController : ControllerBase {
        private readonly IRecordService _recordService;
        private readonly ILogger<RecordsController> _logger;

        /// <summary>
        /// 构造方法，注入病历服务
        /// </summary>
        public RecordsController(IRecordService recordService, ILogger<RecordsController> logger) {
            _recordService = recordService;
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
        /// 获取病历列表 (RESTful GET /Records) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResult<RecordDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] string? diagnosis = null,
            [FromQuery] string? chiefComplaint = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? isShared = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && string.IsNullOrEmpty(diagnosis) && string.IsNullOrEmpty(chiefComplaint) &&
                    !startDate.HasValue && !endDate.HasValue && !isShared.HasValue) {
                    
                    var list = await _recordService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<RecordDto>();
                    var result = new PaginatedResult<RecordDto> {
                        TotalCount = totalCount,
                        Items = pagedList
                    };
                    return Ok(ApiResponse<PaginatedResult<RecordDto>>.Success(result));
                }

                // 使用分页查询服务 (简化版本，只保留基本搜索功能)
                var query = new PaginationRequest {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword
                };
                
                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _recordService.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<RecordDto>>.Success(pagedResult));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取病历列表失败");
                return StatusCode(500, ApiResponse<PaginatedResult<RecordDto>>.Fail("获取病历列表失败", 500));
            }
        }

        /// <summary>
        /// 分页查询病历列表
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<RecordDto>>>> GetPaged([FromBody] PaginationRequest query) {
            try {
                var (_, _, operatorRole) = GetOperator();
                var result = await _recordService.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<RecordDto>>.Success(result));
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询病历失败");
                return StatusCode(500, ApiResponse<PaginatedResult<RecordDto>>.Fail("分页查询病历失败", 500));
            }
        }

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<ApiResponse<List<RecordDto>>>> GetByPatient(Guid patientId) {
            try {
                if (patientId == Guid.Empty) {
                    return BadRequest(ApiResponse<List<RecordDto>>.Fail("患者ID不能为空", 400));
                }

                var list = await _recordService.GetByPatientIdAsync(patientId);
                return Ok(ApiResponse<List<RecordDto>>.Success(list));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取患者病历失败，患者ID: {PatientId}", patientId);
                return StatusCode(500, ApiResponse<List<RecordDto>>.Fail("获取患者病历失败", 500));
            }
        }

        /// <summary>
        /// 获取病历详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RecordDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<RecordDetailDto>.Fail("病历ID不能为空", 400));
                }

                var detail = await _recordService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(ApiResponse<RecordDetailDto>.Fail("病历不存在", 404));
                }
                return Ok(ApiResponse<RecordDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取病历详情失败，ID: {RecordId}", id);
                return StatusCode(500, ApiResponse<RecordDetailDto>.Fail("获取病历详情失败", 500));
            }
        }

        /// <summary>
        /// 新增病历
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] RecordCreateDto recordCreateDto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _recordService.AddAsync(recordCreateDto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("新增病历失败", 400));
                }

                _logger.LogInformation("新增病历成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "新增病历成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增病历失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增病历失败", 500));
            }
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] RecordEditDto recordEditDto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _recordService.UpdateAsync(recordEditDto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("编辑病历失败", 400));
                }

                _logger.LogInformation("编辑病历成功，病历ID: {RecordId}，操作者: {OperatorName}({OperatorId})", recordEditDto.Id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "编辑病历成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑病历失败，病历ID: {RecordId}", recordEditDto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑病历失败", 500));
            }
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("病历ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _recordService.DeleteAsync(id, operatorId, operatorName);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("病历不存在", 404));
                }

                _logger.LogInformation("删除病历成功，病历ID: {RecordId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "删除病历成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除病历失败，病历ID: {RecordId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除病历失败", 500));
            }
        }

        /// <summary>
        /// 标记病历为共享
        /// </summary>
        [HttpPost("share/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> MarkAsShared(Guid id, [FromBody] List<string> doctorIds) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("病历ID不能为空", 400));
                }

                var result = await _recordService.MarkAsSharedAsync(id, doctorIds);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("病历不存在", 404));
                }

                _logger.LogInformation("病历共享设置成功，病历ID: {RecordId}", id);
                return Ok(ApiResponse<object>.Success(new { }, "病历共享设置成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "病历共享设置失败，病历ID: {RecordId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("病历共享设置失败", 500));
            }
        }

        /// <summary>
        /// 取消病历共享
        /// </summary>
        [HttpPost("unshare/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> RevokeSharing(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("病历ID不能为空", 400));
                }

                var result = await _recordService.RevokeSharingAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("病历不存在", 404));
                }

                _logger.LogInformation("病历共享取消成功，病历ID: {RecordId}", id);
                return Ok(ApiResponse<object>.Success(new { }, "病历共享取消成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "病历共享取消失败，病历ID: {RecordId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("病历共享取消失败", 500));
            }
        }

        /// <summary>
        /// 获取共享给指定医生的病历列表
        /// </summary>
        [HttpGet("shared/{doctorId}")]
        public async Task<ActionResult<ApiResponse<List<RecordDto>>>> GetShared(Guid doctorId) {
            try {
                if (doctorId == Guid.Empty) {
                    return BadRequest(ApiResponse<List<RecordDto>>.Fail("医生ID不能为空", 400));
                }

                var list = await _recordService.GetSharedRecordsAsync(doctorId);
                return Ok(ApiResponse<List<RecordDto>>.Success(list));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取共享病历失败，医生ID: {DoctorId}", doctorId);
                return StatusCode(500, ApiResponse<List<RecordDto>>.Fail("获取共享病历失败", 500));
            }
        }
    }
}