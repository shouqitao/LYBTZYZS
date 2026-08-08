using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formulas.Application.Commands;
using LYBT.Module.Formulas.Application.Queries;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 验方管理 API - LocalWebAPI 简化版
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class FormulasController : BaseCrudController
{
    private readonly IFormulaService _formulaService;

    public FormulasController(
        ISender sender,
        ILogger<FormulasController> logger,
        IFormulaService formulaService) : base(sender, logger)
    {
        _formulaService = formulaService;
    }

    /// <summary>
    /// 获取验方分页列表
    /// </summary>
    [HttpGet]
    public override async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var result = await _formulaService.GetPagedAsync(page, pageSize, keyword, ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return SuccessPaged(result.Value!, "查询成功");
    }

    /// <summary>
    /// 获取验方详情
    /// </summary>
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _formulaService.GetByIdAsync(id, ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "验方不存在");

        var (operatorId, _, operatorRole) = GetOperator();
        if (operatorRole == UserRole.Doctor && result.Value.CreatedBy != operatorId && !result.Value.IsShared)
            return Forbid("您没有权限查看此验方");

        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 新增验方
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FormulaInputDto input, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new CreateFormulaCommand(input, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return BusinessFail(result.Error ?? "创建失败");
        }

        LogOperation("新增验方成功", result.Value, null);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id },
            ApiResponse<FormulaDetailDto>.CreateSuccess(result.Value, "验方创建成功"));
    }

    /// <summary>
    /// 更新验方信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto input, CancellationToken ct)
    {
        if (ValidateGuid(id, "验方ID") is { } error) return error;

        var getResult = await _formulaService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound("验方不存在");
        if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
            return ownershipError;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new UpdateFormulaCommand(id, input, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "更新失败");

        LogOperation("更新验方成功", result.Value, id);
        return Success(result.Value, "验方更新成功");
    }

    /// <summary>
    /// 删除验方（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "验方ID") is { } error) return error;

        var getResult = await _formulaService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound("验方不存在");
        if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
            return ownershipError;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new DeleteFormulaCommand(id, operatorId), ct);
        if (!result.IsSuccess)
        {
            return NotFound(result.Error ?? "验方不存在");
        }

        LogOperation("删除验方成功", null, id);
        return Success(true, "删除成功");
    }

    /// <summary>
    /// 切换验方启用/禁用状态
    /// </summary>
    [HttpPost("{id}/toggle-status")]
    public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "验方ID") is { } error) return error;

        var getResult = await _formulaService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound("验方不存在");
        if (ValidateOwnership(getResult.Value.CreatedBy, "验方") is { } ownershipError)
            return ownershipError;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new ToggleFormulaStatusCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");

        LogOperation("切换验方状态", new { NewStatus = result.Value.Status }, id);
        return Success(result.Value, $"验方已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 批量删除验方
    /// </summary>
    [HttpPost("batch-delete")]
    public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        => await ExecuteBatchDeleteAsync(
            dto,
            (ids, operatorId) => new BatchDeleteFormulasCommand(ids, operatorId),
            "请至少选择一个验方",
            "批量删除验方",
            ct);

    /// <summary>
    /// 复制验方
    /// </summary>
    [HttpPost("{id}/clone")]
    public async Task<IActionResult> Clone(Guid id, CancellationToken ct)
    {
        var source = await _formulaService.GetByIdAsync(id, ct);
        if (!source.IsSuccess || source.Value == null)
            return NotFound(source.Error ?? "验方不存在");

        var clone = new FormulaInputDto
        {
            Name = $"{source.Value.Name} (副本)",
            Effect = source.Value.Effect ?? string.Empty,
            Description = source.Value.Description,
            Usage = source.Value.Usage ?? string.Empty,
            Property = source.Value.Property,
            Category = source.Value.Category,
            IsShared = false,
            Remark = source.Value.Remark,
            Herbs = source.Value.Herbs?.Select(h => new FormulaHerbItemInputDto
            {
                HerbId = h.HerbId,
                HerbName = h.HerbName,
                Dosage = h.Dosage,
                Unit = h.Unit,
                Usage = h.Usage,
                DecocteMethod = h.DecocteMethod
            }).ToList() ?? new()
        };

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new CreateFormulaCommand(clone, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "复制失败");
        return Success(result.Value, "复制成功");
    }

    /// <summary>
    /// 批量导入验方
    /// </summary>
    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] List<FormulaImportItemDto> formulas, CancellationToken ct)
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
    [HttpGet("pending-validation")]
    public async Task<IActionResult> GetPendingValidation(CancellationToken ct)
    {
        var result = await Sender.Send(new GetPendingValidationQuery(), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value, $"查询成功，共{result.Value.Count}个待校验验方");
    }

    /// <summary>
    /// 校验验方药材匹配
    /// </summary>
    [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
    public async Task<IActionResult> ValidateHerb(Guid formulaId, Guid herbItemId, [FromBody] ValidateFormulaHerbInputDto request, CancellationToken ct)
    {
        var result = await Sender.Send(new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "药材验证失败");
        return Success("药材验证成功");
    }

    /// <summary>
    /// 恢复已删除的验方 — 仅 Admin（业务管理）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminBusinessOnly)]
    [HttpPost("{id}/restore")]
    public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "验方ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new RestoreFormulaCommand(id, operatorId), ct);
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
    [HttpPost("batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
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
    [HttpPost("batch-disable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");

        var result = await Sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");

        return Success(result.Value, result.Value.Message);
    }
}
