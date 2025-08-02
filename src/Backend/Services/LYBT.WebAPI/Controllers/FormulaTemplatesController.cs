using Asp.Versioning;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 经验方模板 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/FormulaTemplates")]
    [Authorize]
    public class FormulaTemplatesController : ControllerBase {
        private readonly IFormulaTemplateService _service;

        private readonly ILogger<FormulaTemplatesController> _logger;

        /// <summary>
        /// 构造方法，注入经验方模板服务
        /// </summary>
        public FormulaTemplatesController(IFormulaTemplateService service, ILogger<FormulaTemplatesController> logger) {
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
        /// 获取所有模板列表 (RESTful GET /FormulaTemplates) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResult<FormulaTemplateDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? name = null,
            [FromQuery] string? effect = null,
            [FromQuery] string? usage = null,
            [FromQuery] string? property = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isShared = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(name) && 
                    string.IsNullOrEmpty(effect) && string.IsNullOrEmpty(usage) && string.IsNullOrEmpty(property) &&
                    !isActive.HasValue && !isShared.HasValue) {
                    
                    var list = await _service.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<FormulaTemplateDto>();
                    var result = new PaginatedResult<FormulaTemplateDto> {
                        TotalCount = totalCount,
                        Items = pagedList
                    };
                    return Ok(ApiResponse<PaginatedResult<FormulaTemplateDto>>.Success(result));
                }

                // 使用分页查询服务
                var query = new LYBT.Shared.Models.Common.PaginationRequest {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword ?? name
                };
                
                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _service.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<FormulaTemplateDto>>.Success(pagedResult));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取验方模板列表失败");
                return StatusCode(500, ApiResponse<PaginatedResult<FormulaTemplateDto>>.Fail("获取验方模板列表失败", 500));
            }
        }

        /// <summary>
        /// 分页查询验方模板列表
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<FormulaTemplateDto>>>> GetPaged([FromBody] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var (_, _, operatorRole) = GetOperator();
                var result = await _service.GetPagedAsync(query, operatorRole);
                return Ok(ApiResponse<PaginatedResult<FormulaTemplateDto>>.Success(result));
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询验方模板失败");
                return StatusCode(500, ApiResponse<PaginatedResult<FormulaTemplateDto>>.Fail("分页查询验方模板失败", 500));
            }
        }

        /// <summary>
        /// 获取模板详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<FormulaTemplateDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<FormulaTemplateDetailDto>.Fail("模板ID不能为空", 400));
                }

                var detail = await _service.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(ApiResponse<FormulaTemplateDetailDto>.Fail("验方模板不存在", 404));
                }
                return Ok(ApiResponse<FormulaTemplateDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取验方模板详情失败，ID: {TemplateId}", id);
                return StatusCode(500, ApiResponse<FormulaTemplateDetailDto>.Fail("获取验方模板详情失败", 500));
            }
        }

        /// <summary>
        /// 新增模板
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] FormulaTemplateCreateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.AddAsync(dto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("新增验方模板失败", 400));
                }

                _logger.LogInformation("新增验方模板成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return StatusCode(201, ApiResponse<object>.Success(new { }, "新增验方模板成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增验方模板失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增验方模板失败", 500));
            }
        }

        /// <summary>
        /// 编辑模板
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] FormulaTemplateEditDto dto) {
            dto.Id = id; // 确保ID一致
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.UpdateAsync(dto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("编辑验方模板失败", 400));
                }

                _logger.LogInformation("编辑验方模板成功，模板ID: {TemplateId}，操作者: {OperatorName}({OperatorId})", dto.Id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "编辑验方模板成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑验方模板失败，模板ID: {TemplateId}", dto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑验方模板失败", 500));
            }
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("模板ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.DeleteAsync(id, operatorId, operatorName);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("验方模板不存在", 404));
                }

                _logger.LogInformation("删除验方模板成功，模板ID: {TemplateId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "删除验方模板成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除验方模板失败，模板ID: {TemplateId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除验方模板失败", 500));
            }
        }

        /// <summary>
        /// 批量导入验方模板
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<object>>> Import([FromBody] List<FormulaTemplateImportDto> dtos) {
            try {
                if (dtos == null || !dtos.Any()) {
                    return BadRequest(ApiResponse<object>.Fail("导入数据不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var count = await _service.ImportAsync(dtos, operatorId, operatorName);
                _logger.LogInformation("批量导入验方模板成功，导入数量: {Count}，操作者: {OperatorName}({OperatorId})", count, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { Imported = count }, $"成功导入 {count} 个验方模板"));
            } catch (Exception ex) {
                _logger.LogError(ex, "批量导入验方模板失败");
                return StatusCode(500, ApiResponse<object>.Fail("批量导入验方模板失败", 500));
            }
        }

        /// <summary>
        /// 导出验方模板数据
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<ApiResponse<List<FormulaTemplateDetailDto>>>> Export() {
            try {
                var data = await _service.ExportAsync();
                return Ok(ApiResponse<List<FormulaTemplateDetailDto>>.Success(data, "导出验方模板数据成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "导出验方模板数据失败");
                return StatusCode(500, ApiResponse<List<FormulaTemplateDetailDto>>.Fail("导出验方模板数据失败", 500));
            }
        }
    }
}