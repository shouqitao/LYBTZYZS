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
        /// 创建诊疗记录
        /// </summary>
        /// <param name="dto">诊疗创建信息</param>
        /// <returns>创建的诊疗信息</returns>
        /// <remarks>
        /// ⚠️ 不推荐使用：请通过 MedicalCaseController 创建医疗案例，系统会自动创建关联的诊疗记录。
        /// MedicalCase 是聚合根，Consultation 作为其一部分使用共享主键模式（Consultation.Id == MedicalCase.Id）。
        /// 独立创建 Consultation 违反了聚合根架构原则。
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Obsolete("不推荐使用。请通过 POST /api/medicalcases 创建医疗案例，系统会自动创建诊疗记录。此端点仅保留用于向后兼容。", false)]
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
        /// 更新诊疗信息
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <param name="dto">诊疗更新信息</param>
        /// <returns>更新后的诊疗信息</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(404)]
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
        /// 删除诊疗记录（软删除）
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <returns>操作结果</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
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
        /// 获取诊疗统计数据 (Issue #1168)
        /// </summary>
        /// <param name="startDate">开始日期（可选）</param>
        /// <param name="endDate">结束日期（可选）</param>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationStatisticsDto>), 200)]
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