using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Application.Commands;
using LYBT.Module.Registration.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RegistrationsController : BaseApiController
{
    private readonly ISender _sender;

    public RegistrationsController(ISender sender, ILogger<RegistrationsController> logger)
        : base(logger)
    {
        _sender = sender;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? doctorId = null)
    {
        var result = await _sender.Send(new GetRegistrationsQuery(page, pageSize, keyword,
            startDate, endDate, patientId, doctorId));
        return SuccessPaged(result, "查询成功");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _sender.Send(new GetRegistrationQuery(id));
        if (result == null)
            return NotFound("挂号不存在");
        return Success(result, "查询成功");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegistrationInputDto dto)
    {
        var result = await _sender.Send(new CreateRegistrationCommand(dto));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "创建挂号失败");
        return Success(result.Value, "挂号创建成功");
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null)
    {
        var result = await _sender.Send(new GetWaitingQueueQuery(doctorId));
        return Success(result, "查询成功");
    }

    [HttpPut("{id}/start-visit")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
    public async Task<IActionResult> StartVisit(Guid id)
    {
        var result = await _sender.Send(new StartVisitCommand(id));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "接诊失败");
        return Success(result.Value, "开始就诊");
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _sender.Send(new CancelRegistrationCommand(id));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "取消挂号失败");
        return Success("挂号取消成功");
    }

    [HttpPost("quick-visit")]
    public async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto request)
    {
        if (request == null || request.PatientId == Guid.Empty)
            return ValidationFail("患者信息不能为空");

        var (doctorId, doctorName, _) = GetOperator();
        var result = await _sender.Send(new QuickVisitCommand(request, doctorId, doctorName));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "快速看诊失败");
        return Success(result.Value, "快速看诊创建成功");
    }
}
