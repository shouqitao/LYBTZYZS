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

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 验方管理 API - LocalWebAPI 简化版
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class FormulasController : BaseCrudController
{
    public FormulasController(
        ISender sender,
        ILogger<FormulasController> logger) : base(sender, logger)
    {
    }

    /// <summary>
    /// 获取验方详情
    /// </summary>
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetFormulaQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "验方不存在");

        var (operatorId, _, operatorRole) = GetOperator();
        if (operatorRole == UserRole.Doctor && result.Value.CreatedBy != operatorId && !result.Value.IsShared)
        {
            return Forbid("您没有权限查看此验方");
        }

        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 复制验方
    /// </summary>
    [HttpPost("{id}/clone")]
    public async Task<IActionResult> Clone(Guid id, CancellationToken ct)
    {
        var source = await Sender.Send(new GetFormulaQuery(id), ct);
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
    /// 批量启用药方
    /// </summary>
    [HttpPost("batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
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
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");

        var result = await Sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");

        return Success(result.Value, result.Value.Message);
    }
}
