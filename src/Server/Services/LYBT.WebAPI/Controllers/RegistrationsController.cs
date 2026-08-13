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
    /// 医生快速看诊
    /// </summary>
    [HttpPut("{id:guid}/start-visit")]
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
    /// 取消挂号
    /// </summary>
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    [HttpPut("{id:guid}/cancel")]
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
