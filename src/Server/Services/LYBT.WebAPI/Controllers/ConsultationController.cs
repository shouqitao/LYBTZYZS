using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 诊疗管理控制器 - 简化版（仅CRUD）
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class ConsultationController : ControllerBase
    {
        private readonly IConsultationService _consultationService;

        public ConsultationController(IConsultationService consultationService)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
        }

        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        /// <returns>分页的诊疗记录列表</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ConsultationDto>), 200)]
        public async Task<IActionResult> GetConsultations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var result = await _consultationService.GetPagedAsync(page, pageSize, keyword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取诊疗记录列表失败");
                return BadRequest(ApiResponse<object>.CreateFail($"获取诊疗记录列表失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <returns>诊疗详情</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ConsultationDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var consultation = await _consultationService.GetByIdAsync(id);
                if (consultation == null)
                {
                    return NotFound(ApiResponse<object>.CreateFail("诊疗记录不存在"));
                }
                return Ok(consultation);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取诊疗详情失败：{ConsultationId}", id);
                return BadRequest(ApiResponse<object>.CreateFail($"获取诊疗详情失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 创建诊疗记录
        /// </summary>
        /// <param name="dto">诊疗创建信息</param>
        /// <returns>创建的诊疗信息</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ConsultationDetailDto), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateConsultation([FromBody] ConsultationCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.CreateFail("请求数据无效", ModelState));
                }

                var consultation = await _consultationService.CreateAsync(dto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = consultation.Data?.Id },
                    consultation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建诊疗记录失败");
                return BadRequest(ApiResponse<object>.CreateFail($"创建诊疗记录失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 更新诊疗信息
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <param name="dto">诊疗更新信息</param>
        /// <returns>更新后的诊疗信息</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ConsultationDetailDto), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateConsultation(Guid id, [FromBody] ConsultationUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.CreateFail("请求数据无效", ModelState));
                }

                var consultation = await _consultationService.UpdateAsync(id, dto);
                if (consultation == null)
                {
                    return NotFound(ApiResponse<object>.CreateFail("诊疗记录不存在"));
                }

                return Ok(consultation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新诊疗记录失败：{ConsultationId}", id);
                return BadRequest(ApiResponse<object>.CreateFail($"更新诊疗记录失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 删除诊疗记录（软删除）
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <returns>操作结果</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteConsultation(Guid id)
        {
            try
            {
                var result = await _consultationService.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound(ApiResponse<object>.CreateFail("诊疗记录不存在"));
                }

                return Ok(ApiResponse<object>.CreateSuccess("诊疗记录删除成功"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "删除诊疗记录失败：{ConsultationId}", id);
                return BadRequest(ApiResponse<object>.CreateFail($"删除诊疗记录失败: {ex.Message}"));
            }
        }
    }
}