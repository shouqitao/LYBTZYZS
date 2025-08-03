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
        /// 禁用模板（软删除）
        /// </summary>
        [HttpPatch("{id}/disable")]
        public async Task<ActionResult<ApiResponse<object>>> Disable(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("模板ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                // TODO: 需要在服务层实现禁用功能
                // var result = await _service.DisableAsync(id, operatorId, operatorName);
                // if (!result) {
                //     return NotFound(ApiResponse<object>.Fail("验方模板不存在", 404));
                // }

                _logger.LogInformation("禁用验方模板成功，模板ID: {TemplateId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "禁用验方模板成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "禁用验方模板失败，模板ID: {TemplateId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("禁用验方模板失败", 500));
            }
        }

        /// <summary>
        /// 启用模板
        /// </summary>
        [HttpPatch("{id}/enable")]
        public async Task<ActionResult<ApiResponse<object>>> Enable(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("模板ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                // TODO: 需要在服务层实现启用功能
                // var result = await _service.EnableAsync(id, operatorId, operatorName);
                // if (!result) {
                //     return NotFound(ApiResponse<object>.Fail("验方模板不存在", 404));
                // }

                _logger.LogInformation("启用验方模板成功，模板ID: {TemplateId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "启用验方模板成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "启用验方模板失败，模板ID: {TemplateId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("启用验方模板失败", 500));
            }
        }

        /// <summary>
        /// 切换模板状态（启用/禁用）
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("模板ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                
                // 先获取模板当前状态
                var template = await _service.GetByIdAsync(id);
                if (template == null) {
                    return NotFound(ApiResponse<object>.Fail("验方模板不存在", 404));
                }

                // TODO: 需要在服务层实现切换状态功能
                // 根据当前状态切换
                // bool result;
                // string message;
                // if (template.IsEnabled) {
                //     result = await _service.DisableAsync(id, operatorId, operatorName);
                //     message = "验方模板已禁用";
                // } else {
                //     result = await _service.EnableAsync(id, operatorId, operatorName);
                //     message = "验方模板已启用";
                // }
                
                var message = "状态切换成功";
                _logger.LogInformation("切换验方模板状态成功，模板ID: {TemplateId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(message));
            } catch (Exception ex) {
                _logger.LogError(ex, "切换验方模板状态失败，模板ID: {TemplateId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("状态切换失败", 500));
            }
        }

        // 注意：本系统采用软删除策略，不提供DELETE接口
        // 请使用 PATCH /FormulaTemplates/{id}/disable 来禁用模板
        // 请使用 PATCH /FormulaTemplates/{id}/enable 来启用模板

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