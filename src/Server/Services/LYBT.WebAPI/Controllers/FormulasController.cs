using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 验方管理 API - 基础CRUD功能
    /// </summary>
    /// optimize-api-permissions: 验方管理需Doctor或Admin角色
    /// 资源级授权由Service层所有权检查实现
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class FormulasController : BaseApiController
    {
        private readonly IFormulaService _service;
        private readonly IFormulaImportExportService _importExportService;

        public FormulasController(
            IFormulaService service, 
            IFormulaImportExportService importExportService,
            ILogger<FormulasController> logger)
            : base(logger)
        {
            _service = service;
            _importExportService = importExportService;
        }

        /// <summary>
        /// 获取验方列表 - 支持分页和查询（Issue #1164: 扩展支持分类筛选）
        /// optimize-api-permissions: 添加角色过滤，Doctor只能看到自己的和共享的验方
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "FormulasCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaListDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return ValidationFail("页码和页大小参数无效（页码>0，页大小1-100）");
            }

            // optimize-api-permissions: 获取当前用户信息用于角色过滤
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;

            var result = await _service.GetPagedAsync(
                page, pageSize, keyword, category,
                currentUserId: operatorId,
                isAdmin: isAdmin);
            return HandlePagedResult(result, "查询成功");
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var result = await _service.GetByIdAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return NotFound(result.ErrorMessage ?? "验方不存在");
            }

            return Success(result.Data, "查询成功");
        }

        /// <summary>
        /// 新增验方
        /// OpenSpec: implement-formula-copy-flow - 传递当前用户ID用于设置验方所有权
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> Create([FromBody] FormulaInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // OpenSpec: implement-formula-copy-flow - 获取当前用户ID并传递给服务
            var (operatorId, _, _) = GetOperator();
            var result = await _service.CreateAsync(dto, operatorId);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "新增验方失败");
            }

            LogOperation("新增验方成功", result.Data, result.Data.Id);
            return Success(result.Data, "验方创建成功");
        }

        /// <summary>
        /// 更新验方
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _service.GetByIdAsync, "验方");
            if (ownershipError != null) return ownershipError;

            var result = await _service.UpdateAsync(id, dto);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "更新验方失败");
            }

            LogOperation("更新验方成功", result.Data, id);
            return Success(result.Data, "验方更新成功");
        }

        /// <summary>
        /// 删除验方
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _service.GetByIdAsync, "验方");
            if (ownershipError != null) return ownershipError;

            var result = await _service.DeleteAsync(id);
            if (!result.IsSuccess)
            {
                return NotFound("验方不存在");
            }

            LogOperation("删除验方成功", null, id);
            return Success(true, "删除成功");
        }

        /// <summary>
        /// 批量导入验方数据 (Issue #1166, #1758)
        /// 架构原则：Server端只处理结构化DTO，Excel解析由Client端负责
        /// OpenSpec: standardize-api-naming - REQ-API-002 批量操作URL模式
        /// </summary>
        [HttpPost("batch-import")]
        [ProducesResponseType(typeof(ApiResponse<FormulaBatchImportResultDto>), 200)]
        public async Task<IActionResult> Import([FromBody] FormulaBatchImportInputDto request)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (request == null || request.Formulas == null || !request.Formulas.Any())
            {
                return ValidationFail("导入数据不能为空");
            }

            // OpenSpec: refactor-server-srp-patterns - 使用独立的导入导出服务
            var result = await _importExportService.ImportFromDataAsync(request.Formulas, request.FileName);

            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "导入失败");
            }

            LogOperation("批量导入验方",
                new { FileName = request.FileName, TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount },
                null);

            return Success(result.Data, result.Data.Message);
        }

        /// <summary>
        /// 导出验方数据到Excel (Issue #1166)
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> Export([FromQuery] string? category = null)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // OpenSpec: refactor-server-srp-patterns - 使用独立的导入导出服务
            var stream = await _importExportService.ExportAsync(category);
            var fileName = string.IsNullOrWhiteSpace(category)
                ? $"验方数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                : $"验方数据_{category}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            LogOperation("导出验方数据", new { Category = category, FileName = fileName }, null);

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// 下载验方导入模板 (Issue #1166)
        /// </summary>
        [HttpGet("import-template")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public IActionResult ExportTemplate()
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // OpenSpec: refactor-server-srp-patterns - 使用独立的导入导出服务
            var stream = _importExportService.GenerateImportTemplate();
            var fileName = $"验方导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// </summary>
        [HttpGet("pending-validation")]
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> GetPendingValidation()
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var result = await _service.GetPendingValidationFormulasAsync();

            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "获取待校验验方列表失败");
            }

            return Success(result.Data, $"查询成功，共{result.Data.Count}个待校验验方");
        }

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ValidateHerb(
            Guid formulaId,
            Guid herbItemId,
            [FromBody] Guid selectedHerbId)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(formulaId, "验方ID") is { } error1) return error1;
            if (ValidateGuid(herbItemId, "药材项ID") is { } error2) return error2;
            if (ValidateGuid(selectedHerbId, "系统药材ID") is { } error3) return error3;

            var result = await _service.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

            if (!result.IsSuccess)
            {
                return BusinessFail(result.ErrorMessage ?? "验证药材失败");
            }

            LogOperation("验证验方药材",
                new { FormulaId = formulaId, HerbItemId = herbItemId, SelectedHerbId = selectedHerbId },
                formulaId);

            return Success(result.Message ?? "药材验证成功");
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复端点 ==========

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _service.GetByIdAsync, "验方");
            if (ownershipError != null) return ownershipError;

            var result = await _service.ToggleStatusAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "状态切换失败");
            }

            LogOperation("切换验方状态", new { NewStatus = result.Data.Status }, id);
            return Success(result.Data, $"验方已{(result.Data.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的验方
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            // 注: Restore不能使用GetEntityWithOwnershipCheckAsync，因为GetByIdAsync
            // 受全局软删除过滤器影响无法找到已删除记录。
            // RestoreAsync内部使用GetByIdIncludingDeletedAsync绕过过滤器。
            var result = await _service.RestoreAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "恢复失败");
            }

            LogOperation("恢复验方", null, id);
            return Success(result.Data, "验方已恢复");
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除验方
        /// </summary>
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("请至少选择一个方剂");

            var result = await _service.BatchDeleteAsync(dto.Ids);
            if (!result.IsSuccess || result.Data == null)
                return BusinessFail(result.ErrorMessage ?? "批量删除失败");

            LogOperation("批量删除方剂", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        /// <summary>
        /// 批量启用方剂
        /// </summary>
        [HttpPost("batch-enable")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个方剂");
            }

            var result = await _service.BatchUpdateStatusAsync(dto.Ids, CommonStatus.Enabled);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量启用失败");
            }

            LogOperation("批量启用方剂", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        /// <summary>
        /// 批量禁用方剂
        /// </summary>
        [HttpPost("batch-disable")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个方剂");
            }

            var result = await _service.BatchUpdateStatusAsync(dto.Ids, CommonStatus.Disabled);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量禁用失败");
            }

            LogOperation("批量禁用方剂", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }
    }
}
