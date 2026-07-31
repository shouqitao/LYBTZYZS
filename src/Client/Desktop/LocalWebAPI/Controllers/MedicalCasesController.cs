using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Module.MedicalCases.Controllers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 医疗案例管理 API - 继承 BaseMedicalCasesController 提供标准方法（简化版）
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public class MedicalCasesController : BaseMedicalCasesController
{
    public MedicalCasesController(
        ISender sender,
        ILogger<MedicalCasesController> logger) : base(sender, logger)
    {
    }

    /// <summary>
    /// 获取医案详情
    /// </summary>
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMedicalCaseQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "医案不存在");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 统一医案查询端点
    /// </summary>
    [HttpGet("query")]
    public override async Task<IActionResult> Query([FromQuery] MedicalCaseQueryDto query, CancellationToken ct = default)
    {
        var doctorFilter = GetDoctorFilter();
        if (doctorFilter.HasValue && query.DoctorId == null)
            query.DoctorId = doctorFilter.Value;
        var result = await Sender.Send(new QueryMedicalCasesCommand(query), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value!, "查询成功");
    }

    /// <summary>
    /// 按状态查询医案
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(MedicalCaseStatus status, CancellationToken ct = default)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await Sender.Send(new GetMedicalCasesQuery(
            status, null, 1, 100, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!.Items, "查询成功");
    }

    /// <summary>
    /// 获取待处理医案
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] Guid? patientId = null, CancellationToken ct = default)
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
        var result = await Sender.Send(new QueryMedicalCasesCommand(query), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");
        return Success(result.Value!.Items, "查询成功");
    }

    /// <summary>
    /// 关闭医案
    /// </summary>
    [HttpPut("{id}/close")]
    public async Task<IActionResult> CloseCase(Guid id, CancellationToken ct)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await Sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "完成医案失败");
        return Success("医案已完成");
    }

    /// <summary>
    /// 暂存医案
    /// </summary>
    [HttpPut("{id}/suspend")]
    public async Task<IActionResult> SuspendCase(Guid id, [FromBody] ConsultationInputDto? request = null, CancellationToken ct = default)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await Sender.Send(new SuspendMedicalCaseCommand(id, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "挂起失败");
        return Success("医案已暂存");
    }

    /// <summary>
    /// 取消医案
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelCase(Guid id, [FromBody] CancelMedicalCaseRequestDto? request = null, CancellationToken ct = default)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await Sender.Send(new CancelMedicalCaseCommand(id, operatorId, isAdmin, request?.Reason), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "取消失败");
        return Success("医案已取消");
    }

    /// <summary>
    /// 更新医案状态
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto request, CancellationToken ct = default)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;

        if (request.Status == MedicalCaseStatus.Completed)
        {
            var completeResult = await Sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
            if (!completeResult.IsSuccess)
                return BusinessFail(completeResult.Error ?? "完成医案失败");
            return Success("医案已完成");
        }

        var result = await Sender.Send(new UpdateMedicalCaseStatusCommand(id, request.Status, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "状态更新失败");
        return Success(result.Value!, "状态更新成功");
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private bool IsAdmin()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
    }

    private Guid? GetDoctorFilter()
        => IsAdmin() ? null : GetCurrentUserId();
}
