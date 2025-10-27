using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 处方管理 API - 基础CRUD功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PrescriptionsController : BaseApiController
    {
        private readonly IPrescriptionService _service;

        public PrescriptionsController(IPrescriptionService service, IMemoryCache cache, ILogger<PrescriptionsController> logger)
            : base(logger, cache)
        {
            _service = service;
        }

        /// <summary>
        /// 获取处方详情（含药材明细）
        /// </summary>
        /// <param name="id">处方ID</param>
        /// <returns>处方详情</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (validationResult != null) return validationResult;

                var result = await _service.GetByIdAsync(id);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "获取处方详情", new { PrescriptionId = id });
            }
        }

        /// <summary>
        /// 根据病案ID获取处方列表
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>处方列表</returns>
        [HttpGet("medicalcase/{medicalCaseId}")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetByMedicalCaseId(Guid medicalCaseId)
        {
            try
            {
                var validationResult = ValidateGuid<List<PrescriptionDto>>(medicalCaseId, "病案ID");
                if (validationResult != null) return validationResult;

                var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionDto>>(ex, "根据病案ID获取处方列表", new { MedicalCaseId = medicalCaseId });
            }
        }

        /// <summary>
        /// 搜索处方 - 按患者姓名或病症关键字（REQ-2：按病症查询处方）
        /// </summary>
        /// <param name="patientName">患者姓名关键字（可空）</param>
        /// <param name="symptomKeyword">病症关键字（可空，匹配中医诊断和主诉）</param>
        /// <returns>处方搜索结果列表</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> Search(
            [FromQuery] string? patientName = null,
            [FromQuery] string? symptomKeyword = null)
        {
            try
            {
                // 至少提供一个搜索条件
                if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    return BadRequest(ApiResponse<List<PrescriptionSearchResultDto>>.CreateFail("请至少提供一个搜索条件（患者姓名或病症关键字）"));
                }

                var result = await _service.SearchPrescriptionsAsync(patientName, symptomKeyword);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionSearchResultDto>>(ex, "搜索处方", new { PatientName = patientName, SymptomKeyword = symptomKeyword });
            }
        }

        /// <summary>
        /// 获取患者最近处方列表（REQ-1：按患者查询处方）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5条，最大20条）</param>
        /// <returns>患者最近处方列表（按日期倒序）</returns>
        [HttpGet("patient/{patientId}/recent")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> GetRecentByPatient(
            Guid patientId,
            [FromQuery] int count = 5)
        {
            try
            {
                var validationResult = ValidateGuid<List<PrescriptionSearchResultDto>>(patientId, "患者ID");
                if (validationResult != null) return validationResult;

                // 验证count范围
                if (count < 1 || count > 20)
                {
                    return BadRequest(ApiResponse<List<PrescriptionSearchResultDto>>.CreateFail("返回数量必须在1-20之间"));
                }

                var result = await _service.GetPatientRecentPrescriptionsAsync(patientId, count);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionSearchResultDto>>(ex, "获取患者最近处方", new { PatientId = patientId, Count = count });
            }
        }

        // ========== Write方法已移除（Issue #1600 Phase 4）==========
        // PhysicalDelete 已删除，请使用 DELETE /api/v1/medicalcases/{id}
        // SoftDelete 已删除，请使用 DELETE /api/v1/medicalcases/{id}/soft
        // ImportFormulaIntoPrescription 已删除,请使用 POST /api/v1/medicalcases/{id}/prescription/import-formula/{formulaId}
    }
}
