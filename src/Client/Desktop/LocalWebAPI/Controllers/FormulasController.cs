using System.Security.Claims;
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

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class FormulasController : BaseApiController
{
    private readonly ISender _sender;

    public FormulasController(
        ISender sender,
        ILogger<FormulasController> logger) : base(logger)
    {
        _sender = sender;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;
        var result = await _sender.Send(new GetFormulasQuery(page, pageSize, keyword, category), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, "查询成功");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetFormulaQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "验方不存在");

        // Ownership check: Doctor can only see own + shared
        var operatorId = GetCurrentUserId();
        var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
        if (Enum.TryParse<UserRole>(roleStr, true, out var operatorRole) &&
            operatorRole == UserRole.Doctor &&
            result.Value.CreatedBy != operatorId &&
            !result.Value.IsShared)
        {
            return Forbid("您没有权限查看此验方");
        }

        return Success(result.Value, "查询成功");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FormulaInputDto dto, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new CreateFormulaCommand(dto, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建失败");
        return Success(result.Value, "创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto dto, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new UpdateFormulaCommand(id, dto, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "更新失败");
        }
        return Success(result.Value, "更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        // Local模式为单用户桌面端，跳过ValidateOwnership检查（与Remote端的区别）
        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new DeleteFormulaCommand(id, operatorId), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "验方不存在");
        return Success(true, "删除成功");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request, CancellationToken ct)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new BatchDeleteFormulasCommand(request.Ids, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");
        return Success(result.Value, result.Value.Message);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        // Local模式为单用户桌面端，跳过ValidateOwnership检查（与Remote端的区别）
        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new ToggleFormulaStatusCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");
        return Success(result.Value, $"验方已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new RestoreFormulaCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "恢复失败");
        return Success(result.Value, "恢复成功");
    }

    [HttpPost("{id}/clone")]
    public async Task<IActionResult> Clone(Guid id, CancellationToken ct)
    {
        var source = await _sender.Send(new GetFormulaQuery(id), ct);
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

        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new CreateFormulaCommand(clone, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "复制失败");
        return Success(result.Value, "复制成功");
    }

    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] List<FormulaImportItemDto> formulas, CancellationToken ct)
    {
        if (formulas == null || formulas.Count == 0)
            return ValidationFail("导入列表不能为空");
        var result = await _sender.Send(new BatchImportFormulasCommand(formulas, null), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导入失败");
        return Success(result.Value, result.Value.Message);
    }

    [HttpGet("pending-validation")]
    public async Task<IActionResult> GetPendingValidation(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingValidationQuery(), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value, $"查询成功，共{result.Value.Count}个待校验验方");
    }

    [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
    public async Task<IActionResult> ValidateHerb(Guid formulaId, Guid herbItemId, [FromBody] ValidateFormulaHerbInputDto request, CancellationToken ct)
    {
        var result = await _sender.Send(new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "药材验证失败");
        return Success("药材验证成功");
    }

    [HttpPost("batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");

        var result = await _sender.Send(new BatchEnableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量启用失败");

        return Success(result.Value, result.Value.Message);
    }

    [HttpPost("batch-disable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("验方ID列表不能为空");

        var result = await _sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");

        return Success(result.Value, result.Value.Message);
    }
}
