using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Module.Registrations.Application.Commands;
using LYBT.Module.Registrations.Controllers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 挂号管理 API - 继承 BaseRegistrationsController 提供标准方法
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
public class RegistrationsController : BaseRegistrationsController
{
    public RegistrationsController(
        ISender sender,
        ILogger<RegistrationsController> logger)
        : base(sender, logger)
    {
    }

    /// <summary>
    /// 创建挂号（QuickVisit 两步第 1 步——2026-08-13 恢复：a99619f47 误删，真机 405 证实）
    /// </summary>
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    [HttpPost]
    [EnableRateLimiting("ApiCalls")]
    public async Task<IActionResult> Create([FromBody] RegistrationInputDto input, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new CreateRegistrationCommand(input, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建挂号失败");

        LogOperation("创建挂号", input, result.Value.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id, version = ApiVersionConstants.V1 },
            ApiResponse<RegistrationDetailDto>.CreateSuccess(result.Value, "挂号创建成功"));
    }

    /// <summary>
    /// 接诊：从等待队列选中患者（开始看诊——步骤 2）
    /// </summary>
    /// <param name="id">挂号 ID</param>
    /// <param name="ct">取消令牌</param>
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.DoctorOnly)]
    public override async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await Sender.Send(new StartVisitCommand(id), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "接诊失败");

        LogOperation("接诊", null, id);
        return Success(result.Value, "接诊成功");
    }

    /// <summary>
    /// 取消挂号 — 仅 Receptionist（04-permissions P0-3 / REG-BR-002）
    /// </summary>
    [Authorize(Policy = PolicyConstants.ReceptionistOnly)]
    [EnableRateLimiting("ApiCalls")]
    public override async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await Sender.Send(new CancelRegistrationCommand(id), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "取消挂号失败");

        LogOperation("取消挂号", null, id);
        return Success("挂号已取消");
    }
}
