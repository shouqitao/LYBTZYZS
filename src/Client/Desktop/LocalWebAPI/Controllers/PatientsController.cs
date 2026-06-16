using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : BaseApiController
{
    private readonly IPatientService _patientService;

    public PatientsController(
        IPatientService patientService,
        ILogger<PatientsController> logger) : base(logger)
    {
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        var result = await _patientService.GetPagedAsync(page, pageSize, keyword);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _patientService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientInputDto dto)
    {
        var result = await _patientService.CreateAsync(dto);
        return HandleResult(result, "患者创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto dto)
    {
        var result = await _patientService.UpdateAsync(id, dto);
        return HandleResult(result, "患者更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _patientService.DeleteAsync(id);
        return HandleResult(result, "患者删除成功");
    }

    [HttpGet("by-id-number/{idNumber}")]
    public async Task<IActionResult> GetByIdNumber(string idNumber)
    {
        var result = await _patientService.SearchAsync(idNumber);
        if (result.IsSuccess && result.Data != null && result.Data.Count > 0)
        {
            var patient = result.Data.FirstOrDefault(p => p.IdNumber == idNumber);
            if (patient != null)
                return Success(patient);
        }
        return NotFound("未找到匹配的患者");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _patientService.BatchDeleteAsync(request.Ids);
        return HandleResult(result);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _patientService.ToggleStatusAsync(id);
        return HandleResult(result);
    }


}
