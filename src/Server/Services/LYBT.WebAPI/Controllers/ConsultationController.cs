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
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "诊疗ID") is { } error) return error;

                var result = await _consultationService.GetByIdAsync(id);
                return HandleResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取诊疗详情", new { ConsultationId = id });
            }
        }

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        [HttpGet("medicalcase/{medicalCaseId}")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByMedicalCaseId(Guid medicalCaseId)
        {
            try
            {
                if (ValidateGuid(medicalCaseId, "医案ID") is { } error) return error;

                var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
                return HandleResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "根据医案ID获取诊疗记录", new { MedicalCaseId = medicalCaseId });
            }
        }
    }
}
