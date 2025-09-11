using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Compatibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Controllers
{
    /// <summary>
    /// 配伍记录控制器 - MVP版本
    /// 提供处方配伍禁忌记录的REST API端点
    /// </summary>
    [ApiController]
    [Route("api/v1/prescriptions/{prescriptionId}/compat-notes")]
    [Authorize]
    public class CompatibilityNotesController : ControllerBase
    {
        private readonly CompatibilityNoteService _compatibilityNoteService;
        private readonly ILogger<CompatibilityNotesController> _logger;

        public CompatibilityNotesController(
            CompatibilityNoteService compatibilityNoteService,
            ILogger<CompatibilityNotesController> logger)
        {
            _compatibilityNoteService = compatibilityNoteService;
            _logger = logger;
        }

        /// <summary>
        /// 获取处方的所有配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>配伍记录列表</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CompatibilityNoteDto>>>> GetByPrescriptionId(Guid prescriptionId)
        {
            try
            {
                if (prescriptionId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<List<CompatibilityNoteDto>>.CreateFail("处方ID不能为空"));
                }

                var result = await _compatibilityNoteService.GetByPrescriptionIdAsync(prescriptionId);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<List<CompatibilityNoteDto>>.CreateSuccess(result.Data, result.Message ?? "查询成功"));
                }

                return BadRequest(ApiResponse<List<CompatibilityNoteDto>>.CreateFail(result.Message ?? "查询失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方配伍记录失败: {PrescriptionId}", prescriptionId);
                return StatusCode(500, ApiResponse<List<CompatibilityNoteDto>>.CreateFail("内部服务器错误"));
            }
        }

        /// <summary>
        /// 根据ID获取单个配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="noteId">记录ID</param>
        /// <returns>配伍记录详情</returns>
        [HttpGet("{noteId}")]
        public async Task<ActionResult<ApiResponse<CompatibilityNoteDto>>> GetById(Guid prescriptionId, Guid noteId)
        {
            try
            {
                if (prescriptionId == Guid.Empty || noteId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<CompatibilityNoteDto>.CreateFail("处方ID和记录ID不能为空"));
                }

                var result = await _compatibilityNoteService.GetByIdAsync(prescriptionId, noteId);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<CompatibilityNoteDto>.CreateSuccess(result.Data, result.Message ?? "查询成功"));
                }

                return NotFound(ApiResponse<CompatibilityNoteDto>.CreateFail(result.Message ?? "记录不存在"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配伍记录失败: {PrescriptionId}, {NoteId}", prescriptionId, noteId);
                return StatusCode(500, ApiResponse<CompatibilityNoteDto>.CreateFail("内部服务器错误"));
            }
        }

        /// <summary>
        /// 创建配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="createDto">创建数据</param>
        /// <returns>创建的配伍记录</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CompatibilityNoteDto>>> Create(
            Guid prescriptionId,
            [FromBody] CompatibilityNoteCreateDto createDto)
        {
            try
            {
                if (prescriptionId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<CompatibilityNoteDto>.CreateFail("处方ID不能为空"));
                }

                if (createDto == null)
                {
                    return BadRequest(ApiResponse<CompatibilityNoteDto>.CreateFail("请求数据不能为空"));
                }

                // 获取当前用户ID (临时使用固定值，实际应从JWT Token获取)
                var currentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                var result = await _compatibilityNoteService.CreateAsync(prescriptionId, createDto, currentUserId);

                if (result.IsSuccess)
                {
                    return CreatedAtAction(
                        nameof(GetById),
                        new { prescriptionId = prescriptionId, noteId = result.Data!.Id },
                        ApiResponse<CompatibilityNoteDto>.CreateSuccess(result.Data, result.Message ?? "创建成功"));
                }

                return BadRequest(ApiResponse<CompatibilityNoteDto>.CreateFail(result.Message ?? "创建失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建配伍记录失败: {PrescriptionId}", prescriptionId);
                return StatusCode(500, ApiResponse<CompatibilityNoteDto>.CreateFail("内部服务器错误"));
            }
        }

        /// <summary>
        /// 更新配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="noteId">记录ID</param>
        /// <param name="updateDto">更新数据</param>
        /// <returns>更新后的配伍记录</returns>
        [HttpPut("{noteId}")]
        public async Task<ActionResult<ApiResponse<CompatibilityNoteDto>>> Update(
            Guid prescriptionId,
            Guid noteId,
            [FromBody] CompatibilityNoteUpdateDto updateDto)
        {
            try
            {
                if (prescriptionId == Guid.Empty || noteId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<CompatibilityNoteDto>.CreateFail("处方ID和记录ID不能为空"));
                }

                if (updateDto == null)
                {
                    return BadRequest(ApiResponse<CompatibilityNoteDto>.CreateFail("请求数据不能为空"));
                }

                // 获取当前用户ID (临时使用固定值，实际应从JWT Token获取)
                var currentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                var result = await _compatibilityNoteService.UpdateAsync(prescriptionId, noteId, updateDto, currentUserId);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<CompatibilityNoteDto>.CreateSuccess(result.Data, result.Message ?? "更新成功"));
                }

                return NotFound(ApiResponse<CompatibilityNoteDto>.CreateFail(result.Message ?? "记录不存在"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新配伍记录失败: {PrescriptionId}, {NoteId}", prescriptionId, noteId);
                return StatusCode(500, ApiResponse<CompatibilityNoteDto>.CreateFail("内部服务器错误"));
            }
        }

        /// <summary>
        /// 删除配伍记录
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="noteId">记录ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{noteId}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid prescriptionId, Guid noteId)
        {
            try
            {
                if (prescriptionId == Guid.Empty || noteId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<bool>.CreateFail("处方ID和记录ID不能为空"));
                }

                // 获取当前用户ID (临时使用固定值，实际应从JWT Token获取)
                var currentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                var result = await _compatibilityNoteService.DeleteAsync(prescriptionId, noteId, currentUserId);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<bool>.CreateSuccess(true, result.Message ?? "删除成功"));
                }

                return NotFound(ApiResponse<bool>.CreateFail(result.Message ?? "记录不存在"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除配伍记录失败: {PrescriptionId}, {NoteId}", prescriptionId, noteId);
                return StatusCode(500, ApiResponse<bool>.CreateFail("内部服务器错误"));
            }
        }
    }
}
