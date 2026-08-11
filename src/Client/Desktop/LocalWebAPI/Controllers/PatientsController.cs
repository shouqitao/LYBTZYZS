using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Excel;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
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
    /// 获取患者列表 - 支持分页和查询
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

        var isAdmin = User?.IsInRole(RoleConstants.Admin) == true || User?.IsInRole(RoleConstants.SuperAdmin) == true;

        var result = await _patientService.GetPagedAsync(page, pageSize, keyword, filterDisabled: !isAdmin, ct);
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");

        return SuccessPaged(result.Value, "查询成功");
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
    /// 新增患者
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientInputDto input, CancellationToken ct)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new CreatePatientCommand(input, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return BusinessFail(result.Error ?? "创建失败");
        }

        LogOperation("新增患者成功", result.Value, null);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.Id },
            ApiResponse<PatientDetailDto>.CreateSuccess(result.Value, "患者创建成功"));
    }

    /// <summary>
    /// 更新患者信息
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto input, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error) return error;

        var getResult = await _patientService.GetByIdAsync(id, ct);
        if (!getResult.IsSuccess || getResult.Value == null)
            return NotFound("患者不存在");
        if (ValidateOwnership(getResult.Value.CreatedBy, "患者") is { } ownerError)
            return ownerError;

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new UpdatePatientCommand(id, input, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "更新失败");
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
        /// 下载患者导入模板（T4 P0#4: 镜像远程端点）
        /// </summary>
        [HttpGet("import-template")]
        public IActionResult ImportTemplate()
        {
            var headers = new[] { "姓名", "性别", "出生日期", "身份证号", "手机号", "拼音码" };
            var sample = new[] { "张三", "Male", "1990-01-01", "110101199001010011", "13800138000", "zhangsan" };
            var bytes = ExcelExportHelper.CreateWorkbook("患者导入模板", headers, new[] { sample });
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "患者导入模板.xlsx");
        }

        /// <summary>
        /// 导出患者数据（T4 P0#4）
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string? keyword = null, CancellationToken ct = default)
        {
            var result = await _patientService.GetPagedAsync(1, 10000, keyword, filterDisabled: false, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "导出失败");

            var headers = new[] { "姓名", "性别", "年龄", "手机号", "拼音码", "状态" };
            var rows = result.Value.Items.Select(pat => new[]
            {
                pat.Name,
                pat.Gender.ToString(),
                pat.Age?.ToString() ?? string.Empty,
                pat.PhoneNumber ?? string.Empty,
                pat.PinYinCode ?? string.Empty,
                pat.Status.ToString()
            });
            var bytes = ExcelExportHelper.CreateWorkbook("患者数据", headers, rows);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "患者数据.xlsx");
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
    /// 恢复已删除的患者 — 仅 Admin（业务管理）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminBusinessOnly)]
    [HttpPost("{id:guid}/restore")]
    public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        if (ValidateGuid(id, "患者ID") is { } error) return error;

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
    public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)
        => await ExecuteBatchCheckReferenceAsync(
            dto.PatientIds,
            ids => new BatchCheckPatientReferenceQuery(ids),
            "请至少选择一个患者",
            "批量检查最多支持100条",
            "批量检查失败",
            ct);

    /// <summary>
    /// 批量删除患者
    /// </summary>
    [HttpPost("batch-delete")]
    public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        => await ExecuteBatchDeleteAsync(
            dto,
            (ids, operatorId) => new BatchDeletePatientsCommand(ids, operatorId),
            "请至少选择一个患者",
            "批量删除患者",
            ct);

    /// <summary>
    /// 批量导入患者（JSON）
    /// </summary>
    [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] PatientBatchImportInputDto request, CancellationToken ct)
    {
        if (request?.Patients == null || request.Patients.Count == 0)
        {
            return ValidationFail("导入列表不能为空");
        }

        var (operatorId, _, _) = GetOperator();
        var result = await Sender.Send(new BatchImportPatientsCommand(request.Patients, request.Strategy, operatorId), ct);
        if (!result.IsSuccess || result.Value == null)
        {
            return BusinessFail(result.Error ?? "导入失败");
        }

        LogOperation("批量导入患者", new { Count = request.Patients.Count, Strategy = request.Strategy }, null);
        return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条患者");
    }
}
