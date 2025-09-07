using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 医疗案例管理控制器 - 诊疗流程聚合根
/// UltraThink v2.0: 统一管理整个诊疗流程，包含完整病历记录
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class MedicalCaseController : BaseApiController
{
    private readonly IMedicalCaseService _medicalCaseService;

    public MedicalCaseController(
        IMedicalCaseService medicalCaseService,
        ILogger<MedicalCaseController> logger,
        IMemoryCache cache) : base(logger, cache)
    {
        _medicalCaseService = medicalCaseService;
    }

    /// <summary>
    /// 根据ID获取医疗案例详情 - 统一API响应格式
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> GetById(Guid id)
    {
        try
        {
            var validation = ValidateGuid<MedicalCaseDetailDto>(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.GetByIdAsync(id);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<MedicalCaseDetailDto>(ex, "获取医疗案例详情", id);
        }
    }

    /// <summary>
    /// 分页查询医疗案例 - 统一API响应格式
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MedicalCaseDto>>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        try
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return ValidationFailPaged<MedicalCaseDto>("页码和页大小参数无效（页码>0，页大小1-100）");
            }

            var query = new PagedQueryBaseDto
            {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword
            };

            var result = await _medicalCaseService.GetPagedAsync(query);
            return HandlePagedServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleExceptionPaged<MedicalCaseDto>(ex, "获取医疗案例列表", new { page, pageSize, keyword });
        }
    }

    /// <summary>
    /// 创建新的医疗案例 - 统一API响应格式
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> Create([FromBody] MedicalCaseCreateDto dto)
    {
        try
        {
            var validation = ValidateModel<MedicalCaseDto>();
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.CreateAsync(dto);
            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("创建医疗案例", result.Data, result.Data.Id);
            }
            return HandleServiceResult(result, "医疗案例创建成功");
        }
        catch (Exception ex)
        {
            return HandleException<MedicalCaseDto>(ex, "创建医疗案例", dto);
        }
    }

    /// <summary>
    /// 更新医疗案例 - 统一API响应格式
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> Update(Guid id, [FromBody] MedicalCaseUpdateDto dto)
    {
        try
        {
            var idValidation = ValidateGuid<MedicalCaseDto>(id, "医疗案例ID");
            if (idValidation != null)
            {
                return idValidation;
            }

            var modelValidation = ValidateModel<MedicalCaseDto>();
            if (modelValidation != null)
            {
                return modelValidation;
            }

            var result = await _medicalCaseService.UpdateAsync(id, dto);
            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("更新医疗案例", result.Data, id);
            }
            return HandleServiceResult(result, "医疗案例更新成功");
        }
        catch (Exception ex)
        {
            return HandleException<MedicalCaseDto>(ex, "更新医疗案例", new { id, dto });
        }
    }

    /// <summary>
    /// 根据患者ID获取医疗案例 - 统一API响应格式
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<ApiResponse<List<MedicalCaseDto>>>> GetByPatientId(Guid patientId)
    {
        try
        {
            var validation = ValidateGuid<List<MedicalCaseDto>>(patientId, "患者ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.GetByPatientIdAsync(patientId);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<MedicalCaseDto>>(ex, "获取患者医疗案例", patientId);
        }
    }

    /// <summary>
    /// 获取患者的活跃医疗案例 - 统一API响应格式
    /// </summary>
    [HttpGet("patient/{patientId}/active")]
    public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> GetActiveByPatientId(Guid patientId)
    {
        try
        {
            var validation = ValidateGuid<MedicalCaseDto>(patientId, "患者ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.GetActiveByPatientIdAsync(patientId);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<MedicalCaseDto>(ex, "获取患者活跃医疗案例", patientId);
        }
    }

    /// <summary>
    /// 完成医疗案例 - 统一API响应格式
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult<ApiResponse>> Complete(Guid id, [FromBody] CompleteMedicalCaseDto dto)
    {
        try
        {
            var validation = ValidateGuid(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.CompleteAsync(id, dto.CompletionReason ?? "医疗案例完成");
            return HandleBoolServiceResult(result, "医疗案例完成成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "完成医疗案例", new { id, dto });
        }
    }

    /// <summary>
    /// 暂停医疗案例 - 统一API响应格式
    /// </summary>
    [HttpPost("{id}/suspend")]
    public async Task<ActionResult<ApiResponse>> Suspend(Guid id, [FromBody] SuspendMedicalCaseDto dto)
    {
        try
        {
            var validation = ValidateGuid(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.Suspend(id, dto.Reason ?? "暂停医疗案例");
            return HandleBoolServiceResult(result, "医疗案例暂停成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "暂停医疗案例", new { id, dto });
        }
    }

    /// <summary>
    /// 恢复医疗案例 - 统一API响应格式
    /// </summary>
    [HttpPost("{id}/resume")]
    public async Task<ActionResult<ApiResponse>> Resume(Guid id)
    {
        try
        {
            var validation = ValidateGuid(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.Resume(id);
            return HandleBoolServiceResult(result, "医疗案例恢复成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "恢复医疗案例", id);
        }
    }

    /// <summary>
    /// 更新医疗案例状态 - 统一API响应格式
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateMedicalCaseStatusDto dto)
    {
        try
        {
            var validation = ValidateGuid(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.UpdateStatus(id, dto.Status);
            return HandleBoolServiceResult(result, "状态更新成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "更新医疗案例状态", new { id, dto });
        }
    }

    /// <summary>
    /// 归档医疗案例 - 统一API响应格式
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<ActionResult<ApiResponse>> Archive(Guid id, [FromBody] ArchiveMedicalCaseDto dto)
    {
        try
        {
            var validation = ValidateGuid(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.Archive(id, dto.ArchiveReason ?? "归档医疗案例");
            return HandleBoolServiceResult(result, "医疗案例归档成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "归档医疗案例", new { id, dto });
        }
    }

    /// <summary>
    /// 搜索医疗案例 - 统一API响应格式
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<MedicalCaseDto>>>> Search([FromQuery] string keyword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ValidationFail<List<MedicalCaseDto>>("搜索关键词不能为空");
            }

            var result = await _medicalCaseService.SearchAsync(keyword);
            return HandleServiceResult(result, $"搜索完成，找到{result.Data?.Count ?? 0}条记录");
        }
        catch (Exception ex)
        {
            return HandleException<List<MedicalCaseDto>>(ex, "搜索医疗案例", keyword);
        }
    }

    /// <summary>
    /// 获取医疗案例统计信息 - 统一API响应格式
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<object>>> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _medicalCaseService.GetStatistics(startDate, endDate);
            return HandleServiceResult(result, "获取统计信息成功");
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, "获取医疗案例统计信息", new { startDate, endDate });
        }
    }

    /// <summary>
    /// 获取医疗案例历史记录 - 统一API响应格式
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetHistory(Guid id)
    {
        try
        {
            var validation = ValidateGuid<List<object>>(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.GetHistory(id);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<object>>(ex, "获取医疗案例历史记录", id);
        }
    }

    /// <summary>
    /// 删除医疗案例（软删除） - 统一API响应格式
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        try
        {
            var validation = ValidateGuid(id, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _medicalCaseService.DeleteAsync(id);
            return HandleBoolServiceResult(result, "删除成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "删除医疗案例", id);
        }
    }
}

// 辅助DTO类
public class CompleteMedicalCaseDto
{
    public string? CompletionReason { get; set; }
}

public class SuspendMedicalCaseDto
{
    public string? Reason { get; set; }
}

public class UpdateMedicalCaseStatusDto
{
    public int Status { get; set; }
}

public class ArchiveMedicalCaseDto
{
    public string? ArchiveReason { get; set; }
}
