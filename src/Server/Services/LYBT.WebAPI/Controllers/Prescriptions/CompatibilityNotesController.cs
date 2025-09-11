using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Compatibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers.Prescriptions
{
    /// <summary>
    /// 配伍记录控制器 - 统一API网关版本
    /// 提供处方配伍禁忌记录的REST API端点
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/prescriptions/{prescriptionId}/compat-notes")]
    [Authorize]
    public class CompatibilityNotesController : BaseApiController
    {
        private readonly ICompatibilityNoteService _compatibilityNoteService;

        public CompatibilityNotesController(
            ICompatibilityNoteService compatibilityNoteService,
            IMemoryCache cache,
            ILogger<CompatibilityNotesController> logger)
            : base(logger, cache)
        {
            _compatibilityNoteService = compatibilityNoteService;
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
                var validation = ValidateGuid<List<CompatibilityNoteDto>>(prescriptionId, "处方ID");
                if (validation != null)
                    return validation;

                var result = await _compatibilityNoteService.GetByPrescriptionIdAsync(prescriptionId);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<CompatibilityNoteDto>>(ex, "获取处方配伍记录", prescriptionId);
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
                var validation = ValidateGuid<CompatibilityNoteDto>(prescriptionId, "处方ID");
                if (validation != null)
                    return validation;

                validation = ValidateGuid<CompatibilityNoteDto>(noteId, "记录ID");
                if (validation != null)
                    return validation;

                var result = await _compatibilityNoteService.GetByIdAsync(prescriptionId, noteId);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<CompatibilityNoteDto>(ex, "获取配伍记录详情", new { prescriptionId, noteId });
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
                var validation = ValidateGuid<CompatibilityNoteDto>(prescriptionId, "处方ID");
                if (validation != null)
                    return validation;

                validation = ValidateModel<CompatibilityNoteDto>();
                if (validation != null)
                    return validation;

                // TODO: 从JWT Token获取当前用户ID
                var currentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                var result = await _compatibilityNoteService.CreateAsync(prescriptionId, createDto, currentUserId);

                if (result.IsSuccess)
                {
                    return CreatedAtAction(
                        nameof(GetById),
                        new { prescriptionId = prescriptionId, noteId = result.Data!.Id },
                        Success(result.Data, "创建成功"));
                }

                return HandleServiceResult(result, "创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<CompatibilityNoteDto>(ex, "创建配伍记录", prescriptionId);
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
                var validation = ValidateGuid<CompatibilityNoteDto>(prescriptionId, "处方ID");
                if (validation != null)
                    return validation;

                validation = ValidateGuid<CompatibilityNoteDto>(noteId, "记录ID");
                if (validation != null)
                    return validation;

                validation = ValidateModel<CompatibilityNoteDto>();
                if (validation != null)
                    return validation;

                // TODO: 从JWT Token获取当前用户ID
                var currentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                var result = await _compatibilityNoteService.UpdateAsync(prescriptionId, noteId, updateDto, currentUserId);
                return HandleServiceResult(result, "更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<CompatibilityNoteDto>(ex, "更新配伍记录", new { prescriptionId, noteId });
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
                var validation = ValidateGuid<bool>(prescriptionId, "处方ID");
                if (validation != null)
                    return validation;

                validation = ValidateGuid<bool>(noteId, "记录ID");
                if (validation != null)
                    return validation;

                // TODO: 从JWT Token获取当前用户ID
                var currentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                var result = await _compatibilityNoteService.DeleteAsync(prescriptionId, noteId, currentUserId);
                return HandleServiceResult(result, "删除成功");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "删除配伍记录", new { prescriptionId, noteId });
            }
        }
    }
}
