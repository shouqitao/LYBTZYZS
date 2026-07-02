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
[Authorize]
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
        [FromQuery] string? category = null)
    {
        var result = await _sender.Send(new GetHerbsQuery(page, pageSize, keyword, category));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, "查询成功");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _sender.Send(new GetHerbQuery(id));
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "药材不存在");
        return Success(result.Value, "查询成功");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HerbInputDto dto)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new CreateHerbCommand(dto, operatorId));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建失败");
        return Success(result.Value, "创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new UpdateHerbCommand(id, dto, operatorId));
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "更新失败");
        }
        return Success(result.Value, "更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new DeleteHerbCommand(id, operatorId));
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "药材不存在");
        return Success(true, "删除成功");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new BatchDeleteHerbsCommand(request.Ids, operatorId));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");
        return Success(result.Value, result.Value.Message);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new ToggleHerbStatusCommand(id, operatorId));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");
        return Success(result.Value, $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
    }

    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request)
    {
        if (request == null || request.Herbs == null || request.Herbs.Count == 0)
            return ValidationFail("导入列表不能为空");
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导入失败");
        return Success(result.Value, result.Value.Message);
    }
}
