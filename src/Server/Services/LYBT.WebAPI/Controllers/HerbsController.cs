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
    /// 药材管理 API - 继承 BaseCrudController 提供标准 CRUD
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public class HerbsController : BaseCrudController<HerbListDto, HerbDetailDto, HerbInputDto, GetHerbsQuery>
    {
        public HerbsController(ISender sender, ILogger<HerbsController> logger)
            : base(sender, logger)
        {
        }

        /// <summary>
        /// 获取药材分页列表（添加 OutputCache 和分类筛选）
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "HerbsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HerbListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            // 注意：GetHerbsQuery 需要 category 参数，这里先传 null
            // 子类可以 override 这个方法来添加 category 参数
            var result = await Sender.Send(new GetHerbsQuery(page, pageSize, keyword), ct);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 获取药材详情（添加 Ownership 检查）
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await Sender.Send(new GetHerbQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(result.Error ?? "药材不存在");
            }

            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 创建新药材（添加 OutputCache 和 RateLimiting）
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), StatusCodes.Status201Created)]
        public override async Task<IActionResult> Create([FromBody] HerbInputDto dto, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new CreateHerbCommand(dto, operatorId), ct);
            if (result.IsSuccess && result.Value != null)
            {
                LogOperation("创建药材", result.Value, null);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                    ApiResponse<HerbDetailDto>.CreateSuccess(result.Value, "药材创建成功"));
            }

            return BusinessFail(result.Error ?? "创建失败");
        }

        /// <summary>
        /// 更新药材信息（添加 Ownership 检查）
        /// </summary>
        [HttpPut("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var getResult = await Sender.Send(new GetHerbQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return NotFound(getResult.Error ?? "药材不存在");
            }

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            {
                return ownerError;
            }

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new UpdateHerbCommand(id, dto, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "更新失败");
            }

            LogOperation("更新药材", result.Value, result.Value.Id);
            return Success(result.Value, "药材更新成功");
        }

        /// <summary>
        /// 删除药材（添加 Ownership 检查）
        /// </summary>
        [HttpDelete("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var getResult = await Sender.Send(new GetHerbQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return NotFound(getResult.Error ?? "药材不存在");
            }

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            {
                return ownerError;
            }

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new DeleteHerbCommand(id, operatorId), ct);
            if (!result.IsSuccess)
            {
                return BusinessFail(result.Error ?? "删除失败");
            }

            LogOperation("删除药材", new { Id = id }, id);
            return Success<object?>(null, "药材删除成功");
        }

        /// <summary>
        /// 切换药材状态（启用/禁用，添加 Ownership 检查）
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();

            var getResult = await Sender.Send(new GetHerbQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
            {
                return NotFound(getResult.Error ?? "药材不存在");
            }

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            {
                return ownerError;
            }

            var result = await Sender.Send(new ToggleHerbStatusCommand(id, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "切换状态失败");
            }

            LogOperation("切换药材状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的药材
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var (operatorId, _, _) = GetOperator();

            var result = await Sender.Send(new RestoreHerbCommand(id, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "恢复失败");
            }

            LogOperation("恢复药材", result.Value, result.Value.Id);
            return Success(result.Value, "药材恢复成功");
        }

        /// <summary>
        /// 批量删除药材（添加 OutputCache 和 RateLimiting）
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个药材");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new BatchDeleteHerbsCommand(dto.Ids, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除药材", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)
        {
            if (request?.Herbs == null || request.Herbs.Count == 0)
            {
                return ValidationFail("导入列表不能为空");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId), ct);
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
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await Sender.Send(new CheckHerbReferenceQuery(id), ct);
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
            CancellationToken ct)
        {
            if (dto?.HerbIds == null || dto.HerbIds.Count == 0)
                return ValidationFail("药材ID列表不能为空");
            if (dto.HerbIds.Count > 100)
                return ValidationFail("单次最多检查100条药材");

            var result = await Sender.Send(new BatchCheckHerbReferenceQuery(dto.HerbIds), ct);
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
            [FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await Sender.Send(new BatchEnableHerbsCommand(dto.Ids), ct);
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
            [FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await Sender.Send(new BatchDisableHerbsCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");

            LogOperation("批量禁用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        #region 基类抽象方法实现
        protected override GetHerbsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
            => new GetHerbsQuery(page, pageSize, keyword);

        protected override IRequest<Result<HerbDetailDto>> CreateCreateCommand(HerbInputDto dto, Guid operatorId)
            => new CreateHerbCommand(dto, operatorId);

        protected override IRequest<Result<HerbDetailDto>> CreateUpdateCommand(Guid id, HerbInputDto dto, Guid operatorId)
            => new UpdateHerbCommand(id, dto, operatorId);

        protected override IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId)
            => new DeleteHerbCommand(id, operatorId);

        protected override IRequest<Result<HerbDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId)
            => new ToggleHerbStatusCommand(id, operatorId);

        protected override IRequest<Result<HerbDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId)
            => new RestoreHerbCommand(id, operatorId);

        protected override IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
            => new BatchDeleteHerbsCommand(ids, operatorId);
        #endregion
    }
}
