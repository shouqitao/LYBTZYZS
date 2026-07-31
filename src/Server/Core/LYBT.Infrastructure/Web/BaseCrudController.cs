using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 通用 CRUD Controller 基类
/// 提供统一的分页查询、详情、创建、更新、删除、切换状态、恢复、批量删除等方法
/// </summary>
/// <typeparam name="TListDto">列表 DTO 类型</typeparam>
/// <typeparam name="TDetailDto">详情 DTO 类型</typeparam>
/// <typeparam name="TInputDto">输入 DTO 类型</typeparam>
/// <typeparam name="TQuery">查询请求类型（必须实现 IRequest&lt;Result&lt;PagedResult&lt;TListDto&gt;&gt;&gt;）</typeparam>
public abstract class BaseCrudController<TListDto, TDetailDto, TInputDto, TQuery> 
    : BaseApiController
    where TQuery : IRequest<Result<PagedResult<TListDto>>>
{
    private readonly ISender _sender;

    protected BaseCrudController(ISender sender, ILogger logger)
        : base(logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    protected ISender Sender => _sender;

    /// <summary>
    /// 获取分页列表
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
    /// 根据 ID 获取详情（子类应 override 此方法）
    /// </summary>
    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        // 子类应 override 此方法以提供具体的查询逻辑
        return NotFound("未实现 GetById 方法");
    }

    /// <summary>
    /// 创建资源
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TInputDto dto, CancellationToken ct)
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
    /// 更新资源
    /// </summary>
    [HttpPut("{id:guid}")]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] TInputDto dto, CancellationToken ct)
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
    /// 删除资源（软删除）
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
    /// 切换状态（启用/禁用）
    /// </summary>
    [HttpPost("{id:guid}/toggle-status")]
    public virtual async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateToggleStatusCommand(id, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");
        LogOperation("切换状态", null, id);
        return Success(result.Value, "状态已切换");
    }

    /// <summary>
    /// 恢复已删除的资源
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public virtual async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "ID") is { } error) return error;

        var (operatorId, _, _) = GetOperator();
        var command = CreateRestoreCommand(id, operatorId);
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("未被删除") == true)
                return BusinessFail(result.Error);
            return NotFound(result.Error ?? "资源不存在");
        }
        LogOperation("恢复成功", result.Value, id);
        return Success(result.Value, "恢复成功");
    }

    /// <summary>
    /// 批量删除
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

    #region 抽象方法 - 子类必须实现

    /// <summary>
    /// 创建分页查询请求
    /// </summary>
    protected abstract TQuery CreateGetListQuery(int page, int pageSize, string? keyword);

    /// <summary>
    /// 创建创建命令
    /// </summary>
    protected abstract IRequest<Result<TDetailDto>> CreateCreateCommand(TInputDto dto, Guid operatorId);

    /// <summary>
    /// 创建更新命令
    /// </summary>
    protected abstract IRequest<Result<TDetailDto>> CreateUpdateCommand(Guid id, TInputDto dto, Guid operatorId);

    /// <summary>
    /// 创建删除命令
    /// </summary>
    protected abstract IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId);

    /// <summary>
    /// 创建切换状态命令
    /// </summary>
    protected abstract IRequest<Result<TDetailDto>> CreateToggleStatusCommand(Guid id, Guid operatorId);

    /// <summary>
    /// 创建恢复命令
    /// </summary>
    protected abstract IRequest<Result<TDetailDto>> CreateRestoreCommand(Guid id, Guid operatorId);

    /// <summary>
    /// 创建批量删除命令
    /// </summary>
    protected abstract IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId);

    #endregion
}
