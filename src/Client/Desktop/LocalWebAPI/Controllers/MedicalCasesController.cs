using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MedicalCasesController : BaseApiController
{
    private readonly ISender _sender;

    public MedicalCasesController(
        ISender sender,
        ILogger<MedicalCasesController> logger) : base(logger)
    {
        _sender = sender;
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
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin || includeAllDoctors;
        var result = await _sender.Send(new GetMedicalCasesQuery(
            status, patientId, page, pageSize, operatorId, isAdmin, keyword));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value!, "查询成功");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var mc = await _sender.Send(new GetMedicalCaseQuery(id));
        if (!mc.IsSuccess || mc.Value == null)
            return NotFound(mc.Error ?? "医案不存在");

        var consultations = await _sender.Send(new GetMedicalCaseConsultationsQuery(id));
        var prescriptions = await _sender.Send(new GetMedicalCasePrescriptionsQuery(id));

        return Success(new
        {
            medicalCase = mc.Value,
            consultation = consultations.IsSuccess ? consultations.Value?.FirstOrDefault() : null,
            prescription = prescriptions.IsSuccess ? prescriptions.Value?.FirstOrDefault() : null
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
        var result = await _sender.Send(new SearchMedicalCasesQuery(
            patientName, diagnosisKeyword, startDate, endDate, page, pageSize));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "搜索失败");
        return SuccessPaged(result.Value!, "搜索成功");
    }

    [HttpGet("query")]
    public async Task<IActionResult> Query([FromQuery] MedicalCaseQueryDto query)
    {
        var doctorFilter = GetDoctorFilter();
        if (doctorFilter.HasValue && query.DoctorId == null)
            query.DoctorId = doctorFilter.Value;
        var result = await _sender.Send(new QueryMedicalCasesCommand(query));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value!, "查询成功");
    }

    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(MedicalCaseStatus status)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new GetMedicalCasesQuery(
            status, null, 1, 100, operatorId, isAdmin));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!.Items, "查询成功");
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] Guid? patientId = null)
    {
        var doctorFilter = GetDoctorFilter();
        var query = new MedicalCaseQueryDto
        {
            QueryType = MedicalCaseQueryType.Pending,
            PatientId = patientId,
            IncludeAllDoctors = !doctorFilter.HasValue
        };
        if (doctorFilter.HasValue)
            query.DoctorId = doctorFilter.Value;
        var result = await _sender.Send(new QueryMedicalCasesCommand(query));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!.Items, "查询成功");
    }

    [HttpGet("patient/{patientId:guid}/consultations")]
    public async Task<IActionResult> GetPatientConsultations(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _sender.Send(
            new GetPatientConsultationsQuery(patientId, page, pageSize));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    [HttpGet("patient/{patientId:guid}/prescriptions")]
    public async Task<IActionResult> GetPatientPrescriptions(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _sender.Send(
            new GetPatientPrescriptionsQuery(patientId, page, pageSize));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    [HttpGet("{id}/consultations")]
    public async Task<IActionResult> GetConsultations(Guid id)
    {
        var result = await _sender.Send(new GetMedicalCaseConsultationsQuery(id));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    [HttpGet("{id}/prescriptions")]
    public async Task<IActionResult> GetPrescriptions(Guid id)
    {
        var result = await _sender.Send(new GetMedicalCasePrescriptionsQuery(id));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    // ===================== Permissions & Audit Endpoints =====================

    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid id)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var roleInt = (int)operatorRole;
        var result = await _sender.Send(new GetMedicalCasePermissionsQuery(id, operatorId, roleInt));
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");
        return Success(result.Value!, "查询成功");
    }

    [HttpGet("{id}/audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;
        var result = await _sender.Send(new GetMedicalCaseAuditLogsQuery(id, page, pageSize));
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");
        return Success(result.Value!, "查询成功");
    }

    // ===================== Command Endpoints =====================

    [HttpPost]
    [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
    public async Task<IActionResult> Create([FromBody] MedicalCaseInputDto input)
    {
        var operatorId = GetCurrentUserId();
        var result = await _sender.Send(new CreateMedicalCaseCommand(input, operatorId));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "医案创建失败");
        return Success(result.Value!, "医案创建成功");
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Save(Guid id, [FromBody] MedicalCaseInputDto input)
    {
        input.Id = id;
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new SaveMedicalCaseCommand(input, operatorId, isAdmin));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "医案保存失败");
        return Success(result.Value!, "保存成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new DeleteMedicalCaseCommand(id, operatorId, isAdmin));
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");
        return Success(true, "删除成功");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new BatchDeleteMedicalCasesCommand(ids, operatorId, isAdmin));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");
        return Success(result.Value, result.Value.Message);
    }

    [HttpPost("batch-details")]
    public async Task<IActionResult> GetBatchDetails([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
            return ValidationFail("IDs不能为空");
        if (ids.Count > 50)
            return ValidationFail("最多查询50条");

        var result = await _sender.Send(new GetMedicalCasesBatchQuery(ids));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!, "查询成功");
    }

    // ===================== State Transition Endpoints =====================

    [HttpPut("{id}/close")]
    public async Task<IActionResult> CloseCase(Guid id)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "完成医案失败");
        return Success("医案已完成");
    }

    [HttpPut("{id}/suspend")]
    public async Task<IActionResult> SuspendCase(Guid id, [FromBody] ConsultationInputDto? request = null)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new SuspendMedicalCaseCommand(id, operatorId, isAdmin));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "挂起失败");
        return Success("医案已暂存");
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelCase(Guid id, [FromBody] CancelMedicalCaseRequestDto? request = null)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new CancelMedicalCaseCommand(id, operatorId, isAdmin, request?.Reason));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "取消失败");
        return Success("医案已取消");
    }

    [HttpPut("{id}/prescription-flag")]
    public async Task<IActionResult> SetPrescriptionFlag(Guid id, [FromBody] SetPrescriptionFlagRequest request)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _sender.Send(new SetPrescriptionFlagCommand(id, request.NeedsPrescription, operatorId, isAdmin));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "设置失败");
        return Success(result.Value!, "处方标记更新成功");
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto request)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;

        if (request.Status == MedicalCaseStatus.Completed)
        {
            var completeResult = await _sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin));
            if (!completeResult.IsSuccess)
                return BusinessFail(completeResult.Error ?? "完成医案失败");
            return Success("医案已完成");
        }

        var result = await _sender.Send(new UpdateMedicalCaseStatusCommand(id, request.Status, operatorId, isAdmin));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "状态更新失败");
        return Success(result.Value!, "状态更新成功");
    }

    [HttpPut("{id:guid}/print-completed")]
    public async Task<IActionResult> RecordPrint(Guid id, [FromBody] RecordPrintRequest request)
    {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _sender.Send(new RecordPrintCommand(
            id, request.PrintType, request.PrinterName, operatorId, operatorName));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "医案不存在");
        return Success(true, "打印记录已写入");
    }
}

public class SetPrescriptionFlagRequest
{
    public bool NeedsPrescription { get; set; }
}

public class RecordPrintRequest
{
    public int PrintType { get; set; }
    public string? PrinterName { get; set; }
}
