using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 患者管理 API - LocalWebAPI 简化版
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
public class PatientsController : BaseCrudController
{
    private readonly IPatientService _patientService;

    public PatientsController(
        ISender sender,
        ILogger<PatientsController> logger,
        IPatientService patientService) : base(sender, logger)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// 获取患者详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error) return error;

        var result = await _patientService.GetByIdAsync(id, ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "患者不存在");
        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 根据身份证号查询患者
    /// </summary>
    [HttpGet("by-id-number/{idNumber}")]
    public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken ct)
    {
        var result = await _patientService.GetByIdNumberAsync(idNumber, ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "未找到匹配的患者");
        return Success(result.Value);
    }

    /// <summary>
    /// 检查患者引用关系
    /// </summary>
    [HttpGet("{id:guid}/check-reference")]
    public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CheckPatientReferenceQuery(id), ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "患者不存在");
        return Success(result.Value, "引用检查完成");
    }

    /// <summary>
    /// 删除患者（软删除）— 仅 Admin+
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpDelete("{id:guid}")]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error) return error;

        var getResult = await _patientService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound("患者不存在");

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new DeletePatientCommand(id, operatorId), ct);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "删除失败");

        return Success(true, "删除成功");
    }

    /// <summary>
    /// 切换患者状态（启用/禁用）— 仅 Admin+
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("{id:guid}/toggle-status")]
    public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error) return error;

        var getResult = await _patientService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound("患者不存在");

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new TogglePatientStatusCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");

        return Success(result.Value, "状态已切换");
    }

    /// <summary>
    /// 批量检查引用关系
    /// </summary>
    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)
        => await ExecuteBatchCheckReferenceAsync(
            dto.PatientIds,
            ids => new BatchCheckPatientReferenceQuery(ids),
            "请至少选择一个患者",
            "批量检查最多支持100条",
            "批量检查失败",
            ct);
}
