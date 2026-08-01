using MediatR;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Application.Commands;
using LYBT.Module.Registration.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Registration.Controllers;

/// <summary>
/// 挂号管理 Controller 共享基类
/// 继承 BaseCrudController，挂号模块只使用 Create/GetById/GetList，其他操作不支持
/// </summary>
public abstract class BaseRegistrationsController : BaseCrudController
{
    protected BaseRegistrationsController(ISender sender, ILogger logger)
        : base(sender, logger)
    {
    }

    #region Override 基类方法

    /// <summary>
    /// 获取挂号分页列表（保留原始 7 参数过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? doctorId = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var result = await Sender.Send(new GetRegistrationsQuery(page, pageSize, keyword,
            startDate, endDate, patientId, doctorId), ct);

        return SuccessPaged(result, "查询成功");
    }

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await Sender.Send(new GetRegistrationQuery(id), ct);
        if (result == null)
        {
            return NotFound("挂号不存在");
        }

        return Success(result, "查询成功");
    }

    #endregion

    #region Override 不支持的操作（挂号不支持 Update/Delete/BatchDelete）

    [HttpPut("{id:guid}")]
    public override Task<IActionResult> Update(Guid id, [FromBody] object dto, CancellationToken ct)
        => Task.FromResult<IActionResult>(NotFound("挂号不支持更新操作"));

    [HttpDelete("{id:guid}")]
    public override Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Task.FromResult<IActionResult>(NotFound("挂号不支持删除操作"));

    [HttpPost("batch-delete")]
    public override Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        => Task.FromResult<IActionResult>(NotFound("挂号不支持批量删除操作"));

    #endregion

    #region 挂号特化方法

    /// <summary>
    /// 获取等待队列
    /// </summary>
    [HttpGet("queue")]
    public virtual async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null, CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetWaitingQueueQuery(doctorId), ct);
        return Success(result, "查询成功");
    }

    /// <summary>
    /// 接诊: 从队列选中患者
    /// </summary>
    [HttpPut("{id:guid}/start-visit")]
    public virtual async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await Sender.Send(new StartVisitCommand(id), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "接诊失败");

        LogOperation("接诊", null, id);
        return Success(result.Value, "接诊成功");
    }

    /// <summary>
    /// 取消挂号
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    public virtual async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await Sender.Send(new CancelRegistrationCommand(id), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "取消挂号失败");

        LogOperation("取消挂号", null, id);
        return Success("挂号已取消");
    }

    /// <summary>
    /// 医生快速看诊
    /// </summary>
    [HttpPost("quick-visit")]
    public virtual async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)
    {
        var (doctorId, doctorName, _) = GetOperator();

        var result = await Sender.Send(new QuickVisitCommand(dto, doctorId, doctorName), ct);
        if (!result.IsSuccess || result.Value is null)
        {
            return BusinessFail(result.Error ?? "快速看诊失败");
        }

        LogOperation("医生快速看诊", dto, result.Value.RegistrationId);
        return Success(result.Value, "快速看诊创建成功");
    }

    #endregion
}
