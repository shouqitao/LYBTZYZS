using MediatR;
using LYBT.Infrastructure.Constants;
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
/// 提供 GetList、GetById、Create、Save、Delete、BatchDelete、GetPatientConsultations、GetPatientPrescriptions、
/// GetConsultations、GetPrescriptions、GetBatchDetails、GetPermissions、GetAuditLogs、SetPrescriptionFlag、RecordPrint 等方法
/// </summary>
public abstract class BaseMedicalCasesController : BaseApiController
{
    private readonly ISender _sender;

    protected BaseMedicalCasesController(ISender sender, ILogger logger)
        : base(logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    protected ISender Sender => _sender;

    /// <summary>
    /// 查询医案列表（分页）
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> GetList(
        [FromQuery] MedicalCaseStatus? status = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeAllDoctors = false,
        [FromQuery] string? keyword = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin || includeAllDoctors;
        var result = await _sender.Send(new GetMedicalCasesQuery(
            status, patientId, page, pageSize, operatorId, isAdmin, keyword), ct);

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "查询失败");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 获取医案详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMedicalCaseQuery(id), ct);

        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        return Success(result.Value!, "查询成功");
    }

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

        var result = await _sender.Send(new SearchMedicalCasesQuery(
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

        var result = await _sender.Send(new QueryMedicalCasesCommand(query), ct);

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
        var result = await _sender.Send(
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
        var result = await _sender.Send(
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
        var result = await _sender.Send(new GetMedicalCaseConsultationsQuery(medicalCaseId), ct);

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
        var result = await _sender.Send(new GetMedicalCasePrescriptionsQuery(medicalCaseId), ct);

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

        var result = await _sender.Send(new GetMedicalCasesBatchQuery(ids), ct);
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
        var result = await _sender.Send(new GetMedicalCasePermissionsQuery(id, operatorId, roleInt), ct);
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
        var result = await _sender.Send(new GetMedicalCaseAuditLogsQuery(id, page, pageSize), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");
        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 创建新医案
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] MedicalCaseInputDto dto, CancellationToken ct)
    {
        var (doctorId, _, _) = GetOperator();

        dto.Id = null;
        var result = await _sender.Send(new CreateMedicalCaseCommand(dto, doctorId), ct);

        if (!result.IsSuccess)
            return NotFound(result.Error ?? "患者不存在");

        LogOperation("医案创建成功", result.Value, result.Value.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id, version = ApiVersionConstants.V1 },
            result.Value);
    }

    /// <summary>
    /// 保存医案聚合根
    /// </summary>
    [HttpPut("{id:guid}")]
    public virtual async Task<IActionResult> Save(
        Guid id,
        [FromBody] MedicalCaseInputDto request, CancellationToken ct)
    {
        if (request.Id != id)
        {
            return Error("请求ID与路由ID不一致");
        }

        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await _sender.Send(new SaveMedicalCaseCommand(request, operatorId, isAdmin), ct);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error ?? "医案不存在");
        }

        LogOperation("医案聚合保存成功", result.Value, id);
        return Success(result.Value!, "保存成功");
    }

    /// <summary>
    /// 删除医案（软删除）
    /// </summary>
    [HttpDelete("{id:guid}")]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await _sender.Send(new DeleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        LogOperation("医案已软删除", null, id);
        return Success(true, "医案已删除");
    }

    /// <summary>
    /// 批量删除医案
    /// </summary>
    [HttpPost("batch-delete")]
    public virtual async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            return ValidationFail("请至少选择一个医案");
        }

        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await _sender.Send(new BatchDeleteMedicalCasesCommand(dto.Ids, operatorId, isAdmin), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return BusinessFail(result.Error ?? "批量删除失败");
        }

        LogOperation("批量删除医案", new { Ids = dto.Ids, Result = result.Value.Message }, null);
        return Success(result.Value, result.Value.Message);
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

        var result = await _sender.Send(new SetPrescriptionFlagCommand(id, request.NeedsPrescription, operatorId, isAdmin), ct);
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
        var result = await _sender.Send(new RecordPrintCommand(
            id, request.PrintType, request.PrinterName, operatorId, operatorName), ct);

        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        LogOperation("打印记录写入成功", null, id);
        return Success(true, "打印记录已写入");
    }
}
