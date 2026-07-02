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

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PatientsController : BaseApiController
{
    private readonly ISender _sender;
    private readonly IPatientService _patientService;

    public PatientsController(
        ISender sender,
        IPatientService patientService,
        ILogger<PatientsController> logger) : base(logger)
    {
        _sender = sender;
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        var result = await _sender.Send(new GetPatientsQuery(page, pageSize, keyword));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "查询失败");
        return SuccessPaged(result.Value, "查询成功");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _sender.Send(new GetPatientQuery(id));
        if (!result.IsSuccess || result.Value == null)
            return NotFound(result.Error ?? "患者不存在");
        return Success(result.Value, "查询成功");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientInputDto dto)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new CreatePatientCommand(dto, operatorId));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "患者创建失败");
        return Success(result.Value, "患者创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto dto)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new UpdatePatientCommand(id, dto, operatorId));
        if (!result.IsSuccess || result.Value == null)
        {
            if (result.Error?.Contains("不存在") == true)
                return NotFound(result.Error);
            return BusinessFail(result.Error ?? "患者更新失败");
        }
        return Success(result.Value, "患者更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new DeletePatientCommand(id, operatorId));
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("医案记录") == true)
                return BusinessFail(result.Error);
            return NotFound("患者不存在");
        }
        return Success(true, "删除成功");
    }

    [HttpGet("by-id-number/{idNumber}")]
    public async Task<IActionResult> GetByIdNumber(string idNumber)
    {
        var result = await _patientService.SearchAsync(idNumber);
        if (result.IsSuccess && result.Data != null && result.Data.Count > 0)
        {
            var patient = result.Data.FirstOrDefault(p => p.IdNumber == idNumber);
            if (patient != null)
                return Success(patient);
        }
        return NotFound("未找到匹配的患者");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new BatchDeletePatientsCommand(request.Ids, operatorId));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "批量删除失败");
        return Success(result.Value, result.Value.Message);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var (operatorId, _, _) = GetOperator();
        var result = await _sender.Send(new TogglePatientStatusCommand(id, operatorId));
        if (!result.IsSuccess || result.Value == null)
            return BusinessFail(result.Error ?? "切换状态失败");
        return Success(result.Value, $"患者已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
    }
}
