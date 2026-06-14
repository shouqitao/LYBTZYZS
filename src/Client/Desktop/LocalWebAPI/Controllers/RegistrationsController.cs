using System.Security.Claims;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegistrationsController : BaseApiController
{
    private readonly IRegistrationService _registrationService;

    public RegistrationsController(IRegistrationService registrationService, ILogger<RegistrationsController> logger)
        : base(logger)
    {
        _registrationService = registrationService;
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
        var result = await _registrationService.GetPagedAsync(page, pageSize, keyword, startDate, endDate, patientId, doctorId);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _registrationService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegistrationInputDto dto)
    {
        var result = await _registrationService.CreateAsync(dto);
        return HandleResult(result, "挂号创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RegistrationInputDto dto)
    {
        var result = await _registrationService.CreateAsync(dto);
        return HandleResult(result, "挂号更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _registrationService.CancelAsync(id);
        return HandleResult(result, "挂号取消成功");
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null)
    {
        var result = await _registrationService.GetWaitingQueueAsync(doctorId);
        return HandleResult(result);
    }

    [HttpPut("{id}/start-visit")]
    public async Task<IActionResult> StartVisit(Guid id)
    {
        var result = await _registrationService.StartVisitAsync(id);
        return HandleResult(result, "开始就诊");
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _registrationService.CancelAsync(id);
        return HandleResult(result, "挂号取消成功");
    }

    [HttpPost("quick-visit")]
    public async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto request)
    {
        if (request == null || request.PatientId == Guid.Empty)
            return ValidationFail("患者信息不能为空");

        var result = await _registrationService.QuickVisitAsync(request, GetCurrentUserId());
        return HandleResult(result, "快速看诊创建成功");
    }
}
