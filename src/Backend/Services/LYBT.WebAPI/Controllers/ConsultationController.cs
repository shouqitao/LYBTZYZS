using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 看诊管理控制器
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ConsultationController : BaseController
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
        /// 分页查询看诊记录
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] ConsultationPagedQueryDto query)
        {
            try
            {
                var result = await _consultationService.GetPagedAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询看诊记录失败");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _consultationService.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { message = "看诊记录不存在" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情失败: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊信息
        /// </summary>
        [HttpGet("medical-case/{medicalCaseId}")]
        public async Task<IActionResult> GetByMedicalCaseId(Guid medicalCaseId)
        {
            try
            {
                var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
                if (result == null)
                {
                    return NotFound(new { message = "看诊记录不存在" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取看诊信息失败: {MedicalCaseId}", medicalCaseId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartConsultation([FromBody] ConsultationStartDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _consultationService.StartConsultationAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊失败");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 更新看诊信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConsultation(Guid id, [FromBody] ConsultationUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _consultationService.UpdateConsultationAsync(id, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊信息失败: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteConsultation(Guid id, [FromBody] ConsultationCompleteDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _consultationService.CompleteConsultationAsync(id, dto);
                if (result)
                {
                    return Ok(new { message = "看诊完成" });
                }
                return BadRequest(new { message = "完成看诊失败" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成看诊失败: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取医生今日看诊列表
        /// </summary>
        [HttpGet("doctor/{doctorId}/today")]
        public async Task<IActionResult> GetTodayConsultationsByDoctor(Guid doctorId)
        {
            try
            {
                var result = await _consultationService.GetTodayConsultationsByDoctorAsync(doctorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生今日看诊列表失败: {DoctorId}", doctorId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取患者历史看诊记录
        /// </summary>
        [HttpGet("patient/{patientId}/history")]
        public async Task<IActionResult> GetPatientHistory(Guid patientId)
        {
            try
            {
                var result = await _consultationService.GetPatientHistoryAsync(patientId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者历史看诊记录失败: {PatientId}", patientId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 统计医生看诊数量
        /// </summary>
        [HttpGet("doctor/{doctorId}/count")]
        public async Task<IActionResult> GetDoctorConsultationCount(
            Guid doctorId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var count = await _consultationService.GetDoctorConsultationCountAsync(doctorId, startDate, endDate);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "统计医生看诊数量失败: {DoctorId}", doctorId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _consultationService.DeleteAsync(id);
                if (result)
                {
                    return Ok(new { message = "删除成功" });
                }
                return NotFound(new { message = "看诊记录不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}