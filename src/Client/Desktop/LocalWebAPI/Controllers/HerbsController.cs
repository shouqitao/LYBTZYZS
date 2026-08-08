using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Application.Commands;
using LYBT.Module.Herbs.Application.Queries;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 药材管理 API - LocalWebAPI 简化版
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class HerbsController : BaseCrudController
{
    private readonly IHerbService _herbService;

    public HerbsController(ISender sender, ILogger<HerbsController> logger, IHerbService herbService)
        : base(sender, logger)
    {
        _herbService = herbService;
    }

    /// <summary>
    /// 获取药材分页列表
    /// </summary>
    [HttpGet]
    public override async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var result = await _herbService.GetPagedAsync(page, pageSize, keyword, ct);
        if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 获取药材详情
    /// </summary>
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "药材ID") is { } error) return error;

        var result = await _herbService.GetByIdAsync(id, ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "药材不存在");
        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 创建新药材
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HerbInputDto input, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new CreateHerbCommand(input, operatorId), ct);
        if (result.IsSuccess && result.Value != null)
        {
            LogOperation("创建药材", result.Value, null);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Value.Id },
                ApiResponse<HerbDetailDto>.CreateSuccess(result.Value, "药材创建成功"));
        }

        return BusinessFail(result.Error ?? "创建失败");
    }

    /// <summary>
    /// 更新药材信息
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto input, CancellationToken ct)
    {
        if (ValidateGuid(id, "药材ID") is { } error) return error;

        var getResult = await _herbService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound(getResult.Error ?? "药材不存在");

        if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            return ownerError;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new UpdateHerbCommand(id, input, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "更新失败");

        LogOperation("更新药材", result.Value, result.Value.Id);
        return Success(result.Value, "药材更新成功");
    }

    /// <summary>
    /// 删除药材
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpDelete("{id}")]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "药材ID") is { } error) return error;

        var getResult = await _herbService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound(getResult.Error ?? "药材不存在");

        if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            return ownerError;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new DeleteHerbCommand(id, operatorId), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "删除失败");

        LogOperation("删除药材", new { Id = id }, id);
        return Success<object?>(null, "药材删除成功");
    }

    /// <summary>
    /// 切换药材状态
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("{id}/toggle-status")]
    public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "药材ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();

        var getResult = await _herbService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound(getResult.Error ?? "药材不存在");

        if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
            return ownerError;

        var result = await Sender.Send(new ToggleHerbStatusCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");

        LogOperation("切换药材状态", new { NewStatus = result.Value.Status }, id);
        return Success(result.Value, $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 恢复已删除的药材
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("{id}/restore")]
    public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "药材ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new RestoreHerbCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "恢复失败");

        LogOperation("恢复药材", result.Value, result.Value.Id);
        return Success(result.Value, "药材恢复成功");
    }

    /// <summary>
    /// 批量删除药材
    /// </summary>
    [HttpPost("batch-delete")]
    public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        => await ExecuteBatchDeleteAsync(
            dto,
            (ids, operatorId) => new BatchDeleteHerbsCommand(ids, operatorId),
            "请至少选择一个药材",
            "批量删除药材",
            ct);

    /// <summary>
    /// 批量导入药材
    /// </summary>
    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)
    {
        if (request == null || request.Herbs == null || request.Herbs.Count == 0)
            return ValidationFail("导入列表不能为空");
        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导入失败");
        return Success(result.Value, result.Value.Message);
    }

    /// <summary>
    /// 检查药材引用关系
    /// </summary>
    [HttpGet("{id}/check-reference")]
    public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CheckHerbReferenceQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "药材不存在");
        return Success(result.Value, "引用检查完成");
    }

    /// <summary>
    /// 批量检查引用关系
    /// </summary>
    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference([FromBody] HerbBatchCheckReferenceInputDto dto, CancellationToken ct)
        => await ExecuteBatchCheckReferenceAsync(
            dto.HerbIds,
            ids => new BatchCheckHerbReferenceQuery(ids),
            "药材ID列表不能为空",
            "单次最多检查100条药材",
            "批量引用检查失败",
            ct);

    /// <summary>
    /// 批量启用药材
    /// </summary>
    [HttpPost("batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("药材ID列表不能为空");

        var result = await Sender.Send(new BatchEnableHerbsCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量启用失败");

        return Success(result.Value, result.Value.Message);
    }

    /// <summary>
    /// 批量禁用药材
    /// </summary>
    [HttpPost("batch-disable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("药材ID列表不能为空");

        var result = await Sender.Send(new BatchDisableHerbsCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");

        return Success(result.Value, result.Value.Message);
    }
}
