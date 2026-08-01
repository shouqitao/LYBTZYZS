using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
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
    public PatientsController(
        ISender sender,
        ILogger<PatientsController> logger) : base(sender, logger)
    {
    }

    /// <summary>
    /// 获取患者详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error) return error;

        var result = await Sender.Send(new GetPatientQuery(id), ct);
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
        var result = await Sender.Send(new SearchPatientByIdNumberQuery(idNumber), ct);
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
    /// 批量检查引用关系
    /// </summary>
    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)
    {
        if (dto.PatientIds == null || dto.PatientIds.Count == 0)
            return ValidationFail("请至少选择一个患者");
        if (dto.PatientIds.Count > 100)
            return ValidationFail("批量检查最多支持100条");
        var result = await Sender.Send(new BatchCheckPatientReferenceQuery(dto.PatientIds), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量检查失败");
        return Success(result.Value, "批量引用检查完成");
    }
}
