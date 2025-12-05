using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 处方管理控制器 - 只读查询层
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-012)
    /// 职责：提供处方的只读查询和搜索功能
    /// 所有写操作必须使用 MedicalCaseController（聚合根入口）
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PrescriptionsController : BaseApiController
    {
        private readonly IPrescriptionService _service;

        public PrescriptionsController(IPrescriptionService service, ILogger<PrescriptionsController> logger)
            : base(logger)
        {
            _service = service;
        }

        /// <summary>
        /// 获取处方详情（含药材明细）
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "处方ID") is { } error) return error;

                var result = await _service.GetByIdAsync(id);
                return HandleResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取处方详情", new { PrescriptionId = id });
            }
        }

        /// <summary>
        /// 根据病案ID获取处方列表
        /// </summary>
        [HttpGet("medicalcase/{medicalCaseId}")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByMedicalCaseId(Guid medicalCaseId)
        {
            try
            {
                if (ValidateGuid(medicalCaseId, "病案ID") is { } error) return error;

                var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);
                return HandleResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "根据病案ID获取处方列表", new { MedicalCaseId = medicalCaseId });
            }
        }

        /// <summary>
        /// 搜索处方 - 按患者姓名或病症关键字（REQ-2：按病症查询处方）
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        public async Task<IActionResult> Search(
            [FromQuery] string? patientName = null,
            [FromQuery] string? symptomKeyword = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    return ValidationFail("请至少提供一个搜索条件（患者姓名或病症关键字）");
                }

                var result = await _service.SearchPrescriptionsAsync(patientName, symptomKeyword);
                return HandleResult(result, "搜索成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "搜索处方", new { PatientName = patientName, SymptomKeyword = symptomKeyword });
            }
        }

        /// <summary>
        /// 获取患者最近处方列表（REQ-1：按患者查询处方）
        /// </summary>
        [HttpGet("patient/{patientId}/recent")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRecentByPatient(
            Guid patientId,
            [FromQuery] int count = 5)
        {
            try
            {
                if (ValidateGuid(patientId, "患者ID") is { } error) return error;

                if (count < 1 || count > 20)
                {
                    return ValidationFail("返回数量必须在1-20之间");
                }

                var result = await _service.GetPatientRecentPrescriptionsAsync(patientId, count);
                return HandleResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取患者最近处方", new { PatientId = patientId, Count = count });
            }
        }
    }
}
