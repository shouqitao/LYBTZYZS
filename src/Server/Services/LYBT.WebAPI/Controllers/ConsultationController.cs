using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 看诊管理控制器 - 中医四诊核心模块
/// UltraThink v2.0: 支持完整的中医诊疗流程
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/consultations")]
[Authorize]
public class ConsultationController : BaseApiController
{
    private readonly IConsultationService _consultationService;

    public ConsultationController(
        IConsultationService consultationService,
        ILogger<ConsultationController> logger,
        IMemoryCache cache) : base(logger, cache)
    {
        _consultationService = consultationService;
    }

    /// <summary>
    /// 根据ID获取看诊详情 - 统一API响应格式
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ConsultationDetailDto>>> GetById(Guid id)
    {
        try
        {
            var validation = ValidateGuid<ConsultationDetailDto>(id, "看诊ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _consultationService.GetByIdAsync(id);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<ConsultationDetailDto>(ex, "获取看诊详情", id);
        }
    }

    /// <summary>
    /// 分页查询看诊记录 - 统一API响应格式
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ConsultationDto>>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        try
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return ValidationFailPaged<ConsultationDto>("页码和页大小参数无效（页码>0，页大小1-100）");
            }

            var query = new PagedQueryBaseDto
            {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword
            };

            var result = await _consultationService.GetPagedAsync(query);
            return HandlePagedServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleExceptionPaged<ConsultationDto>(ex, "获取看诊列表", new { page, pageSize, keyword });
        }
    }

    /// <summary>
    /// 开始看诊 - 统一API响应格式
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> StartConsultation([FromBody] ConsultationStartDto dto)
    {
        try
        {
            var validation = ValidateModel<ConsultationDto>();
            if (validation != null)
            {
                return validation;
            }

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _consultationService.StartAsync(dto);

            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("开始看诊", result.Data, result.Data.Id);
            }

            return HandleServiceResult(result, "看诊开始成功");
        }
        catch (Exception ex)
        {
            return HandleException<ConsultationDto>(ex, "开始看诊", dto);
        }
    }

    /// <summary>
    /// 更新看诊记录 - 统一API响应格式
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> Update(Guid id, [FromBody] ConsultationDetailDto dto)
    {
        try
        {
            var idValidation = ValidateGuid<ConsultationDto>(id, "看诊ID");
            if (idValidation != null)
            {
                return idValidation;
            }

            var modelValidation = ValidateModel<ConsultationDto>();
            if (modelValidation != null)
            {
                return modelValidation;
            }

            var result = await _consultationService.UpdateAsync(id, dto);
            if (result.IsSuccess && result.Data != null)
            {
                LogOperation("更新看诊记录", result.Data, id);
            }

            return HandleServiceResult(result, "看诊记录更新成功");
        }
        catch (Exception ex)
        {
            return HandleException<ConsultationDto>(ex, "更新看诊记录", new { id, dto });
        }
    }

    /// <summary>
    /// 根据患者ID获取看诊历史 - 统一API响应格式
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> GetByPatientId(Guid patientId)
    {
        try
        {
            var validation = ValidateGuid<List<ConsultationDto>>(patientId, "患者ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _consultationService.GetByPatientIdAsync(patientId);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<ConsultationDto>>(ex, "获取患者看诊历史", patientId);
        }
    }

    /// <summary>
    /// 根据医疗案例ID获取看诊记录 - 统一API响应格式
    /// </summary>
    [HttpGet("medical-case/{medicalCaseId}")]
    public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> GetByMedicalCaseId(Guid medicalCaseId)
    {
        try
        {
            var validation = ValidateGuid<List<ConsultationDto>>(medicalCaseId, "医疗案例ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<ConsultationDto>>(ex, "获取医疗案例看诊记录", medicalCaseId);
        }
    }

    /// <summary>
    /// 根据医生ID获取看诊记录 - 统一API响应格式
    /// </summary>
    [HttpGet("doctor/{doctorId}")]
    public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> GetByDoctorId(Guid doctorId)
    {
        try
        {
            var validation = ValidateGuid<List<ConsultationDto>>(doctorId, "医生ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _consultationService.GetByDoctorIdAsync(doctorId);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<ConsultationDto>>(ex, "获取医生看诊记录", doctorId);
        }
    }

    /// <summary>
    /// 搜索看诊记录 - 统一API响应格式
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> Search([FromQuery] string keyword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ValidationFail<List<ConsultationDto>>("搜索关键词不能为空");
            }

            var result = await _consultationService.SearchAsync(keyword);
            return HandleServiceResult(result, $"搜索完成，找到{result.Data?.Count ?? 0}条记录");
        }
        catch (Exception ex)
        {
            return HandleException<List<ConsultationDto>>(ex, "搜索看诊记录", keyword);
        }
    }

    /// <summary>
    /// 获取患者历史就诊记录 - 统一API响应格式
    /// </summary>
    [HttpGet("patient/{patientId}/history")]
    public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> GetPatientHistory(Guid patientId)
    {
        try
        {
            var validation = ValidateGuid<List<ConsultationDto>>(patientId, "患者ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _consultationService.GetPatientHistoryAsync(patientId);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<ConsultationDto>>(ex, "获取患者历史就诊记录", patientId);
        }
    }


    /// <summary>
    /// 删除看诊记录（软删除） - 统一API响应格式
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        try
        {
            var validation = ValidateGuid(id, "看诊ID");
            if (validation != null)
            {
                return validation;
            }

            var result = await _consultationService.DeleteAsync(id);
            return HandleBoolServiceResult(result, "删除成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "删除看诊记录", id);
        }
    }
}
