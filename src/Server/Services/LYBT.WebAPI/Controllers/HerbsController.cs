using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材管理 API - 基础CRUD功能
    /// </summary>
    /// optimize-api-permissions: 药材管理需Doctor或Admin角色
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class HerbsController : BaseApiController
    {
        private readonly IHerbService _herbService;

        public HerbsController(
            IHerbService herbService,
            ILogger<HerbsController> logger)
            : base(logger)
        {
            _herbService = herbService;
        }

        /// <summary>
        /// 获取药材分页列表（Issue #1164: 扩展支持分类筛选）
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "HerbsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HerbListDto>>), 200)]
        public async Task<IActionResult> GetList(
            CancellationToken cancellationToken = default,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _herbService.GetPagedAsync(page, pageSize, keyword, category, cancellationToken);
            return Success(result.Data!, "查询成功");
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await _herbService.GetByIdAsync(id, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return NotFound(result.ErrorMessage ?? "药材不存在");
            }
            return Success(result.Data, "查询成功");
        }

        /// <summary>
        /// 创建新药材
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] HerbInputDto dto, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var result = await _herbService.CreateAsync(dto, cancellationToken);
            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("创建药材", result.Data, result.Data.Id);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Data.Id, version = "1" },
                    ApiResponse<HerbDetailDto>.CreateSuccess(result.Data, "药材创建成功"));
            }

            return HandleResult(result);
        }

        /// <summary>
        /// 更新药材信息
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, guid => _herbService.GetByIdAsync(guid, cancellationToken), "药材");
            if (ownershipError != null) return ownershipError;

            dto.Id = id;

            var result = await _herbService.UpdateAsync(id, dto, cancellationToken);
            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("更新药材信息", result.Data, id);
                return Success(result.Data, "药材信息更新成功");
            }

            return HandleResult(result);
        }

        /// <summary>
        /// 删除药材（软删除）
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, guid => _herbService.GetByIdAsync(guid, cancellationToken), "药材");
            if (ownershipError != null) return ownershipError;

            var result = await _herbService.DeleteAsync(id, cancellationToken);
            if (!result.IsSuccess)
            {
                // X7: 区分引用阻塞(422)和不存在(404)
                if (result.ErrorMessage?.Contains("处方引用") == true)
                    return HandleResult(result);
                return NotFound(result.ErrorMessage ?? "药材不存在");
            }

            LogOperation("删除药材", null, id);
            return Success(true, "删除成功");
        }

        // ========== Epic #1962: 批量导入/导出和引用检查端点 ==========

        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        [HttpPost("batch-import")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (request.Herbs == null || request.Herbs.Count == 0)
            {
                return ValidationFail("药材列表不能为空");
            }

            if (request.Herbs.Count > 10000)
            {
                return ValidationFail("批量导入最多支持10000条记录");
            }

            var result = await _herbService.BatchImportAsync(request.Herbs, request.Strategy, cancellationToken);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("批量导入药材（Epic #1962）",
                    new
                    {
                        TotalCount = result.Data.TotalCount,
                        SuccessCount = result.Data.SuccessCount,
                        FailureCount = result.Data.FailureCount,
                        SkippedCount = result.Data.SkippedCount,
                        Strategy = request.Strategy.ToString()
                    },
                    null);
            }

            return Success(result.Data!, $"批量导入完成: 成功{result.Data?.SuccessCount ?? 0}条");
        }

        /// <summary>
        /// 导出药材数据（返回JSON列表，Desktop层负责Excel生成）
        /// </summary>
        [HttpGet("export-all")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbDetailDto>>), 200)]
        public async Task<IActionResult> GetAllForExport([FromQuery] string? category = null, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var result = await _herbService.GetAllForExportAsync(category, cancellationToken);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("导出药材数据（Epic #1962）",
                    new { Category = category, Count = result.Data.Count },
                    null);
            }

            return Success(result.Data!, "导出数据查询成功");
        }

        /// <summary>
        /// 检查药材是否被处方引用
        /// </summary>
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<HerbReferenceCheckDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await _herbService.CheckReferenceAsync(id, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<HerbReferenceCheckDto>(result);
            }
            return Success(result.Data, "引用检查完成");
        }

        /// <summary>
        /// 批量检查药材引用关系
        /// </summary>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbReferenceCheckDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchCheckReference([FromBody] HerbBatchCheckReferenceInputDto request, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (request.HerbIds == null || request.HerbIds.Count == 0)
            {
                return ValidationFail("药材ID列表不能为空");
            }

            if (request.HerbIds.Count > 100)
            {
                return ValidationFail("批量检查最多支持100条记录");
            }

            var result = await _herbService.BatchCheckReferenceAsync(request.HerbIds, cancellationToken);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("批量检查药材引用（Epic #1962）",
                    new { Count = result.Data.Count },
                    null);
            }

            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<List<HerbReferenceCheckDto>>(result);
            }
            return Success(result.Data, "批量引用检查完成");
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复端点 ==========

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, guid => _herbService.GetByIdAsync(guid, cancellationToken), "药材");
            if (ownershipError != null) return ownershipError;

            var result = await _herbService.ToggleStatusAsync(id, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<HerbDetailDto>(result);
            }

            LogOperation("切换药材状态", new { NewStatus = result.Data.Status }, id);
            return Success(result.Data, $"药材已{(result.Data.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的药材
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            // 注: Restore不能使用GetEntityWithOwnershipCheckAsync，因为GetByIdAsync
            // 受全局软删除过滤器影响无法找到已删除记录。
            // RestoreAsync内部使用GetByIdIncludingDeletedAsync绕过过滤器。
            var result = await _herbService.RestoreAsync(id, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<HerbDetailDto>(result);
            }

            LogOperation("恢复药材", null, id);
            return Success(result.Data, "药材已恢复");
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量启用药材
        /// </summary>
        [HttpPost("batch-enable")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个药材");
            }

            var result = await _herbService.BatchUpdateStatusAsync(dto.Ids, CommonStatus.Enabled, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<BatchOperationResultDto>(result);
            }

            LogOperation("批量启用药材", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        /// <summary>
        /// 批量禁用药材
        /// </summary>
        [HttpPost("batch-disable")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个药材");
            }

            var result = await _herbService.BatchUpdateStatusAsync(dto.Ids, CommonStatus.Disabled, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<BatchOperationResultDto>(result);
            }

            LogOperation("批量禁用药材", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken cancellationToken = default)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个药材");
            }

            var result = await _herbService.BatchDeleteAsync(dto.Ids, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<BatchOperationResultDto>(result);
            }

            LogOperation("批量删除药材", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        // ========== Issue #1166 - 导出功能 ==========

        /// <summary>
        /// 导出药材数据
        /// US-HERB-010: 导出功能
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportHerbs([FromQuery] string? category = null, CancellationToken cancellationToken = default)
        {
            var stream = await _herbService.ExportAsync(category, cancellationToken);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "药材数据.xlsx");
        }

        /// <summary>
        /// 导出药材导入模板
        /// US-HERB-011: 导出模板
        /// </summary>
        [HttpGet("import-template")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public IActionResult ExportTemplate()
        {
            var stream = _herbService.GenerateImportTemplate();
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "药材导入模板.xlsx");
        }
    }
}
