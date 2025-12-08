using Asp.Versioning;
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
    /// 资源级授权通过FormulaAuthorizationHandler实现
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "DoctorOrAdmin")]
    public class FormulasController : BaseApiController
    {
        private readonly IFormulaService _service;

        public FormulasController(IFormulaService service, ILogger<FormulasController> logger)
            : base(logger)
        {
            _service = service;
        }

        /// <summary>
        /// 获取验方列表 - 支持分页和查询（Issue #1164: 扩展支持分类筛选）
        /// optimize-api-permissions: 添加角色过滤，Doctor只能看到自己的和共享的验方
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "FormulasCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null)
        {
            try
            {
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
            catch (Exception ex)
            {
                return HandleException(ex, "获取验方列表", new { page, pageSize, keyword, category });
            }
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "验方ID") is { } error) return error;

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound(result.ErrorMessage ?? "验方不存在");
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取验方详情", id);
            }
        }

        /// <summary>
        /// 新增验方
        /// OpenSpec: implement-formula-copy-flow - 传递当前用户ID用于设置验方所有权
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FormulaDto>), 200)]
        public async Task<IActionResult> Add([FromBody] FormulaInputDto dto)
        {
            try
            {
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
            catch (Exception ex)
            {
                return HandleException(ex, "新增验方", dto);
            }
        }

        /// <summary>
        /// 更新验方
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto dto)
        {
            try
            {
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
            catch (Exception ex)
            {
                return HandleException(ex, "更新验方", new { id, dto });
            }
        }

        /// <summary>
        /// 删除验方
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // 使用统一的所有权检查方法
                var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _service.GetByIdAsync, "验方");
                if (ownershipError != null) return ownershipError;

                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound("验方不存在");
                }

                LogOperation("删除验方成功", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除验方", new { Id = id });
            }
        }

        /// <summary>
        /// 批量导入验方数据 (Issue #1166, #1758)
        /// 架构原则：Server端只处理结构化DTO，Excel解析由Client端负责
        /// </summary>
        [HttpPost("import")]
        [ProducesResponseType(typeof(ApiResponse<FormulaImportResultDto>), 200)]
        public async Task<IActionResult> Import([FromBody] ImportFormulasDataRequest request)
        {
            try
            {
                if (request == null || request.Formulas == null || !request.Formulas.Any())
                {
                    return ValidationFail("导入数据不能为空");
                }

                var result = await _service.ImportFromDataAsync(request.Formulas, request.FileName);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "导入失败");
                }

                LogOperation("批量导入验方",
                    new { FileName = request.FileName, TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount },
                    null);

                return Success(result.Data, result.Data.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量导入验方", new { FileName = request?.FileName });
            }
        }

        /// <summary>
        /// 导出验方数据到Excel (Issue #1166)
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> Export([FromQuery] string? category = null)
        {
            try
            {
                var stream = await _service.ExportAsync(category);
                var fileName = string.IsNullOrWhiteSpace(category)
                    ? $"验方数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : $"验方数据_{category}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                LogOperation("导出验方数据", new { Category = category, FileName = fileName }, null);

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出验方数据失败，分类筛选：{Category}", category);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// 下载验方导入模板 (Issue #1166)
        /// </summary>
        [HttpGet("import-template")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public IActionResult ExportTemplate()
        {
            try
            {
                var stream = _service.GenerateImportTemplate();
                var fileName = $"验方导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成验方导入模板失败");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// </summary>
        [HttpGet("pending-validation")]
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDto>>), 200)]
        public async Task<IActionResult> GetPendingValidation()
        {
            try
            {
                var result = await _service.GetPendingValidationFormulasAsync();

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "获取待校验验方列表失败");
                }

                return Success(result.Data, $"查询成功，共{result.Data.Count}个待校验验方");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取待校验验方列表", null);
            }
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
            try
            {
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
            catch (Exception ex)
            {
                return HandleException(ex, "验证验方药材", new { formulaId, herbItemId, selectedHerbId });
            }
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复端点 ==========

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            try
            {
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
            catch (Exception ex)
            {
                return HandleException(ex, "切换验方状态", id);
            }
        }

        /// <summary>
        /// 恢复已删除的验方
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id)
        {
            try
            {
                // 使用统一的所有权检查方法
                var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _service.GetByIdAsync, "验方");
                if (ownershipError != null) return ownershipError;

                var result = await _service.RestoreAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "恢复失败");
                }

                LogOperation("恢复验方", null, id);
                return Success(result.Data, "验方已恢复");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "恢复验方", id);
            }
        }
    }
}
