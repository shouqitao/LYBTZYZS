using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Application.Commands;
using LYBT.Module.Herbs.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材管理 API - 基础CRUD功能
    /// </summary>
    /// optimize-api-permissions: 药材管理权限与LocalWebAPI对齐（DoctorOrReceptionist）
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public class HerbsController : BaseApiController
    {
        private readonly ISender _sender;

        public HerbsController(
            ISender sender,
            ILogger<HerbsController> logger)
            : base(logger)
        {
            _sender = sender;
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
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _sender.Send(new GetHerbsQuery(page, pageSize, keyword, category), cancellationToken);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await _sender.Send(new GetHerbQuery(id), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(result.Error ?? "药材不存在");
            }
            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 创建新药材
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] HerbInputDto dto, CancellationToken cancellationToken = default)
        {
            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new CreateHerbCommand(dto, operatorId), cancellationToken);
            if (result.IsSuccess && result.Value != null)
            {
                LogOperation("创建药材", result.Value, result.Value.Id);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                    ApiResponse<HerbDetailDto>.CreateSuccess(result.Value, "药材创建成功"));
            }

            return BusinessFail(result.Error ?? "创建失败");
        }

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken cancellationToken = default)
        {
            var (operatorId, _, _) = GetOperator();

            var getResult = await _sender.Send(new GetHerbQuery(id), cancellationToken);
            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return NotFound(getResult.Error ?? "药材不存在");
            }

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            {
                return ownerError;
            }

            var result = await _sender.Send(new ToggleHerbStatusCommand(id, operatorId), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "切换状态失败");
            }

            LogOperation("切换药材状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 批量删除药材
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个药材");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new BatchDeleteHerbsCommand(dto.Ids, operatorId), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除药材", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 更新药材信息
        /// </summary>
        [HttpPut("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var (operatorId, _, _) = GetOperator();

            var getResult = await _sender.Send(new GetHerbQuery(id), cancellationToken);
            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return NotFound(getResult.Error ?? "药材不存在");
            }

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            {
                return ownerError;
            }

            var result = await _sender.Send(new UpdateHerbCommand(id, dto, operatorId), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "更新失败");
            }

            LogOperation("更新药材", result.Value, result.Value.Id);
            return Success(result.Value, "药材更新成功");
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        [HttpDelete("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var (operatorId, _, _) = GetOperator();

            var getResult = await _sender.Send(new GetHerbQuery(id), cancellationToken);
            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return NotFound(getResult.Error ?? "药材不存在");
            }

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            {
                return ownerError;
            }

            var result = await _sender.Send(new DeleteHerbCommand(id, operatorId), cancellationToken);
            if (!result.IsSuccess)
            {
                return BusinessFail(result.Error ?? "删除失败");
            }

            LogOperation("删除药材", new { Id = id }, id);
            return Success<object?>(null, "药材删除成功");
        }

        /// <summary>
        /// 恢复已删除的药材
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var (operatorId, _, _) = GetOperator();

            var result = await _sender.Send(new RestoreHerbCommand(id, operatorId), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "恢复失败");
            }

            LogOperation("恢复药材", result.Value, result.Value.Id);
            return Success(result.Value, "药材恢复成功");
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken cancellationToken = default)
        {
            if (request?.Herbs == null || request.Herbs.Count == 0)
            {
                return ValidationFail("导入列表不能为空");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "导入失败");
            }

            LogOperation("批量导入药材", new { Count = request.Herbs.Count, Strategy = request.Strategy }, null);
            return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条药材");
        }

        /// <summary>
        /// 检查药材引用关系（删除前检查）
        /// </summary>
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<HerbReferenceCheckDto>), 200)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken cancellationToken = default)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await _sender.Send(new CheckHerbReferenceQuery(id), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "药材不存在");
            return Success(result.Value, "引用检查完成");
        }

        /// <summary>
        /// 批量检查药材引用关系
        /// </summary>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbReferenceCheckDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchCheckReference(
            [FromBody] HerbBatchCheckReferenceInputDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto?.HerbIds == null || dto.HerbIds.Count == 0)
                return ValidationFail("药材ID列表不能为空");
            if (dto.HerbIds.Count > 100)
                return ValidationFail("单次最多检查100条药材");

            var result = await _sender.Send(new BatchCheckHerbReferenceQuery(dto.HerbIds), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量引用检查失败");
            return Success(result.Value, "批量引用检查完成");
        }

        /// <summary>
        /// 批量启用药材
        /// </summary>
        [HttpPost("batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchEnable(
            [FromBody] BatchDeleteInputDto dto, CancellationToken cancellationToken = default)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await _sender.Send(new BatchEnableHerbsCommand(dto.Ids), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量启用失败");

            LogOperation("批量启用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 批量禁用药材
        /// </summary>
        [HttpPost("batch-disable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchDisable(
            [FromBody] BatchDeleteInputDto dto, CancellationToken cancellationToken = default)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await _sender.Send(new BatchDisableHerbsCommand(dto.Ids), cancellationToken);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");

            LogOperation("批量禁用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }
    }
}


