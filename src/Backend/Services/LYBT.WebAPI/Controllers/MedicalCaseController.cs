using Asp.Versioning;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class MedicalCaseController : BaseController
    {
        private readonly IMedicalCaseService _medicalCaseService;

        public MedicalCaseController(
            IMedicalCaseService medicalCaseService,
            ILogger<MedicalCaseController> logger,
            IMemoryCache cache) : base(logger, cache)
        {
            _medicalCaseService = medicalCaseService;
        }

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _medicalCaseService.GetPagedAsync(pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询医疗案例失败");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _medicalCaseService.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { message = "医疗案例不存在" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MedicalCaseCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _medicalCaseService.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MedicalCaseEditDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 确保DTO的ID与路由参数一致
                dto.Id = id;
                var result = await _medicalCaseService.UpdateAsync(dto);
                if (!result)
                {
                    return NotFound(new { message = "医疗案例不存在" });
                }
                return Ok(new { message = "更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取患者的医疗案例列表
        /// </summary>
        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetByPatientId(Guid patientId)
        {
            try
            {
                var result = await _medicalCaseService.GetByPatientIdAsync(patientId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者医疗案例列表失败: {PatientId}", patientId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取今日医疗案例列表
        /// </summary>
        [HttpGet("user/{userId}/today")]
        public async Task<IActionResult> GetTodayByUserId(Guid userId)
        {
            try
            {
                var result = await _medicalCaseService.GetTodayByUserIdAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取今日医疗案例列表失败: {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatus status)
        {
            try
            {
                var result = await _medicalCaseService.UpdateStatusAsync(id, status);
                if (!result)
                {
                    return NotFound(new { message = "医疗案例不存在" });
                }
                return Ok(new { message = "状态更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _medicalCaseService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "医疗案例不存在" });
                }
                return Ok(new { message = "删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}