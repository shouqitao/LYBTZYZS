using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formulas.Application.Commands;
using LYBT.Module.Formulas.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 验方管理 API - CRUD、验证、批量操作（权限与LocalWebAPI对齐：DoctorOrReceptionist）
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public class FormulasController : BaseApiController
    {
        private readonly ISender _sender;

        public FormulasController(
            ISender sender,
            ILogger<FormulasController> logger)
            : base(logger)
        {
            _sender = sender;
        }

        /// <summary>
        /// 获取验方分页列表（按所有权过滤）
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "FormulasCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaListDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _sender.Send(new GetFormulasQuery(page, pageSize, keyword, category), ct);
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return SuccessPaged(result.Value!, "查询成功");
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var result = await _sender.Send(new GetFormulaQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(result.Error ?? "验方不存在");
            }

            // Ownership check: Doctor can only see own + shared
            var (operatorId, _, operatorRole) = GetOperator();
            if (operatorRole == UserRole.Doctor && result.Value.CreatedBy != operatorId && !result.Value.IsShared)
                return Forbid("无权限查看此验方");

            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 新增验方
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] FormulaInputDto dto, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new CreateFormulaCommand(dto, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "创建失败");
            }

            LogOperation("新增验方成功", result.Value, result.Value.Id);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                ApiResponse<FormulaDetailDto>.CreateSuccess(result.Value, "验方创建成功"));
        }

        /// <summary>
        /// 更新验方信息
        /// </summary>
        [HttpPut("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var getResult = await _sender.Send(new GetFormulaQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new UpdateFormulaCommand(id, dto, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "更新失败");
            }

            LogOperation("更新验方成功", result.Value, id);
            return Success(result.Value, "验方更新成功");
        }

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var getResult = await _sender.Send(new GetFormulaQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new DeleteFormulaCommand(id, operatorId), ct);
            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "验方不存在");
            }

            LogOperation("删除验方成功", null, id);
            return Success(true, "删除成功");
        }

        /// <summary>
        /// 恢复已删除的验方
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var (operatorId, _, _) = GetOperator();

            var result = await _sender.Send(new RestoreFormulaCommand(id, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "恢复失败");
            }

            LogOperation("恢复验方", result.Value, result.Value.Id);
            return Success(result.Value, "验方恢复成功");
        }

        /// <summary>
        /// 批量导入验方
        /// </summary>
        [HttpPost("batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaBatchImportResultDto>), 200)]
        public async Task<IActionResult> Import([FromBody] FormulaBatchImportInputDto request, CancellationToken ct)
        {
            if (request == null || request.Formulas == null || !request.Formulas.Any())
            {
                return ValidationFail("导入数据不能为空");
            }

            var result = await _sender.Send(new BatchImportFormulasCommand(request.Formulas, request.FileName), ct);

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "导入失败");
            }

            LogOperation("批量导入验方",
                new { FileName = request.FileName, TotalCount = result.Value.TotalCount, SuccessCount = result.Value.SuccessCount },
                null);

            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 获取待校验验方列表
        /// </summary>
        [HttpGet("pending-validation")]
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> GetPendingValidation(CancellationToken ct)
        {
            var result = await _sender.Send(new GetPendingValidationQuery(), ct);

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "查询失败");
            }

            return Success(result.Value, $"查询成功，共{result.Value.Count}个待校验验方");
        }

        /// <summary>
        /// 校验验方药材匹配
        /// </summary>
        [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ValidateHerb(
            Guid formulaId,
            Guid herbItemId,
            [FromBody] ValidateFormulaHerbInputDto request,
            CancellationToken ct)
        {
            if (ValidateGuid(formulaId, "验方ID") is { } error1) return error1;
            if (ValidateGuid(herbItemId, "药材项ID") is { } error2) return error2;
            if (ValidateGuid(request.SelectedHerbId, "系统药材ID") is { } error3) return error3;

            var result = await _sender.Send(new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId), ct);

            if (!result.IsSuccess)
            {
                return BusinessFail(result.Error ?? "验证失败");
            }

            LogOperation("验证验方药材",
                new { FormulaId = formulaId, HerbItemId = herbItemId, SelectedHerbId = request.SelectedHerbId },
                formulaId);

            return Success("药材验证成功");
        }

        /// <summary>
        /// 批量删除验方
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个验方");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new BatchDeleteFormulasCommand(dto.Ids, operatorId), ct);

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除验方", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 切换验方启用/禁用状态
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var getResult = await _sender.Send(new GetFormulaQuery(id), ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new ToggleFormulaStatusCommand(id, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "切换状态失败");
            }

            LogOperation("切换验方状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"验方已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }
    }
}


