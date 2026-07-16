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

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class HerbsController : BaseApiController
{
    private readonly ISender _sender;

    public HerbsController(ISender sender, ILogger<HerbsController> logger) : base(logger)
    {
        _sender = sender;
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
        var result = await _sender.Send(new GetHerbsQuery(page, pageSize, keyword, category), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, "查询成功");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetHerbQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "药材不存在");
        return Success(result.Value, "查询成功");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HerbInputDto dto, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new CreateHerbCommand(dto, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建失败");
        return Success(result.Value, "创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new UpdateHerbCommand(id, dto, operatorId), ct);
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
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new DeleteHerbCommand(id, operatorId), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "药材不存在");
        return Success(true, "删除成功");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request, CancellationToken ct)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new BatchDeleteHerbsCommand(request.Ids, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");
        return Success(result.Value, result.Value.Message);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new ToggleHerbStatusCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");
        return Success(result.Value, $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new RestoreHerbCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "恢复失败");
        return Success(result.Value, "恢复成功");
    }

    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)
    {
        if (request == null || request.Herbs == null || request.Herbs.Count == 0)
            return ValidationFail("导入列表不能为空");
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导入失败");
        return Success(result.Value, result.Value.Message);
    }

    [HttpGet("{id}/check-reference")]
    public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new CheckHerbReferenceQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "药材不存在");
        return Success(result.Value, "引用检查完成");
    }

    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference([FromBody] HerbBatchCheckReferenceInputDto dto, CancellationToken ct)
    {
        if (dto?.HerbIds == null || dto.HerbIds.Count == 0)
            return ValidationFail("药材ID列表不能为空");
        if (dto.HerbIds.Count > 100)
            return ValidationFail("单次最多检查100条药材");

        var result = await _sender.Send(new BatchCheckHerbReferenceQuery(dto.HerbIds), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量引用检查失败");
        return Success(result.Value, "批量引用检查完成");
    }
}
