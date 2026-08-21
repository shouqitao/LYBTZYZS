using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 通用 CRUD Controller 基类
/// 提供统一的路由声明和批量操作辅助方法。
/// 子类必须 override 所有需要的 action 方法。
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
    /// 获取分页列表 — 子类必须 override
    /// </summary>
    [HttpGet]
    public virtual Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] CommonStatus? status = null,
        CancellationToken ct = default)
        => throw new NotSupportedException("请 override GetList 方法");

    /// <summary>
    /// 根据 ID 获取详情 — 子类必须 override
    /// </summary>
    [HttpGet("{id:guid}")]
    public virtual Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => throw new NotSupportedException("此资源不支持 GetById 操作");

    /// <summary>
    /// 删除资源（软删除）— 子类必须 override
    /// </summary>
    [HttpDelete("{id:guid}")]
    public virtual Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => throw new NotSupportedException("请 override Delete 方法");

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
    public virtual Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        => throw new NotSupportedException("请 override BatchDelete 方法");

    #region 批量操作模板 - 子类 action 方法体委托到此，消除复制粘贴

    /// <summary>
    /// 批量删除模板执行器。子类 BatchDelete action 委托到此方法，传入命令工厂与文案。
    /// </summary>
    protected async Task<IActionResult> ExecuteBatchDeleteAsync(
        BatchDeleteInputDto dto,
        Func<List<Guid>, Guid, IRequest<Result<BatchOperationResultDto>>> createCommand,
        string emptyMessage,
        string operationName,
        CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return ValidationFail(emptyMessage);

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(createCommand(dto.Ids, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");

        LogOperation(operationName, new { Ids = dto.Ids, Result = result.Value.Message }, null);
        return Success(result.Value, result.Value.Message);
    }

    /// <summary>
    /// 批量引用检查模板执行器。子类 BatchCheckReference action 委托到此方法。
    /// </summary>
    protected async Task<IActionResult> ExecuteBatchCheckReferenceAsync<T>(
        List<Guid> ids,
        Func<List<Guid>, IRequest<Result<List<T>>>> createQuery,
        string emptyMessage,
        string maxMessage,
        string errorMessage,
        CancellationToken ct)
    {
        if (ids == null || ids.Count == 0)
            return ValidationFail(emptyMessage);
        if (ids.Count > BatchOptions.DefaultMaxBatchSize)
            return ValidationFail(maxMessage);

        var result = await Sender.Send(createQuery(ids), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? errorMessage);

        return Success(result.Value, "批量引用检查完成");
    }

    #endregion
}
