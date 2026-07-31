using MediatR;
using LYBT.Infrastructure.Constants;
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
/// 提供 GetList、GetById、Create、GetQueue、StartVisit、Cancel、QuickVisit 等方法
/// </summary>
public abstract class BaseRegistrationsController : BaseApiController
{
    private readonly ISender _sender;

    protected BaseRegistrationsController(ISender sender, ILogger logger)
        : base(logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    protected ISender Sender => _sender;

    /// <summary>
    /// 分页查询挂号记录
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> GetList(
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

        var result = await _sender.Send(new GetRegistrationsQuery(page, pageSize, keyword,
            startDate, endDate, patientId, doctorId), ct);

        return SuccessPaged(result, "查询成功");
    }

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await _sender.Send(new GetRegistrationQuery(id), ct);
        if (result == null)
        {
            return NotFound("挂号不存在");
        }

        return Success(result, "查询成功");
    }

    /// <summary>
    /// 创建挂号记录
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] RegistrationInputDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateRegistrationCommand(dto), ct);

        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建挂号失败");

        LogOperation("创建挂号", dto, result.Value.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id, version = ApiVersionConstants.V1 },
            result.Value);
    }

    /// <summary>
    /// 获取等待队列
    /// </summary>
    [HttpGet("queue")]
    public virtual async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetWaitingQueueQuery(doctorId), ct);
        return Success(result, "查询成功");
    }

    /// <summary>
    /// 接诊: 从队列选中患者
    /// </summary>
    [HttpPut("{id:guid}/start-visit")]
    public virtual async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await _sender.Send(new StartVisitCommand(id), ct);
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

        var result = await _sender.Send(new CancelRegistrationCommand(id), ct);
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

        var result = await _sender.Send(new QuickVisitCommand(dto, doctorId, doctorName), ct);
        if (!result.IsSuccess || result.Value is null)
        {
            return BusinessFail(result.Error ?? "快速看诊失败");
        }

        LogOperation("医生快速看诊", dto, result.Value.RegistrationId);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.RegistrationId, version = ApiVersionConstants.V1 },
            result.Value);
    }
}
