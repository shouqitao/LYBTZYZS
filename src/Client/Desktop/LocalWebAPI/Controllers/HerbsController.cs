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

/// <summary>
/// 药材管理 API - 继承 BaseCrudController 提供标准 CRUD（简化版）
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
public class HerbsController : BaseCrudController<HerbListDto, HerbDetailDto, HerbInputDto, GetHerbsQuery>
{
    public HerbsController(ISender sender, ILogger<HerbsController> logger)
        : base(sender, logger)
    {
    }

    /// <summary>
    /// 获取药材详情
    /// </summary>
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "药材ID") is { } error) return error;

        var result = await Sender.Send(new GetHerbQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "药材不存在");
        return Success(result.Value, "查询成功");
    }

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
    {
        if (dto?.HerbIds == null || dto.HerbIds.Count == 0)
            return ValidationFail("药材ID列表不能为空");
        if (dto.HerbIds.Count > 100)
            return ValidationFail("单次最多检查100条药材");

        var result = await Sender.Send(new BatchCheckHerbReferenceQuery(dto.HerbIds), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量引用检查失败");
        return Success(result.Value, "批量引用检查完成");
    }

    /// <summary>
    /// 批量启用药材
    /// </summary>
    [HttpPost("batch-enable")]
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto?.Ids == null || dto.Ids.Count == 0)
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
        if (dto?.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("药材ID列表不能为空");

        var result = await Sender.Send(new BatchDisableHerbsCommand(dto.Ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量禁用失败");

        return Success(result.Value, result.Value.Message);
    }

    #region 基类抽象方法实现
    protected override GetHerbsQuery CreateGetListQuery(int page, int pageSize, string? keyword)
        => new GetHerbsQuery(page, pageSize, keyword);

    protected override IRequest<Result<HerbDetailDto>> CreateCreateCommand(HerbInputDto dto, Guid operatorId)
        => new CreateHerbCommand(dto, operatorId);

    protected override IRequest<Result<HerbDetailDto>> CreateUpdateCommand(Guid id, HerbInputDto dto, Guid operatorId)
        => new UpdateHerbCommand(id, dto, operatorId);

    protected override IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId)
        => new DeleteHerbCommand(id, operatorId);

    

    protected override IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
        => new BatchDeleteHerbsCommand(ids, operatorId);
    #endregion
}
