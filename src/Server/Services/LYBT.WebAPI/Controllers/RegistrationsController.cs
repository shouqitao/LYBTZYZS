using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Application.Commands;
using LYBT.Module.Registration.Controllers;
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
    /// 医生快速看诊 (添加 OutputCache 和 RateLimiting)
    /// </summary>
    [HttpPost("quick-visit")]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public override async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)
    {
        var (doctorId, doctorName, _) = GetOperator();

        var result = await Sender.Send(new QuickVisitCommand(dto, doctorId, doctorName), ct);
        if (!result.IsSuccess || result.Value is null)
        {
            return BusinessFail(result.Error ?? "快速看诊失败");
        }

        LogOperation("医生快速看诊", dto, result.Value.RegistrationId);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.RegistrationId, version = ApiVersionConstants.V1 },
            ApiResponse<QuickVisitResultDto>.CreateSuccess(result.Value, "快速看诊创建成功"));
    }

    /// <summary>
    /// 创建挂号记录 (添加 OutputCache 和 RateLimiting)
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("ApiCalls")]
    public override async Task<IActionResult> Create([FromBody] RegistrationInputDto dto, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateRegistrationCommand(dto), ct);

        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建挂号失败");

        LogOperation("创建挂号", dto, result.Value.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id, version = ApiVersionConstants.V1 },
            ApiResponse<RegistrationDetailDto>.CreateSuccess(result.Value, "挂号创建成功"));
    }

    /// <summary>
    /// 接诊: 从队列选中患者 (添加 OutputCache 和 RateLimiting)
    /// </summary>
    [HttpPut("{id:guid}/start-visit")]
    [EnableRateLimiting("ApiCalls")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
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
    /// 取消挂号 (添加 OutputCache 和 RateLimiting)
    /// </summary>
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
