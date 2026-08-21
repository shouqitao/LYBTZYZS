using Asp.Versioning;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Module.Catalog.Application.Queries;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 验方目录 API（P1-24 自 CatalogController 拆出，绝对路由 /api/v1/formulas/*）。
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/formulas")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class FormulasController : BaseCrudController
{
    private readonly ICatalogQueryService<FormulaListDto, FormulaDetailDto> _formulaService;

    public FormulasController(
        ISender sender,
        ILogger<FormulasController> logger,
        ICatalogQueryService<FormulaListDto, FormulaDetailDto> formulaService
    )
        : base(sender, logger)
    {
        _formulaService = formulaService;
    }

        #region 验方端点（原 FormulasController，绝对路由 /api/v1/formulas/*）

        /// <summary>
        /// 获取验方分页列表
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/formulas")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaListDto>>), 200)]
        public async Task<IActionResult> GetFormulaList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default
        )
        {
            if (ValidatePagination(page, pageSize) is { } error)
                return error;

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _formulaService.GetPagedAsync(
                page,
                pageSize,
                keyword,
                operatorId,
                isAdmin,
                ct
            );
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return SuccessPaged(result.Value!, "查询成功");
        }

        /// <summary>
        /// 下载验方导入模板（T4 P0#4: 此前端点缺失桌面调用 404）
        /// </summary>
        /// <summary>
        /// 下载验方导入 JSON 模板（2026-08-13：Excel→JSON——后端不涉及 Excel 格式，保持通用性）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpGet("/api/v{version:apiVersion}/formulas/import-template")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult FormulaImportTemplate()
        {
            var template = new
            {
                Description = "验方批量导入 JSON 模板（与 POST /formulas/batch-import 期望的 DTO 一致）",
                Fields = new[]
                {
                    new
                    {
                        Field = "Name",
                        Required = true,
                        Description = "验方名称",
                    },
                    new
                    {
                        Field = "Category",
                        Required = false,
                        Description = "分类",
                    },
                    new
                    {
                        Field = "Effect",
                        Required = false,
                        Description = "功效",
                    },
                    new
                    {
                        Field = "Usage",
                        Required = false,
                        Description = "用法",
                    },
                    new
                    {
                        Field = "Herbs",
                        Required = true,
                        Description = "药材组成（对象数组 [{HerbName, Dosage, Unit}]，如 [{\"HerbName\":\"人参\",\"Dosage\":10,\"Unit\":\"g\"}]）",
                    },
                },
                Example = new[]
                {
                    new
                    {
                        Name = "四君子汤",
                        Category = "补益剂",
                        Effect = "益气健脾",
                        Usage = "水煎服",
                        Herbs = new[]
                        {
                            new { HerbName = "人参", Dosage = 10, Unit = "g" },
                            new { HerbName = "白术", Dosage = 10, Unit = "g" },
                        },
                    },
                },
            };
            return Success(template, "验方导入模板（JSON）");
        }

        /// <summary>
        /// 导出验方为 JSON 数组（含药材组成明细，2026-08-13：Excel→JSON；P2：按分类筛选，对齐客户端 category）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpGet("/api/v{version:apiVersion}/formulas/export")]
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> FormulaExport(
            [FromQuery] string? category = null,
            CancellationToken ct = default
        )
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _formulaService.ExportDetailsAsync(
                keyword: null,
                category: category,
                operatorId: operatorId,
                isAdmin: isAdmin,
                ct: ct
            );
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "导出失败");

            // JSON 数组（含 Herbs 明细，满足 US-FORM-013）
            return Success(result.Value!, "验方导出（JSON）");
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/formulas/{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> GetFormulaById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            var result = await _formulaService.GetByIdAsync(id, ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "验方不存在");

            var (operatorId, _, operatorRole) = GetOperator();
            if (
                operatorRole == UserRole.Doctor
                && result.Value.CreatedBy != operatorId
                && !result.Value.IsShared
            )
                return Forbid("无权限查看此验方");

            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 新增验方
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateFormula(
            [FromBody] FormulaInputDto input,
            CancellationToken ct
        )
        {
            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(
                new CreateEntityCommand<FormulaInputDto, FormulaDetailDto>(input, operatorId),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "创建失败");
            }

            LogOperation("新增验方成功", result.Value, null);
            return CreatedAtAction(
                nameof(GetFormulaById),
                new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                ApiResponse<FormulaDetailDto>.CreateSuccess(result.Value, "验方创建成功")
            );
        }

        /// <summary>
        /// 更新验方信息
        /// </summary>
        [HttpPut("/api/v{version:apiVersion}/formulas/{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> UpdateFormula(
            Guid id,
            [FromBody] FormulaInputDto input,
            CancellationToken ct
        )
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            // P1-8（2026-08-14）: 存在性+所有权检查移入 CommandHandler——Controller 仅编排
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new UpdateEntityCommand<FormulaInputDto, FormulaDetailDto>(id, input, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess)
                return HandleResult(result, useAuthMapping: true);

            LogOperation("更新验方成功", result.Value, id);
            return Success(result.Value!, "验方更新成功");
        }

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        [HttpDelete("/api/v{version:apiVersion}/formulas/{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> DeleteFormula(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            // P1-8（2026-08-14）: 存在性+所有权检查移入 CommandHandler
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new DeleteEntityCommand<Formula>(id, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess)
                return HandleResult(result);

            LogOperation("删除验方成功", null, id);
            return Success(true, "删除成功");
        }

        /// <summary>
        /// 切换验方启用/禁用状态
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleFormulaStatus(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            // P1-8（2026-08-14）: 存在性+所有权检查移入 CommandHandler
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new ToggleEntityStatusCommand<Formula, FormulaDetailDto>(id, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess)
                return HandleResult(result, useAuthMapping: true);

            LogOperation("切换验方状态", new { NewStatus = result.Value!.Status }, id);
            return Success(
                result.Value!,
                $"验方已{(result.Value!.Status == CommonStatus.Enabled ? "启用" : "禁用")}"
            );
        }

        /// <summary>
        /// 恢复已删除的验方 — 仅 Admin（业务管理）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminBusinessOnly)]
        [HttpPost("/api/v{version:apiVersion}/formulas/{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> RestoreFormula(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(
                new RestoreEntityCommand<Formula, FormulaDetailDto>(id, operatorId),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.Error?.Contains("未被删除") == true)
                    return BusinessFail(result.Error);
                return NotFound(result.Error ?? "验方不存在");
            }

            LogOperation("恢复验方", result.Value, result.Value.Id);
            return Success(result.Value, "验方恢复成功");
        }

        /// <summary>
        /// 批量删除验方
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDeleteFormulas(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct
        ) =>
            await ExecuteBatchDeleteAsync(
                dto,
                (ids, operatorId) => new BatchDeleteFormulasCommand(ids, operatorId),
                "请至少选择一个验方",
                "批量删除验方",
                ct
            );

        /// <summary>
        /// 批量导入验方（JSON）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaBatchImportResultDto>), 200)]
        public async Task<IActionResult> ImportFormulas(
            [FromBody] FormulaBatchImportInputDto request,
            CancellationToken ct
        )
        {
            if (request == null || request.Formulas == null || !request.Formulas.Any())
            {
                return ValidationFail("导入数据不能为空");
            }

            var result = await Sender.Send(
                new BatchImportFormulasCommand(request.Formulas, request.FileName),
                ct
            );

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "导入失败");
            }

            LogOperation(
                "批量导入验方",
                new
                {
                    FileName = request.FileName,
                    TotalCount = result.Value.TotalCount,
                    SuccessCount = result.Value.SuccessCount,
                },
                null
            );

            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 获取待校验验方列表
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/formulas/pending-validation")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> GetPendingValidation(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default
        )
        {
            var result = await Sender.Send(new GetPendingValidationQuery(page, pageSize), ct);

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "查询失败");
            }

            return SuccessPaged(result.Value, $"查询成功，共{result.Value.TotalCount}个待校验验方");
        }

        /// <summary>
        /// 校验验方药材匹配
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/{formulaId}/herbs/{herbItemId}/validate")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ValidateHerb(
            Guid formulaId,
            Guid herbItemId,
            [FromBody] ValidateFormulaHerbInputDto request,
            CancellationToken ct
        )
        {
            if (ValidateGuid(formulaId, "验方ID") is { } error1)
                return error1;
            if (ValidateGuid(herbItemId, "药材项ID") is { } error2)
                return error2;
            if (ValidateGuid(request.SelectedHerbId, "系统药材ID") is { } error3)
                return error3;

            var result = await Sender.Send(
                new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId),
                ct
            );

            if (!result.IsSuccess)
            {
                return BusinessFail(result.Error ?? "验证失败");
            }

            LogOperation(
                "验证验方药材",
                new
                {
                    FormulaId = formulaId,
                    HerbItemId = herbItemId,
                    SelectedHerbId = request.SelectedHerbId,
                },
                formulaId
            );

            return Success("药材验证成功");
        }

        /// <summary>
        /// 批量启用药方
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchEnableFormulas(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct = default
        )
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("验方ID列表不能为空");

            var result = await Sender.Send(new BatchEnableFormulasCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量启用失败");

            LogOperation("批量启用药方", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 批量禁用药方
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-disable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchDisableFormulas(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct = default
        )
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("验方ID列表不能为空");

            var result = await Sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");

            LogOperation("批量禁用药方", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        #endregion
}
