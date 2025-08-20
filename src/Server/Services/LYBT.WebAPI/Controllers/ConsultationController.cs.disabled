using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Shared.Models.Enums;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 看诊管理控制器 - 统一API响应格式
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
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
        /// 分页查询看诊记录 - 统一API响应格式
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.PagedApiResponse<ConsultationDto>>> GetConsultations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null,
            [FromQuery] Guid? doctorId = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? status = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<ConsultationDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var query = new ConsultationPagedQueryDto
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword,
                    DoctorId = doctorId,
                    PatientId = patientId,
                    StartDate = startDate,
                    EndDate = endDate,
                    // Status 不在 ConsultationPagedQueryDto 中，可能需要扩展或使用其他参数
                };

                var result = await _consultationService.GetPagedAsync(query);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<ConsultationDto>(ex, "分页查询看诊记录", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 获取看诊详情 - 统一API响应格式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<ConsultationDetailDto>>> GetById(Guid id)
        {
            try
            {
                var validation = ValidateGuid<ConsultationDetailDto>(id, "看诊记录ID");
                if (validation != null) return validation;

                var result = await _consultationService.GetByIdAsync(id);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDetailDto>(ex, "获取看诊详情", id);
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊信息 - 统一API响应格式
        /// </summary>
        [HttpGet("medical-case/{medicalCaseId}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<ConsultationDetailDto>>> GetByMedicalCaseId(Guid medicalCaseId)
        {
            try
            {
                var validation = ValidateGuid<ConsultationDetailDto>(medicalCaseId, "医疗案例ID");
                if (validation != null) return validation;

                var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
                if (!result.IsSuccess || result.Data == null || result.Data.Count == 0)
                {
                    return NotFound<ConsultationDetailDto>("看诊记录不存在", ApiErrorCodes.CONSULTATION_NOT_FOUND);
                }
                // 取第一个看诊记录转换为详细信息
                var detailDto = new ConsultationDetailDto
                {
                    Id = result.Data[0].Id,
                    PatientId = result.Data[0].PatientId,
                    DoctorId = result.Data[0].UserId, // ConsultationDto使用UserId
                    Status = Enum.TryParse<ConsultationStatus>(result.Data[0].Status, out var status) ? status : ConsultationStatus.InProgress,
                    CreateTime = DateTime.Now // 使用当前时间作为默认值
                };
                return Success(detailDto, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDetailDto>(ex, "根据医疗案例ID获取看诊信息", medicalCaseId);
            }
        }

        /// <summary>
        /// 开始看诊 - 统一API响应格式
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<ConsultationDetailDto>>> StartConsultation([FromBody] ConsultationStartDto dto)
        {
            try
            {
                var validation = ValidateModel<ConsultationDetailDto>();
                if (validation != null) return validation;

                var result = await _consultationService.StartAsync(dto);
                // 将ConsultationDto转换为ConsultationDetailDto
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<ConsultationDetailDto>("开始看诊失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }
                var detailDto = new ConsultationDetailDto
                {
                    Id = result.Data.Id,
                    PatientId = result.Data.PatientId,
                    DoctorId = result.Data.UserId, // ConsultationDto使用UserId
                    Status = Enum.TryParse<ConsultationStatus>(result.Data.Status, out var status) ? status : ConsultationStatus.InProgress,
                    CreateTime = DateTime.Now
                };
                return Success(detailDto, "看诊已开始");
            }
            catch (InvalidOperationException ex)
            {
                return BusinessFail<ConsultationDetailDto>(ex.Message, ApiErrorCodes.DATA_UPDATE_FAILED);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDetailDto>(ex, "开始看诊", dto);
            }
        }

        /// <summary>
        /// 更新看诊信息 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<ConsultationDetailDto>>> UpdateConsultation(Guid id, [FromBody] ConsultationUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<ConsultationDetailDto>(id, "看诊记录ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<ConsultationDetailDto>();
                if (modelValidation != null) return modelValidation;

                // 先获取现有记录，然后更新
                var existingResult = await _consultationService.GetByIdAsync(id);
                if (!existingResult.IsSuccess || existingResult.Data == null)
                {
                    return NotFound<ConsultationDetailDto>("看诊记录不存在", ApiErrorCodes.CONSULTATION_NOT_FOUND);
                }
                // 更新现有记录的字段（ConsultationUpdateDto只包含某些字段）
                if (!string.IsNullOrEmpty(dto.ChiefComplaint)) existingResult.Data.ChiefComplaint = dto.ChiefComplaint;
                if (!string.IsNullOrEmpty(dto.Diagnosis)) existingResult.Data.Diagnosis = dto.Diagnosis;
                if (!string.IsNullOrEmpty(dto.Remark)) existingResult.Data.Remark = dto.Remark;
                // 更新时间
                existingResult.Data.UpdateTime = DateTime.Now;
                var updateDto = existingResult.Data;
                var result = await _consultationService.UpdateAsync(id, updateDto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<ConsultationDetailDto>("更新看诊信息失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }
                var detailDto = new ConsultationDetailDto
                {
                    Id = result.Data.Id,
                    PatientId = result.Data.PatientId,
                    DoctorId = result.Data.UserId, // ConsultationDto使用UserId
                    Status = Enum.TryParse<ConsultationStatus>(result.Data.Status, out var updateStatus) ? updateStatus : ConsultationStatus.InProgress,
                    CreateTime = DateTime.Now
                };
                return Success(detailDto, "看诊信息更新成功");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("不存在"))
            {
                return NotFound<ConsultationDetailDto>(ex.Message, ApiErrorCodes.CONSULTATION_NOT_FOUND);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDetailDto>(ex, "更新看诊信息", new { id, dto });
            }
        }

        /// <summary>
        /// 完成看诊 - 统一API响应格式
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> CompleteConsultation(Guid id, [FromBody] ConsultationCompleteDto dto)
        {
            try
            {
                var idValidation = ValidateGuid(id, "看诊记录ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel();
                if (modelValidation != null) return modelValidation;

                var result = await _consultationService.CompleteConsultationAsync(id, dto);
                if (!result.IsSuccess || !result.Data)
                {
                    return BusinessFail("完成看诊操作失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }
                
                LogOperation("完成看诊", null, id);
                return Success("看诊完成");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("不存在"))
            {
                return NotFound(ex.Message, ApiErrorCodes.CONSULTATION_NOT_FOUND);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "完成看诊", new { id, dto });
            }
        }

        /// <summary>
        /// 获取医生今日看诊列表 - 统一API响应格式
        /// </summary>
        [HttpGet("doctor/{doctorId}/today")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<ConsultationDto>>>> GetTodayConsultationsByDoctor(Guid doctorId)
        {
            try
            {
                var validation = ValidateGuid<List<ConsultationDto>>(doctorId, "医生ID");
                if (validation != null) return validation;

                // 使用现有方法获取医生的所有看诊记录，然后过滤今日的
                var result = await _consultationService.GetByDoctorIdAsync(doctorId);
                if (!result.IsSuccess || result.Data == null)
                {
                    return HandleServiceResult(ServiceResult<List<ConsultationDto>>.Failure(result.ErrorMessage ?? "获取医生看诊记录失败"));
                }
                // 过滤今日的记录
                var today = DateTime.Today;
                var todayConsultations = result.Data.Where(c => c.ConsultationTime.Date == today).ToList();
                return Success(todayConsultations, $"查询成功，共{todayConsultations.Count}条今日看诊记录");
            }
            catch (Exception ex)
            {
                return HandleException<List<ConsultationDto>>(ex, "获取医生今日看诊列表", doctorId);
            }
        }

        /// <summary>
        /// 获取患者历史看诊记录 - 统一API响应格式
        /// </summary>
        [HttpGet("patient/{patientId}/history")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<ConsultationDto>>>> GetPatientHistory(Guid patientId)
        {
            try
            {
                var validation = ValidateGuid<List<ConsultationDto>>(patientId, "患者ID");
                if (validation != null) return validation;

                // 使用现有方法获取患者的所有看诊记录
                var result = await _consultationService.GetByPatientIdAsync(patientId);
                return HandleServiceResult(result, result.IsSuccess ? $"查询成功，共{result.Data?.Count ?? 0}条历史看诊记录" : "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<ConsultationDto>>(ex, "获取患者历史看诊记录", patientId);
            }
        }

        /// <summary>
        /// 统计医生看诊数量 - 统一API响应格式
        /// </summary>
        [HttpGet("doctor/{doctorId}/count")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> GetDoctorConsultationCount(
            Guid doctorId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var validation = ValidateGuid<object>(doctorId, "医生ID");
                if (validation != null) return validation;

                // 使用现有方法获取医生所有看诊记录，然后根据日期范围过滤并计算数量
                var consultationsResult = await _consultationService.GetByDoctorIdAsync(doctorId);
                if (!consultationsResult.IsSuccess || consultationsResult.Data == null)
                {
                    return HandleServiceResult<object>(ServiceResult<object>.Failure(consultationsResult.ErrorMessage ?? "获取看诊记录失败"));
                }
                
                // 根据日期范围过滤
                var consultations = consultationsResult.Data.AsQueryable();
                if (startDate.HasValue)
                {
                    consultations = consultations.Where(c => c.ConsultationTime >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    consultations = consultations.Where(c => c.ConsultationTime <= endDate.Value.AddDays(1));
                }
                
                var count = consultations.Count();
                var result = new { count };
                return Success<object>(result, $"看诊数量：{count}");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "统计医生看诊数量", new { doctorId, startDate, endDate });
            }
        }

        /// <summary>
        /// 更新看诊状态 - 统一API响应格式
        /// </summary>
        [HttpPost("{id}/update-status")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<ConsultationDetailDto>>> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<ConsultationDetailDto>(id, "看诊记录ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<ConsultationDetailDto>();
                if (modelValidation != null) return modelValidation;

                // 先获取现有的看诊记录
                var existingResult = await _consultationService.GetByIdAsync(id);
                if (!existingResult.IsSuccess || existingResult.Data == null)
                {
                    return NotFound<ConsultationDetailDto>("看诊记录不存在", ApiErrorCodes.CONSULTATION_NOT_FOUND);
                }
                
                // 更新状态
                existingResult.Data.Status = dto.Status;
                var result = await _consultationService.UpdateAsync(id, existingResult.Data);
                
                LogOperation("更新看诊状态", dto, id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<ConsultationDetailDto>("状态更新失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }
                
                // 转换为ConsultationDetailDto
                var detailDto = new ConsultationDetailDto
                {
                    Id = result.Data.Id,
                    PatientId = result.Data.PatientId,
                    DoctorId = result.Data.UserId, // ConsultationDto使用UserId
                    Status = Enum.TryParse<ConsultationStatus>(result.Data.Status, out var resultStatus) ? resultStatus : ConsultationStatus.InProgress,
                    CreateTime = DateTime.Now
                };
                return Success(detailDto, "状态更新成功");
            }
            catch (InvalidOperationException ex)
            {
                return BusinessFail<ConsultationDetailDto>(ex.Message, ApiErrorCodes.DATA_UPDATE_FAILED);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDetailDto>(ex, "更新看诊状态", new { id, dto });
            }
        }

        /// <summary>
        /// 删除看诊记录（软删除） - 统一API响应格式
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "看诊记录ID");
                if (validation != null) return validation;

                var result = await _consultationService.DeleteAsync(id);
                if (!result.IsSuccess || !result.Data)
                {
                    return NotFound("看诊记录不存在", ApiErrorCodes.CONSULTATION_NOT_FOUND);
                }
                
                LogOperation("删除看诊记录", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除看诊记录", id);
            }
        }
    }
}