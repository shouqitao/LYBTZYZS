using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HerbsController : BaseApiController
{
    private readonly IHerbService _herbService;

    public HerbsController(IHerbService herbService, ILogger<HerbsController> logger) : base(logger)
    {
        _herbService = herbService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null)
    {
        var result = await _herbService.SearchAsync(keyword ?? "", default);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _herbService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HerbInputDto dto)
    {
        var result = await _herbService.CreateAsync(dto);
        return HandleResult(result, "创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto)
    {
        var result = await _herbService.UpdateAsync(id, dto);
        return HandleResult(result, "更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _herbService.DeleteAsync(id);
        return HandleResult(result, "删除成功");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _herbService.BatchDeleteAsync(request.Ids);
        return HandleResult(result);
    }



    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _herbService.ToggleStatusAsync(id);
        return HandleResult(result);
    }

    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request)
    {
        if (request == null || request.Herbs == null || request.Herbs.Count == 0)
            return ValidationFail("导入列表不能为空");
        var result = await _herbService.BatchImportAsync(request.Herbs, request.Strategy);
        return HandleResult(result);
    }


}
