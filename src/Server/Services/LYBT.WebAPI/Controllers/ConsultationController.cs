using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 诊疗管理控制器 - 简化版（仅CRUD）
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/consultations")]
    [Authorize]
    public class ConsultationController : BaseApiController
    {
        private readonly IConsultationService _consultationService;

        public ConsultationController(IConsultationService consultationService, ILogger<ConsultationController> logger, IMemoryCache? cache = null)
            : base(logger, cache)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
        }

        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        /// <returns>分页的诊疗记录列表</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ConsultationDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<ConsultationDto>>>> GetConsultations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var result = await _consultationService.GetPagedAsync(page, pageSize, keyword);
                return HandlePagedServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<ConsultationDto>(ex, "获取诊疗记录列表");
            }
        }

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <returns>诊疗详情</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<ConsultationDto>(id, "诊疗ID");
                if (validationResult != null) return validationResult;

                var result = await _consultationService.GetByIdAsync(id);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "获取诊疗详情", new { ConsultationId = id });
            }
        }

        /// <summary>
        /// 创建诊疗记录（已废弃）
        /// </summary>
        /// <param name="dto">诊疗创建信息</param>
        /// <returns>创建的诊疗信息</returns>
        /// <remarks>
        /// ⚠️ 已废弃：请使用 POST /api/medicalcases/with-details 创建完整病案。Consultation模块仅提供查询功能。
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案。Consultation模块仅提供查询功能。", true)]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation([FromBody] ConsultationCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<ConsultationDto>();
                if (validationResult != null) return validationResult;

                var result = await _consultationService.CreateAsync(dto);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建诊疗记录", dto, result.Data.Id);
                    // Issue #1262: 添加 version 参数以匹配版本化路由
                    return CreatedAtAction(
                        nameof(GetById),
                        new { id = result.Data.Id, version = "1" },
                        ApiResponse<ConsultationDto>.CreateSuccess(result.Data));
                }
                
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "创建诊疗记录", dto);
            }
        }

        /// <summary>
        /// 更新诊疗信息（已废弃）
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <param name="dto">诊疗更新信息</param>
        /// <returns>更新后的诊疗信息</returns>
        /// <remarks>
        /// ⚠️ 已废弃：请使用 PUT /api/medicalcases/{id}/consultation 更新诊断信息。Consultation模块仅提供查询功能。
        /// </remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(404)]
        [Obsolete("请使用 PUT /api/medicalcases/{id}/consultation 更新诊断信息。Consultation模块仅提供查询功能。", true)]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(Guid id, [FromBody] ConsultationUpdateDto dto)
        {
            try
            {
                var guidValidationResult = ValidateGuid<ConsultationDto>(id, "诊疗ID");
                if (guidValidationResult != null) return guidValidationResult;

                var modelValidationResult = ValidateModel<ConsultationDto>();
                if (modelValidationResult != null) return modelValidationResult;

                var result = await _consultationService.UpdateAsync(id, dto);
                
                if (result.IsSuccess)
                {
                    LogOperation("更新诊疗记录", dto, id);
                }
                
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "更新诊疗记录", new { ConsultationId = id, UpdateData = dto });
            }
        }

        /// <summary>
        /// 删除诊疗记录（已废弃）
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <returns>操作结果</returns>
        /// <remarks>
        /// ⚠️ 已废弃：请通过 DELETE /api/medicalcases/{id} 删除病案（级联删除）。Consultation模块仅提供查询功能。
        /// </remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        [Obsolete("请通过 DELETE /api/medicalcases/{id} 删除病案（级联删除）。Consultation模块仅提供查询功能。", true)]
        public async Task<ActionResult<ApiResponse>> DeleteConsultation(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "诊疗ID");
                if (validationResult != null) return validationResult;

                var result = await _consultationService.DeleteAsync(id);

                if (result.IsSuccess)
                {
                    LogOperation("删除诊疗记录", null, id);
                }

                return HandleServiceResult(result, "诊疗记录删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除诊疗记录", new { ConsultationId = id });
            }
        }

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>诊疗记录列表</returns>
        [HttpGet("medicalcase/{medicalCaseId}")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> GetByMedicalCaseId(Guid medicalCaseId)
        {
            try
            {
                var validationResult = ValidateGuid<List<ConsultationDto>>(medicalCaseId, "医案ID");
                if (validationResult != null) return validationResult;

                var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<ConsultationDto>>(ex, "根据医案ID获取诊疗记录", new { MedicalCaseId = medicalCaseId });
            }
        }

        /// <summary>
        /// 搜索诊疗记录
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的诊疗记录列表</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> Search([FromQuery] string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest(ApiResponse<List<ConsultationDto>>.CreateFail("搜索关键词不能为空"));
                }

                var result = await _consultationService.SearchAsync(keyword);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<ConsultationDto>>(ex, "搜索诊疗记录", new { Keyword = keyword });
            }
        }


        /// <summary>
        /// 获取诊疗统计数据（已废弃 - MVP过度开发）
        /// </summary>
        /// <param name="startDate">开始日期（可选）</param>
        /// <param name="endDate">结束日期（可选）</param>
        /// <remarks>
        /// ⚠️ 已废弃：统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。
        /// </remarks>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationStatisticsDto>), 200)]
        [Obsolete("统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。", true)]
        public async Task<ActionResult<ApiResponse<ConsultationStatisticsDto>>> GetStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // 日期范围验证
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return ValidationFail<ConsultationStatisticsDto>("开始日期不能晚于结束日期");
                }

                var result = await _consultationService.GetStatisticsAsync(startDate, endDate);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationStatisticsDto>(ex, "获取诊疗统计", new { startDate, endDate });
            }
        }
    }
}