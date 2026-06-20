using System.Security.Claims;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MedicalCasesController : BaseApiController
{
    private readonly IMedicalCaseFacade _facade;
    private readonly IMedicalCaseQueryService _queryService;

    public MedicalCasesController(
        IMedicalCaseFacade facade,
        IMedicalCaseQueryService queryService,
        ILogger<MedicalCasesController> logger) : base(logger)
    {
        _facade = facade;
        _queryService = queryService;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
    }

    private UserRole GetCurrentUserRole()
        => Enum.TryParse<UserRole>(User.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : UserRole.Receptionist;

    private Guid? GetDoctorFilter()
        => IsAdmin() ? null : GetCurrentUserId();

    // ===================== Query Endpoints =====================

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] MedicalCaseStatus? status = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeAllDoctors = false,
        [FromQuery] string? keyword = null)
    {
        var result = await _facade.GetListDtoAsync(status, patientId, page, pageSize, GetDoctorFilter(), IsAdmin(), keyword);
        return SuccessPaged(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var consultations = await _facade.GetConsultationListAsync(id);
        var prescriptions = await _facade.GetPrescriptionListAsync(id);
        var mc = await _facade.GetByIdAsync(id);
        if (mc == null) return NotFound();
        return Success(new
        {
            medicalCase = mc,
            consultation = consultations.FirstOrDefault(),
            prescription = prescriptions.FirstOrDefault()
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? patientName,
        [FromQuery] string? diagnosisKeyword,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _facade.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
        return SuccessPaged(result);
    }

    [HttpGet("query")]
    public async Task<IActionResult> Query([FromQuery] MedicalCaseQueryDto query)
    {
        var doctorFilter = GetDoctorFilter();
        if (doctorFilter.HasValue && query.DoctorId == null)
            query.DoctorId = doctorFilter.Value;
        var result = await _facade.QueryAsync(query);
        return SuccessPaged(result);
    }

    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(MedicalCaseStatus status)
    {
        var doctorFilter = GetDoctorFilter();
        var list = await _queryService.GetListAsync(status, null, 1, 100, doctorFilter);
        return Success(list);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] Guid? patientId = null)
    {
        var doctorFilter = GetDoctorFilter();
        if (doctorFilter.HasValue)
        {
            var result = await _facade.GetPendingCasesAsync(doctorFilter.Value, patientId);
            return Success(result);
        }
        var all = await _facade.GetAllPendingCasesAsync();
        return Success(all);
    }

    [HttpGet("{id}/consultations")]
    public async Task<IActionResult> GetConsultations(Guid id)
    {
        var result = await _facade.GetConsultationListAsync(id);
        return Success(result);
    }

    [HttpGet("{id}/prescriptions")]
    public async Task<IActionResult> GetPrescriptions(Guid id)
    {
        var result = await _facade.GetPrescriptionListAsync(id);
        return Success(result);
    }

    // ===================== Command Endpoints =====================

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MedicalCaseInputDto input)
    {
        var operatorId = GetCurrentUserId();
        var result = await _facade.SaveAsync(input, operatorId, IsAdmin());
        if (result == null) return BusinessFail("医案创建失败");
        return Success(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Save(Guid id, [FromBody] MedicalCaseInputDto input)
    {
        input.Id = id;
        var operatorId = GetCurrentUserId();
        var result = await _facade.SaveAsync(input, operatorId, IsAdmin());
        if (result == null) return BusinessFail("医案保存失败");
        return Success(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _facade.DeleteAsync(id, GetCurrentUserId(), IsAdmin());
        if (result) return Success("删除成功");
        return BusinessFail("删除失败");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _facade.BatchDeleteAsync(ids, GetCurrentUserId(), IsAdmin());
        return HandleResult(result);
    }

    // ===================== State Transition Endpoints =====================

    [HttpPut("{id}/close")]
    public async Task<IActionResult> CloseCase(Guid id)
    {
        var result = await _facade.CompleteAsync(id, GetCurrentUserId(), IsAdmin());
        if (result == null) return BusinessFail("完成医案失败");
        return Success(result, "医案已完成");
    }

    [HttpPut("{id}/suspend")]
    public async Task<IActionResult> SuspendCase(Guid id, [FromBody] ConsultationInputDto? request = null)
    {
        var result = await _facade.SuspendAsync(id, request, GetCurrentUserId(), IsAdmin());
        if (result == null) return BusinessFail("挂起失败");
        return Success(result, "医案已挂起");
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelCase(Guid id, [FromBody] CancelMedicalCaseRequestDto? request = null)
    {
        var result = await _facade.CancelAsync(id, GetCurrentUserId(), IsAdmin(), request?.Reason);
        if (result == null) return BusinessFail("取消失败");
        return Success("医案已取消");
    }

    [HttpPut("{id}/prescription-flag")]
    public async Task<IActionResult> SetPrescriptionFlag(Guid id, [FromBody] SetPrescriptionFlagRequest request)
    {
        var result = await _facade.SetPrescriptionFlagAsync(id, request.NeedsPrescription, GetCurrentUserId(), IsAdmin());
        if (result == null) return BusinessFail("设置失败");
        return Success(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto request)
    {
        if (request.Status == MedicalCaseStatus.Completed)
            return ValidationFail("请使用 /close 端点完成医案");
        var result = await _facade.UpdateStatusAsync(id, request.Status);
        if (result == null) return BusinessFail("状态更新失败");
        return Success(result);
    }
}

public class SetPrescriptionFlagRequest
{
    public bool NeedsPrescription { get; set; }
}
