using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Consultations.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 诊疗管理控制器 - 只读查询层
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-012)
    /// 职责：提供诊疗记录的只读查询功能
    /// 所有写操作必须使用 MedicalCaseController（聚合根入口）
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/consultations")]
    [Authorize]
    public class ConsultationController : BaseApiController
    {
        private readonly IConsultationService _consultationService;

        public ConsultationController(
            IConsultationService consultationService,
            ILogger<ConsultationController> logger)
            : base(logger)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
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

        // ========== Write方法已移除（Issue #1600 Phase 4）==========
        // CompleteStep1 已删除，请使用 POST /api/v1/medicalcases/{id}/complete-step1
    }
}
