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
                    new { id = result.Data.Id, version = ApiVersionConstants.V1 },
                    ApiResponse<HerbDetailDto>.CreateSuccess(result.Data, "药材创建成功"));
            }

            return HandleResult(result);
        }
        /// <summary>
        /// 切换药材状态（启用/禁用）
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

        /// <summary>
        /// 更新药材信息
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, guid => _herbService.GetByIdAsync(guid, cancellationToken), "药材");
            if (ownershipError != null) return ownershipError;

            var result = await _herbService.UpdateAsync(id, dto, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<HerbDetailDto>(result);
            }

            LogOperation("更新药材", result.Data, result.Data.Id);
            return Success(result.Data, "药材更新成功");
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, guid => _herbService.GetByIdAsync(guid, cancellationToken), "药材");
            if (ownershipError != null) return ownershipError;

            var result = await _herbService.DeleteAsync(id, cancellationToken);
            if (!result.IsSuccess)
            {
                return HandleResult(result);
            }

            LogOperation("删除药材", new { Id = id }, id);
            return Success<object?>(null, "药材删除成功");
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("batch-import")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken cancellationToken = default)
        {
            if (request?.Herbs == null || request.Herbs.Count == 0)
            {
                return ValidationFail("导入列表不能为空");
            }

            var result = await _herbService.BatchImportAsync(request.Herbs, request.Strategy, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult<HerbBatchImportResultDto>(result);
            }

            LogOperation("批量导入药材", new { Count = request.Herbs.Count, Strategy = request.Strategy }, null);
            return Success(result.Data, $"成功导入 {result.Data.SuccessCount} 条药材");
        }
    }
}
