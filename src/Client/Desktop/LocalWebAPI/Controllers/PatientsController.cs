using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
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
        IPatientService patientService
    )
        : base(sender, logger)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// 获取患者列表 - 支持分页和查询
    /// </summary>
    [HttpGet]
    public override async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] CommonStatus? status = null,
        CancellationToken ct = default
    )
    {
        if (ValidatePagination(page, pageSize) is { } error)
            return error;

        var isAdmin =
            User?.IsInRole(RoleConstants.Admin) == true
            || User?.IsInRole(RoleConstants.SuperAdmin) == true;

        var result = await _patientService.GetPagedAsync(
            page,
            pageSize,
            keyword,
            filterDisabled: !isAdmin,
            ct
        );
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");

        return SuccessPaged(result.Value, "查询成功");
    }

    /// <summary>
    /// 获取患者详情
    /// </summary>
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error)
            return error;

        var result = await _patientService.GetByIdAsync(id, ct);
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "患者不存在");
        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 新增患者
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientInputDto input, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new CreatePatientCommand(input, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            // PATIENT-PHONE-409-FIX: 按 ErrorCode 映射（电话唯一 → 409）——原 BusinessFail 恒 422
            return HandleResult(result, useAuthMapping: true);
        }

        LogOperation("新增患者成功", result.Value, null);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            ApiResponse<PatientDetailDto>.CreateSuccess(result.Value, "患者创建成功")
        );
    }

    /// <summary>
    /// 更新患者信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] PatientInputDto input,
        CancellationToken ct
    )
    {
        if (ValidateGuid(id, "患者ID") is { } error)
            return error;

        // P1-9（2026-08-14）: 存在性+所有权检查移入 CommandHandler——Controller 仅编排
        var (operatorId, _, operatorRole) = GetOperator();
        var result = await Sender.Send(
            new UpdatePatientCommand(id, input, operatorId, operatorRole),
            ct
        );
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return HandleResult(result, useAuthMapping: true);
        }

        LogOperation("更新患者成功", result.Value, id);
        return Success(result.Value, "患者更新成功");
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
    /// 下载患者导入 JSON 模板（2026-08-13：Excel→JSON——后端不涉及 Excel 格式，保持通用性）
    /// </summary>
    [HttpGet("import-template")]
    public IActionResult ImportTemplate()
    {
        var template = new
        {
            Description = "患者批量导入 JSON 模板（与 POST /patients/batch-import 期望的 DTO 一致）",
            Fields = new[]
            {
                new
                {
                    Field = "Name",
                    Required = true,
                    Description = "姓名",
                },
                new
                {
                    Field = "Gender",
                    Required = true,
                    Description = "性别（Male/Female）",
                },
                new
                {
                    Field = "BirthDate",
                    Required = false,
                    Description = "出生日期（yyyy-MM-dd）",
                },
                new
                {
                    Field = "IdNumber",
                    Required = false,
                    Description = "身份证号（敏感字段）",
                },
                new
                {
                    Field = "PhoneNumber",
                    Required = false,
                    Description = "手机号（敏感字段）",
                },
                new
                {
                    Field = "PinYinCode",
                    Required = false,
                    Description = "拼音码",
                },
            },
            Example = new[]
            {
                new
                {
                    Name = "张三",
                    Gender = "Male",
                    BirthDate = "1990-01-01",
                    IdNumber = "110101199001010011",
                    PhoneNumber = "13800138000",
                    PinYinCode = "zhangsan",
                },
            },
        };
        return Success(template, "患者导入模板（JSON）");
    }

    /// <summary>
    /// 导出患者数据为 JSON 数组（2026-08-13：Excel→JSON——敏感字段自动脱敏管道）
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? keyword = null,
        CancellationToken ct = default
    )
    {
        var result = await _patientService.GetPagedAsync(
            1,
            10000,
            keyword,
            filterDisabled: false,
            ct
        );
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "导出失败");

        // JSON 数组（SensitiveDataJsonConverterFactory 管道自动脱敏 PhoneNumber 等敏感字段）
        return Success(result.Value.Items, "患者导出（JSON）");
    }

    /// <summary>
    /// 检查患者引用关系
    /// </summary>
    [HttpGet("{id}/check-reference")]
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
    [HttpDelete("{id}")]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error)
            return error;

        // P1-9（2026-08-14）: 存在性+所有权检查移入 CommandHandler
        var (operatorId, _, operatorRole) = GetOperator();
        var result = await Sender.Send(
            new DeletePatientCommand(id, operatorId, operatorRole),
            ct
        );
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCode.Forbidden)
                return Forbid(result.Error ?? "无权删除该患者");
            return BusinessFail(result.Error ?? "删除失败");
        }

        return Success(true, "删除成功");
    }

    /// <summary>
    /// 切换患者状态（启用/禁用）— 仅 Admin+
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("{id}/toggle-status")]
    public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error)
            return error;

        // P1-9（2026-08-14）: 存在性+所有权检查移入 CommandHandler
        var (operatorId, _, operatorRole) = GetOperator();
        var result = await Sender.Send(
            new TogglePatientStatusCommand(id, operatorId, operatorRole),
            ct
        );
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.ErrorCode == ErrorCode.Forbidden)
                return Forbid(result.Error ?? "无权切换该患者状态");
            return BusinessFail(result.Error ?? "切换状态失败");
        }

        return Success(result.Value, "状态已切换");
    }

    /// <summary>
    /// 恢复已删除的患者 — 仅 Admin（业务管理）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminBusinessOnly)]
    [HttpPost("{id}/restore")]
    public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error)
            return error;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new RestorePatientCommand(id, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "恢复失败");

        return Success(result.Value, "患者恢复成功");
    }

    /// <summary>
    /// 批量检查引用关系
    /// </summary>
    [HttpPost("batch-check-reference")]
    public async Task<IActionResult> BatchCheckReference(
        [FromBody] PatientBatchCheckReferenceInputDto dto,
        CancellationToken ct
    ) =>
        await ExecuteBatchCheckReferenceAsync(
            dto.PatientIds,
            ids => new BatchCheckPatientReferenceQuery(ids),
            "请至少选择一个患者",
            "批量检查最多支持100条",
            "批量检查失败",
            ct
        );

    /// <summary>
    /// 批量删除患者
    /// </summary>
    [HttpPost("batch-delete")]
    public override async Task<IActionResult> BatchDelete(
        [FromBody] BatchDeleteInputDto dto,
        CancellationToken ct
    ) =>
        await ExecuteBatchDeleteAsync(
            dto,
            (ids, operatorId) => new BatchDeletePatientsCommand(ids, operatorId),
            "请至少选择一个患者",
            "批量删除患者",
            ct
        );

    /// <summary>
    /// 批量导入患者（JSON）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport(
        [FromBody] PatientBatchImportInputDto request,
        CancellationToken ct
    )
    {
        if (request?.Patients == null || request.Patients.Count == 0)
        {
            return ValidationFail("导入列表不能为空");
        }

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(
            new BatchImportPatientsCommand(request.Patients, request.Strategy, operatorId),
            ct
        );
        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, useAuthMapping: true);
        }

        LogOperation(
            "批量导入患者",
            new { Count = request.Patients.Count, Strategy = request.Strategy },
            null
        );
        return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条患者");
    }
}
