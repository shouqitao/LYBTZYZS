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

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
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

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaListDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _sender.Send(new GetFormulasQuery(page, pageSize, keyword, category));
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return SuccessPaged(result.Value!, "查询成功");
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var result = await _sender.Send(new GetFormulaQuery(id));
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(result.Error ?? "验方不存在");
            }

            return Success(result.Value, "查询成功");
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] FormulaInputDto dto)
        {
            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new CreateFormulaCommand(dto, operatorId));
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "创建失败");
            }

            LogOperation("新增验方成功", result.Value, result.Value.Id);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                ApiResponse<FormulaDetailDto>.CreateSuccess(result.Value, "验方创建成功"));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto dto)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var getResult = await _sender.Send(new GetFormulaQuery(id));
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new UpdateFormulaCommand(id, dto, operatorId));
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "更新失败");
            }

            LogOperation("更新验方成功", result.Value, id);
            return Success(result.Value, "验方更新成功");
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var getResult = await _sender.Send(new GetFormulaQuery(id));
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new DeleteFormulaCommand(id, operatorId));
            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "验方不存在");
            }

            LogOperation("删除验方成功", null, id);
            return Success(true, "删除成功");
        }

        [HttpPost("batch-import")]
        [ProducesResponseType(typeof(ApiResponse<FormulaBatchImportResultDto>), 200)]
        public async Task<IActionResult> Import([FromBody] FormulaBatchImportInputDto request)
        {
            if (request == null || request.Formulas == null || !request.Formulas.Any())
            {
                return ValidationFail("导入数据不能为空");
            }

            var result = await _sender.Send(new BatchImportFormulasCommand(request.Formulas, request.FileName));

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "导入失败");
            }

            LogOperation("批量导入验方",
                new { FileName = request.FileName, TotalCount = result.Value.TotalCount, SuccessCount = result.Value.SuccessCount },
                null);

            return Success(result.Value, result.Value.Message);
        }

        [HttpGet("pending-validation")]
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> GetPendingValidation()
        {
            var result = await _sender.Send(new GetPendingValidationQuery());

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "查询失败");
            }

            return Success(result.Value, $"查询成功，共{result.Value.Count}个待校验验方");
        }

        [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ValidateHerb(
            Guid formulaId,
            Guid herbItemId,
            [FromBody] ValidateFormulaHerbInputDto request)
        {
            if (ValidateGuid(formulaId, "验方ID") is { } error1) return error1;
            if (ValidateGuid(herbItemId, "药材项ID") is { } error2) return error2;
            if (ValidateGuid(request.SelectedHerbId, "系统药材ID") is { } error3) return error3;

            var result = await _sender.Send(new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId));

            if (!result.IsSuccess)
            {
                return BusinessFail(result.Error ?? "验证失败");
            }

            LogOperation("验证验方药材",
                new { FormulaId = formulaId, HerbItemId = herbItemId, SelectedHerbId = request.SelectedHerbId },
                formulaId);

            return Success("药材验证成功");
        }

        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("ids 不能为空");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new BatchDeleteFormulasCommand(dto.Ids, operatorId));

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除验方", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
        }

        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            if (ValidateGuid(id, "验方ID") is { } error) return error;

            var getResult = await _sender.Send(new GetFormulaQuery(id));
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound("验方不存在");
            if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
                return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await _sender.Send(new ToggleFormulaStatusCommand(id, operatorId));
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "切换状态失败");
            }

            LogOperation("切换验方状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"验方已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }
    }
}


