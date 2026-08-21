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
namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 验方目录 API（P1-24 自 CatalogController 拆出，绝对路由 /api/v1/formulas/*）。
/// </summary>
[ApiController]
[Route("api/v1/formulas")]
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
    [HttpGet("/api/v1/formulas")]
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
    /// 下载验方导入 JSON 模板（2026-08-13：Excel→JSON——后端不涉及 Excel 格式，保持通用性）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpGet("/api/v1/formulas/import-template")]
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
    [HttpGet("/api/v1/formulas/export")]
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
            ct: ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "导出失败");

        // JSON 数组（含 Herbs 明细）
        return Success(result.Value!, "验方导出（JSON）");
    }

    /// <summary>
    /// 获取验方详情
    /// </summary>
    [HttpGet("/api/v1/formulas/{id}")]
    public async Task<IActionResult> GetFormulaById(Guid id, CancellationToken ct)
    {
        var result = await _formulaService.GetByIdAsync(id, ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "验方不存在");

        var (operatorId, _, operatorRole) = GetOperator();
        if (
            operatorRole == UserRole.Doctor
            && result.Value.CreatedBy != operatorId
            && !result.Value.IsShared
        )
            return Forbid("您没有权限查看此验方");

        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 新增验方
    /// </summary>
    [HttpPost("/api/v1/formulas")]
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
            new { id = result.Value.Id },
            ApiResponse<FormulaDetailDto>.CreateSuccess(result.Value, "验方创建成功")
        );
    }

    /// <summary>
    /// 更新验方信息
    /// </summary>
    [HttpPut("/api/v1/formulas/{id}")]
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
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.ErrorCode == ErrorCode.Forbidden)
                return Forbid(result.Error ?? "无权更新该验方");
            return BusinessFail(result.Error ?? "更新失败");
        }

        LogOperation("更新验方成功", result.Value, id);
        return Success(result.Value, "验方更新成功");
    }

    /// <summary>
    /// 删除验方（软删除）
    /// </summary>
    [HttpDelete("/api/v1/formulas/{id}")]
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
        {
            if (result.ErrorCode == ErrorCode.Forbidden)
                return Forbid(result.Error ?? "无权删除该验方");
            return NotFound(result.Error ?? "验方不存在");
        }

        LogOperation("删除验方成功", null, id);
        return Success(true, "删除成功");
    }

    /// <summary>
    /// 切换验方启用/禁用状态
    /// </summary>
    [HttpPost("/api/v1/formulas/{id}/toggle-status")]
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
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.ErrorCode == ErrorCode.Forbidden)
                return Forbid(result.Error ?? "无权切换该验方状态");
            return BusinessFail(result.Error ?? "切换状态失败");
        }

        LogOperation("切换验方状态", new { NewStatus = result.Value.Status }, id);
        return Success(
            result.Value,
            $"验方已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}"
        );
    }

    /// <summary>
    /// 批量删除验方
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("/api/v1/formulas/batch-delete")]
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
    /// 复制验方
    /// </summary>
    [HttpPost("/api/v1/formulas/{id}/clone")]
    public async Task<IActionResult> CloneFormula(Guid id, CancellationToken ct)
    {
        var source = await _formulaService.GetByIdAsync(id, ct);
        if (!source.IsSuccess || source.Value == null)
            return NotFound(source.Error ?? "验方不存在");

        var clone = new FormulaInputDto
        {
            Name = $"{source.Value.Name} (副本)",
            Effect = source.Value.Effect ?? string.Empty,
            Usage = source.Value.Usage ?? string.Empty,
            Property = source.Value.Property,
            Category = source.Value.Category,
            IsShared = false,
            Remark = source.Value.Remark,
            Herbs =
                source
                    .Value.Herbs?.Select(h => new FormulaHerbItemInputDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage,
                        Unit = h.Unit,
                        Usage = h.Usage,
                        DecocteMethod = h.DecocteMethod,
                    })
                    .ToList()
                ?? new(),
        };

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(
            new CreateEntityCommand<FormulaInputDto, FormulaDetailDto>(clone, operatorId),
            ct
        );
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "复制失败");
        return Success(result.Value, "复制成功");
    }

    /// <summary>
    /// 批量导入验方
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("/api/v1/formulas/batch-import")]
    public async Task<IActionResult> BatchImportFormulas(
        [FromBody] List<FormulaImportItemDto> formulas,
        CancellationToken ct
    )
    {
        if (formulas == null || formulas.Count == 0)
            return ValidationFail("导入列表不能为空");
        var result = await Sender.Send(new BatchImportFormulasCommand(formulas, null), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导入失败");
        return Success(result.Value, result.Value.Message);
    }

    /// <summary>
    /// 获取待校验验方列表
    /// </summary>
    [HttpGet("/api/v1/formulas/pending-validation")]
    public async Task<IActionResult> GetPendingValidation(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    )
    {
        var result = await Sender.Send(new GetPendingValidationQuery(page, pageSize), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, $"查询成功，共{result.Value.TotalCount}个待校验验方");
    }

    /// <summary>
    /// 校验验方药材匹配
    /// </summary>
    [HttpPost("/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate")]
    public async Task<IActionResult> ValidateHerb(
        Guid formulaId,
        Guid herbItemId,
        [FromBody] ValidateFormulaHerbInputDto request,
        CancellationToken ct
    )
    {
        var result = await Sender.Send(
            new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId),
            ct
        );
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "药材验证失败");
        return Success("药材验证成功");
    }

    /// <summary>
    /// 恢复已删除的验方 — 仅 Admin（业务管理）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminBusinessOnly)]
    [HttpPost("/api/v1/formulas/{id}/restore")]
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

        return Success(result.Value, "验方恢复成功");
    }

    /// <summary>
    /// 批量启用药方
    /// </summary>
    [HttpPost("/api/v1/formulas/batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnableFormulas(
        [FromBody] BatchDeleteInputDto dto,
        CancellationToken ct
    )
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");

        var result = await Sender.Send(new BatchEnableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量启用失败");

        return Success(result.Value, result.Value.Message);
    }

    /// <summary>
    /// 批量禁用药方
    /// </summary>
    [HttpPost("/api/v1/formulas/batch-disable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchDisableFormulas(
        [FromBody] BatchDeleteInputDto dto,
        CancellationToken ct
    )
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");

        var result = await Sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");

        return Success(result.Value, result.Value.Message);
    }

    #endregion
}
