using MediatR;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Controllers;

/// <summary>
/// 医案管理 Controller 共享基类
/// 继承 BaseCrudController 提供标准 CRUD，保留医案特化方法
/// </summary>
public abstract class BaseMedicalCasesController : BaseCrudController
{
    protected readonly IMedicalCaseCommandService _medicalCaseCommandService;
    protected readonly IMedicalCaseQueryService _medicalCaseQueryService;
    protected readonly IMedicalCaseStateService _medicalCaseStateService;

    protected BaseMedicalCasesController(
        ISender sender,
        ILogger logger,
        IMedicalCaseCommandService medicalCaseCommandService,
        IMedicalCaseQueryService medicalCaseQueryService,
        IMedicalCaseStateService medicalCaseStateService)
        : base(sender, logger)
    {
        _medicalCaseCommandService = medicalCaseCommandService;
        _medicalCaseQueryService = medicalCaseQueryService;
        _medicalCaseStateService = medicalCaseStateService;
    }

    #region 医案特化方法

    /// <summary>
    /// 跨医案搜索
    /// </summary>
    [HttpGet("search")]
    public virtual async Task<IActionResult> Search(
        [FromQuery] string? patientName = null,
        [FromQuery] string? diagnosisKeyword = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
        var result = await _medicalCaseQueryService.SearchMedicalCasesAsync(
            patientName, diagnosisKeyword, startDate, endDate, page, pageSize, operatorId, isAdmin, ct);

        return Success(result, "搜索成功");
    }

    /// <summary>
    /// 统一医案查询端点
    /// </summary>
    [HttpGet("query")]
    public virtual async Task<IActionResult> Query([FromQuery] MedicalCaseQueryDto query, CancellationToken ct = default)
    {
        if (ValidatePagination(query.PageIndex, query.PageSize) is { } error) return error;

        var (operatorId, _, operatorRole) = GetOperator();

        if (!query.DoctorId.HasValue)
        {
            query.DoctorId = operatorId;
        }

        if (operatorRole is UserRole.SuperAdmin or UserRole.Admin)
        {
            query.IncludeAllDoctors = true;
        }

        var result = await _medicalCaseQueryService.QueryAsync(query, ct);

        return Success(result, "查询成功");
    }

    /// <summary>
    /// 获取医案操作权限
    /// </summary>
    [HttpGet("{id:guid}/permissions")]
    public virtual async Task<IActionResult> GetPermissions(Guid id, CancellationToken ct)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var roleInt = (int)operatorRole;
        var result = await _medicalCaseQueryService.GetPermissionsAsync(id, operatorId, roleInt, ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");
        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 获取医案审计日志
    /// </summary>
    [HttpGet("{id:guid}/audit-logs")]
    public virtual async Task<IActionResult> GetAuditLogs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
        var result = await _medicalCaseQueryService.GetAuditLogsAsync(id, page, pageSize, operatorId, isAdmin, ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");
        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 标记是否需要开处方
    /// </summary>
    [HttpPut("{id:guid}/prescription-flag")]
    public virtual async Task<IActionResult> SetPrescriptionFlag(
        Guid id,
        [FromBody] SetPrescriptionFlagRequest request, CancellationToken ct)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await _medicalCaseCommandService.SetPrescriptionFlagWithDetailAsync(
            id, request.NeedsPrescription, operatorId, isAdmin, ct);

        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        return Success(result.Value!, "处方标记更新成功");
    }

    /// <summary>
    /// 记录打印完成 — 仅 Doctor（打印仅 Doctor，2026-08-03 决策）
    /// </summary>
    [Authorize(Policy = PolicyConstants.DoctorOnly)]
    [HttpPut("{id:guid}/print-completed")]
    public virtual async Task<IActionResult> RecordPrint(
        Guid id,
        [FromBody] RecordPrintRequest request, CancellationToken ct)
    {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await _medicalCaseCommandService.RecordPrintAsync(
            id, request.PrintType, request.PrinterName, operatorId, operatorName, ct);

        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        LogOperation("打印记录写入成功", null, id);
        return Success(true, "打印记录已写入");
    }

    #endregion
}
