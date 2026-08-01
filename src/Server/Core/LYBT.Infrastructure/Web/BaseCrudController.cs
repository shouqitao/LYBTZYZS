using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 通用 CRUD Controller 基类
/// 提供统一的分页查询、详情、创建、更新、删除、切换状态、恢复、批量删除等方法
/// 子类直接 override 需要的 action 方法，通过 Sender 发送 MediatR 命令
/// </summary>
public abstract class BaseCrudController : BaseApiController
{
    private readonly ISender _sender;

    protected BaseCrudController(ISender sender, ILogger logger)
        : base(logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    protected ISender Sender => _sender;

    /// <summary>
    /// 获取分页列表 — 子类按需 override
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var query = CreateGetListQuery(page, pageSize, keyword);
        var result = await _sender.Send(query, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, "查询成功");
    }

    /// <summary>
    /// 根据 ID 获取详情 — 子类应 override 此方法
    /// </summary>
    [HttpGet("{id:guid}")]
    public virtual Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => throw new NotSupportedException("此资源不支持 GetById 操作");

    /// <summary>
    /// 创建资源 — 子类按需 override
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] object dto, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var command = CreateCreateCommand(dto, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建失败");
        LogOperation("创建成功", result.Value, null);
        return Success(result.Value, "创建成功");
    }

    /// <summary>
    /// 更新资源 — 子类按需 override
    /// </summary>
    [HttpPut("{id:guid}")]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] object dto, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateUpdateCommand(id, dto, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "更新失败");
        }
        LogOperation("更新成功", result.Value, id);
        return Success(result.Value, "更新成功");
    }

    /// <summary>
    /// 删除资源（软删除）— 子类按需 override
    /// </summary>
    [HttpDelete("{id:guid}")]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateDeleteCommand(id, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "删除失败");
        }
        LogOperation("删除成功", null, id);
        return Success("删除成功");
    }

    /// <summary>
    /// 切换状态（启用/禁用）— 默认不支持，子类按需 override
    /// </summary>
    [HttpPost("{id:guid}/toggle-status")]
    public virtual Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        => throw new NotSupportedException("此资源不支持切换状态操作");

    /// <summary>
    /// 恢复已删除的资源 — 默认不支持，子类按需 override
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public virtual Task<IActionResult> Restore(Guid id, CancellationToken ct)
        => throw new NotSupportedException("此资源不支持恢复操作");

    /// <summary>
    /// 批量删除 — 子类按需 override
    /// </summary>
    [HttpPost("batch-delete")]
    public virtual async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail("请至少选择一个资源");

        var (operatorId, _, _) = GetOperator();
        var command = CreateBatchDeleteCommand(dto.Ids, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");
        LogOperation("批量删除", new { Ids = dto.Ids }, null);
        return Success(result.Value, "批量删除成功");
    }

    #region 工厂方法 - 子类按需 override（默认 throw）

    /// <summary>
    /// 创建分页查询请求 — 子类 override GetList 时无需实现此方法
    /// </summary>
    protected virtual IRequest<Result<PagedResult<object>>> CreateGetListQuery(int page, int pageSize, string? keyword)
        => throw new NotSupportedException("请 override GetList 方法或实现 CreateGetListQuery");

    /// <summary>
    /// 创建创建命令 — 子类 override Create 时无需实现此方法
    /// </summary>
    protected virtual IRequest<Result<object>> CreateCreateCommand(object dto, Guid operatorId)
        => throw new NotSupportedException("请 override Create 方法或实现 CreateCreateCommand");

    /// <summary>
    /// 创建更新命令 — 子类 override Update 时无需实现此方法
    /// </summary>
    protected virtual IRequest<Result<object>> CreateUpdateCommand(Guid id, object dto, Guid operatorId)
        => throw new NotSupportedException("请 override Update 方法或实现 CreateUpdateCommand");

    /// <summary>
    /// 创建删除命令 — 子类 override Delete 时无需实现此方法
    /// </summary>
    protected virtual IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId)
        => throw new NotSupportedException("请 override Delete 方法或实现 CreateDeleteCommand");

    /// <summary>
    /// 创建批量删除命令 — 子类 override BatchDelete 时无需实现此方法
    /// </summary>
    protected virtual IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
        => throw new NotSupportedException("请 override BatchDelete 方法或实现 CreateBatchDeleteCommand");

    #endregion
}
