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
    private readonly IPatientImportExportService _importExportService;

    public PatientsController(
        IPatientService patientService,
        IPatientImportExportService importExportService,
        ILogger<PatientsController> logger) : base(logger)
    {
        _patientService = patientService;
        _importExportService = importExportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string? keyword = null)
    {
        var result = await _patientService.SearchAsync(keyword ?? "");
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

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _patientService.RestoreAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _patientService.ToggleStatusAsync(id);
        return HandleResult(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? keyword = null)
    {
        var stream = await _importExportService.ExportPatientsAsync(keyword);
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "patients.xlsx");
    }

    [HttpGet("import-template")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportTemplate()
    {
        var stream = await _importExportService.ExportTemplateAsync(new ExportTemplateDto());
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "patients-template.xlsx");
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return ValidationFail("请上传文件");
        using var stream = file.OpenReadStream();
        var result = await _importExportService.BatchImportAsync(stream, file.FileName);
        return HandleResult(result);
    }

    [HttpGet("{id}/check-reference")]
    public async Task<IActionResult> CheckReference(Guid id)
    {
        var result = await _patientService.CheckReferenceAsync(id);
        return HandleResult(result);
    }

    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _patientService.BatchCheckReferenceAsync(request.Ids);
        return HandleResult(result);
    }
}
