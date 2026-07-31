using MediatR;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Controllers;

/// <summary>
/// 医案管理 Controller 共享基类
/// 继承 BaseCrudController 提供标准 CRUD，保留医案特化方法
/// </summary>
public abstract class BaseMedicalCasesController : BaseCrudController<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto, GetMedicalCasesQuery>
{
    protected BaseMedicalCasesController(ISender sender, ILogger logger)
        : base(sender, logger)
    {
    }

    #region 抽象方法实现

    protected override GetMedicalCasesQuery CreateGetListQuery(int page, int pageSize, string? keyword)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        return new GetMedicalCasesQuery(
            Page: page,
            PageSize: pageSize,
            CurrentDoctorId: operatorId,
            IsAdmin: isAdmin,
            Keyword: keyword);
    }

    protected override IRequest<Result<MedicalCaseDetailDto>> CreateCreateCommand(MedicalCaseInputDto dto, Guid operatorId)
    {
        dto.Id = null;
        return new CreateMedicalCaseCommand(dto, operatorId);
    }

    protected override IRequest<Result<MedicalCaseDetailDto>> CreateUpdateCommand(Guid id, MedicalCaseInputDto dto, Guid operatorId)
    {
        var (_, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        return new SaveMedicalCaseCommand(dto, operatorId, isAdmin);
    }

    protected override IRequest<Result> CreateDeleteCommand(Guid id, Guid operatorId)
        => throw new NotSupportedException("医案删除需要 isAdmin 参数，请在子类 override Delete 方法");

    protected override IRequest<Result<BatchOperationResultDto>> CreateBatchDeleteCommand(List<Guid> ids, Guid operatorId)
    {
        var (_, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        return new BatchDeleteMedicalCasesCommand(ids, operatorId, isAdmin);
    }

    #endregion

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

        var result = await Sender.Send(new SearchMedicalCasesQuery(
            patientName, diagnosisKeyword, startDate, endDate, page, pageSize), ct);

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "搜索失败");

        return Success(result.Value!, "搜索成功");
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

        var result = await Sender.Send(new QueryMedicalCasesCommand(query), ct);

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 查询患者辨证记录历史
    /// </summary>
    [HttpGet("patient/{patientId:guid}/consultations")]
    public virtual async Task<IActionResult> GetPatientConsultations(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(
            new GetPatientConsultationsQuery(patientId, page, pageSize), ct);

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 查询患者处方历史
    /// </summary>
    [HttpGet("patient/{patientId:guid}/prescriptions")]
    public virtual async Task<IActionResult> GetPatientPrescriptions(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(
            new GetPatientPrescriptionsQuery(patientId, page, pageSize), ct);

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 查询辨证记录列表
    /// </summary>
    [HttpGet("{medicalCaseId:guid}/consultations")]
    public virtual async Task<IActionResult> GetConsultations(
        Guid medicalCaseId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMedicalCaseConsultationsQuery(medicalCaseId), ct);

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 查询处方列表
    /// </summary>
    [HttpGet("{medicalCaseId:guid}/prescriptions")]
    public virtual async Task<IActionResult> GetPrescriptions(
        Guid medicalCaseId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMedicalCasePrescriptionsQuery(medicalCaseId), ct);

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 批量查询医案详情（≤50条）
    /// </summary>
    [HttpPost("batch-details")]
    public virtual async Task<IActionResult> GetBatchDetails([FromBody] List<Guid> ids, CancellationToken ct)
    {
        if (ids == null || ids.Count == 0)
            return ValidationFail("IDs不能为空");
        if (ids.Count > 50)
            return ValidationFail("最多查询50条");

        var result = await Sender.Send(new GetMedicalCasesBatchQuery(ids), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 获取医案操作权限
    /// </summary>
    [HttpGet("{id:guid}/permissions")]
    public virtual async Task<IActionResult> GetPermissions(Guid id, CancellationToken ct)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var roleInt = (int)operatorRole;
        var result = await Sender.Send(new GetMedicalCasePermissionsQuery(id, operatorId, roleInt), ct);
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
        var result = await Sender.Send(new GetMedicalCaseAuditLogsQuery(id, page, pageSize), ct);
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

        var result = await Sender.Send(new SetPrescriptionFlagCommand(id, request.NeedsPrescription, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
        {
            return NotFound(result.Error ?? "医案不存在");
        }

        return Success(result.Value!, "处方标记更新成功");
    }

    /// <summary>
    /// 记录打印完成
    /// </summary>
    [HttpPut("{id:guid}/print-completed")]
    public virtual async Task<IActionResult> RecordPrint(
        Guid id,
        [FromBody] RecordPrintRequest request, CancellationToken ct)
    {
        var (operatorId, operatorName, _) = GetOperator();
        var result = await Sender.Send(new RecordPrintCommand(
            id, request.PrintType, request.PrinterName, operatorId, operatorName), ct);

        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        LogOperation("打印记录写入成功", null, id);
        return Success(true, "打印记录已写入");
    }

    #endregion
}
