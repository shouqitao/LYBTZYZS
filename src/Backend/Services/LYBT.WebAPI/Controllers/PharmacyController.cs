using Asp.Versioning;
using LYBT.Module.Pharmacy.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 药房 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PharmacyController : ControllerBase {
        private readonly IPharmacyService _pharmacyService;
        private readonly ILogger<PharmacyController> _logger;

        /// <summary>
        /// 构造方法，注入药房服务
        /// </summary>
        public PharmacyController(IPharmacyService pharmacyService, ILogger<PharmacyController> logger) {
            _pharmacyService = pharmacyService;
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
        /// 获取待抓药的处方列表
        /// </summary>
        [HttpGet("waiting")]
        public async Task<ActionResult<ApiResponse<List<PharmacyDto>>>> GetWaitingList() {
            try {
                var list = await _pharmacyService.GetWaitingListAsync();
                return Ok(ApiResponse<List<PharmacyDto>>.Success(list));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取待抓药处方列表失败");
                return StatusCode(500, ApiResponse<List<PharmacyDto>>.Fail("获取待抓药处方列表失败", 500));
            }
        }

        /// <summary>
        /// 获取药房单列表 (RESTful GET /Pharmacy) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResult<PharmacyDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] PharmacyStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? needDecoction = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && !status.HasValue && !startDate.HasValue && 
                    !endDate.HasValue && !needDecoction.HasValue) {
                    
                    var list = await _pharmacyService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<PharmacyDto>();
                    var result = new PaginatedResult<PharmacyDto> {
                        TotalCount = totalCount,
                        Items = pagedList
                    };
                    return Ok(ApiResponse<PaginatedResult<PharmacyDto>>.Success(result));
                }

                // 使用分页查询服务 (简化版本，只保留基本搜索功能)
                var query = new LYBT.Shared.Models.Common.PaginationRequest {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword
                };
                
                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _pharmacyService.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<PharmacyDto>>.Success(pagedResult));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药房单列表失败");
                return StatusCode(500, ApiResponse<PaginatedResult<PharmacyDto>>.Fail("获取药房单列表失败", 500));
            }
        }

        /// <summary>
        /// 分页获取药房单列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<PharmacyDto>>>> GetPagedList([FromQuery] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<PaginatedResult<PharmacyDto>>.Fail($"参数验证失败：{errors}", 400));
                }

                var (_, _, operatorRole) = GetOperator();
                var result = await _pharmacyService.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<PharmacyDto>>.Success(result));
            } catch (Exception ex) {
                _logger.LogError(ex, "分页获取药房单列表失败");
                return StatusCode(500, ApiResponse<PaginatedResult<PharmacyDto>>.Fail("分页获取药房单列表失败", 500));
            }
        }

        /// <summary>
        /// 获取药房单详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PharmacyDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<PharmacyDetailDto>.Fail("药房单ID不能为空", 400));
                }

                var detail = await _pharmacyService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(ApiResponse<PharmacyDetailDto>.Fail("药房单不存在", 404));
                }
                return Ok(ApiResponse<PharmacyDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药房单详情失败，ID: {PharmacyId}", id);
                return StatusCode(500, ApiResponse<PharmacyDetailDto>.Fail("获取药房单详情失败", 500));
            }
        }

        /// <summary>
        /// 新增药房单
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] PharmacyCreateDto pharmacyCreateDto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.AddAsync(pharmacyCreateDto);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("新增药房单失败", 400));
                }

                _logger.LogInformation("新增药房单成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "新增药房单成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增药房单失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增药房单失败", 500));
            }
        }

        /// <summary>
        /// 编辑药房单
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] PharmacyEditDto pharmacyEditDto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.UpdateAsync(pharmacyEditDto);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("编辑药房单失败", 400));
                }

                _logger.LogInformation("编辑药房单成功，药房单ID: {PharmacyId}，操作者: {OperatorName}({OperatorId})", pharmacyEditDto.Id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "编辑药房单成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑药房单失败，药房单ID: {PharmacyId}", pharmacyEditDto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑药房单失败", 500));
            }
        }

        /// <summary>
        /// 删除药房单
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("药房单ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.DeleteAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("药房单不存在", 404));
                }

                _logger.LogInformation("删除药房单成功，药房单ID: {PharmacyId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "删除药房单成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除药房单失败，药房单ID: {PharmacyId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除药房单失败", 500));
            }
        }

        /// <summary>
        /// 标记处方为已抓药
        /// </summary>
        [HttpPost("{id}/prepared")]
        public async Task<ActionResult<ApiResponse<object>>> MarkAsPrepared(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("药房单ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.MarkAsPreparedAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("药房单不存在", 404));
                }

                _logger.LogInformation("标记处方为已抓药成功，药房单ID: {PharmacyId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "标记为已抓药成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "标记处方为已抓药失败，药房单ID: {PharmacyId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("标记为已抓药失败", 500));
            }
        }
    }
}