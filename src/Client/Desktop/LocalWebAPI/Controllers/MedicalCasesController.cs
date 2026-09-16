using LYBT.Infrastructure.Constants;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Module.MedicalCases.Controllers;
using LYBT.Module.MedicalCases.Interfaces;
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
        ILogger<MedicalCasesController> logger,
        IMedicalCaseCommandService medicalCaseCommandService,
        IMedicalCaseQueryService medicalCaseQueryService,
        IMedicalCaseStateService medicalCaseStateService)
        : base(sender, logger, medicalCaseCommandService, medicalCaseQueryService, medicalCaseStateService)
    {
    }

    /// <summary>
    /// 查询医案列表（分页）
    /// </summary>
    [HttpGet]
    public override async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] CommonStatus? status = null,
        CancellationToken ct = default)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await Sender.Send(new GetMedicalCaseListQuery(
            Status: null,
            PatientId: null,
            Page: page,
            PageSize: pageSize,
            CurrentDoctorId: operatorId,
            IsAdmin: isAdmin,
            Keyword: keyword), ct);

        return Success(result, "查询成功");
    }

    /// <summary>
    /// 获取医案详情
    /// </summary>
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
        var result = await Sender.Send(new GetMedicalCaseQuery(id, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 统一医案查询端点
    /// </summary>
    [HttpGet("query")]
    public override async Task<IActionResult> Query([FromQuery] MedicalCaseQueryDto query, CancellationToken ct = default)
    {
        if (ValidatePagination(query.PageIndex, query.PageSize) is { } error) return error;

        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        if (!isAdmin && query.DoctorId == null)
            query.DoctorId = operatorId;
        var result = await _medicalCaseQueryService.QueryAsync(query, ct);
        return SuccessPaged(result, "查询成功");
    }

    /// <summary>
    /// 按状态查询医案
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(MedicalCaseStatus status, CancellationToken ct = default)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await _medicalCaseQueryService.GetListDtoAsync(
            status, null, 1, 100, operatorId, isAdmin, cancellationToken: ct);
        return Success(result.Items, "查询成功");
    }

    /// <summary>
    /// 获取待处理医案
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] Guid? patientId = null, CancellationToken ct = default)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
        var result = await Sender.Send(new GetPendingCasesQuery(operatorId, patientId, isAdmin), ct);
        return Success(result.Items, "查询成功");
    }

    /// <summary>
    /// 保存医案聚合根
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] MedicalCaseInputDto input, CancellationToken ct)
    {
        if (input.Id != id)
        {
            return Error("请求ID与路由ID不一致");
        }

        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await Sender.Send(new UpdateMedicalCaseCommand(input, operatorId, isAdmin), ct);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error ?? "医案不存在");
        }

        return Success(result.Value!, "保存成功");
    }

    /// <summary>
    /// 删除医案（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await Sender.Send(new DeleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "医案不存在");

        return Success(true, "医案已删除");
    }

    /// <summary>
    /// 批量删除医案
    /// </summary>
    [HttpPost("batch-delete")]
    public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
        {
            return ValidationFail("请至少选择一个医案");
        }

        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await _medicalCaseCommandService.BatchDeleteAsync(dto.Ids, operatorId, isAdmin, ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return BusinessFail(result.Error ?? "批量删除失败");
        }

        LogOperation("批量删除医案", new { Ids = dto.Ids, Result = result.Value.Message }, null);
        return Success(result.Value, result.Value.Message);
    }

    /// <summary>
    /// 批量查询医案详情（P0-9 双端对齐：Remote 已有，Local 补齐）
    /// </summary>
    [HttpPost("batch-details")]
    [ProducesResponseType(typeof(ApiResponse<List<MedicalCaseDetailDto>>), 200)]
    public async Task<IActionResult> GetBatchDetails([FromBody] BatchIdsRequest request, CancellationToken ct)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("医案ID列表不能为空");
        if (request.Ids.Count > BatchOptions.DefaultMaxBatchSize)
            return ValidationFail($"单次批量查询不能超过 {BatchOptions.DefaultMaxBatchSize} 条");
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
        var result = await _medicalCaseQueryService.GetDetailDtosBatchAsync(request.Ids, operatorId, isAdmin, ct);
        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 创建新医案 — 仅 Doctor
    /// </summary>
    [Authorize(Policy = PolicyConstants.DoctorOnly)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MedicalCaseInputDto input, CancellationToken ct)
    {
        var (doctorId, _, _) = GetOperator();
        var result = await Sender.Send(
            new CreateMedicalCaseCommand(input, doctorId, IsAdmin: false), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "创建失败");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<MedicalCaseDetailDto>.CreateSuccess(result.Value!, "医案创建成功")
        );
    }

    /// <summary>
    /// 关闭医案（P1-11 2026-08-14: 权限判断改方法级 Authorize——强制关闭仅限 Admin/SuperAdmin，双端同步）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPut("{id}/close")]
    public async Task<IActionResult> CloseCase(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "医案ID") is { } error) return error;
        var (operatorId, _, _) = GetOperator();

        var result = await Sender.Send(
            new CompleteMedicalCaseCommand(id, operatorId, IsAdmin: true, SkipWorkflowValidation: true), ct);
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
        if (ValidateGuid(id, "医案ID") is { } error) return error;
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin || operatorRole == UserRole.Admin;

        var result = await Sender.Send(
            new SuspendMedicalCaseCommand(id, request, operatorId, isAdmin), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "挂起失败");
        LogOperation("暂存医案", request, id);
        return Success("医案已暂存");
    }

    /// <summary>
    /// 取消医案
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelCase(Guid id, [FromBody] CancelMedicalCaseRequest? request = null, CancellationToken ct = default)
    {
        if (ValidateGuid(id, "医案ID") is { } error) return error;
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;

        var result = await Sender.Send(
            new CancelMedicalCaseCommand(id, operatorId, isAdmin, request?.Reason), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "取消失败");
        LogOperation("取消医案", null, id);
        return Success("医案已取消");
    }

    /// <summary>
    /// 更新医案状态（P1-10 2026-08-14: Completed 分支路由移入 StateService.UpdateStatus 统一处理——双端同步）
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto request, CancellationToken ct = default)
    {
        if (ValidateGuid(id, "医案ID") is { } error) return error;
        var (operatorId, _, operatorRole) = GetOperator();
        var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;

        // P1-10: 统一走 StateService 状态机校验（含 Completed→CompleteAsync 统一分派）
        var entity = await _medicalCaseStateService.UpdateStatusAsync(
            id, request.Status, operatorId, isAdmin, ct);
        if (entity == null)
            return BusinessFail("状态更新失败");

        LogOperation("更新医案状态", request, id);
        return Success("状态更新成功");
    }
}
