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

        // Issue #1562 Phase 4: 已删除 CreateConsultation（请使用 POST /api/medicalcases/with-details）

        // Issue #1562 Phase 4: 已删除 UpdateConsultation（请使用 PUT /api/medicalcases/{id}/consultation）

        // Issue #1562 Phase 4: 已删除 DeleteConsultation（请通过 DELETE /api/medicalcases/{id} 级联删除）

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
        /// 完成辩证步骤（Step 1）
        /// Issue #1598: REQ-001 - 三步工作流优化-Step1
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="request">Step1请求参数</param>
        /// <returns>Step1完成状态</returns>
        [HttpPost("{medicalCaseId}/complete-step1")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationStepDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<ConsultationStepDto>>> CompleteStep1(
            Guid medicalCaseId,
            [FromBody] CompleteStep1Request request)
        {
            try
            {
                var validationResult = ValidateGuid<ConsultationStepDto>(medicalCaseId, "医案ID");
                if (validationResult != null) return validationResult;

                var result = await _consultationService.CompleteStep1Async(medicalCaseId, request);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationStepDto>(ex, "完成Step1", new { MedicalCaseId = medicalCaseId });
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
    }
}