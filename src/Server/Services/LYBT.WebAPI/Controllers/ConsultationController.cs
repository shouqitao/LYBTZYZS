using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
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

        /// <summary>
        /// 创建诊疗记录
        /// </summary>
        /// <param name="dto">诊疗创建信息</param>
        /// <returns>创建的诊疗信息</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation([FromBody] ConsultationCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<ConsultationDto>();
                if (validationResult != null) return validationResult;

                var result = await _consultationService.CreateAsync(dto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建诊疗记录", dto, result.Data.Id);
                    return CreatedAtAction(
                        nameof(GetById),
                        new { id = result.Data.Id },
                        ApiResponse<ConsultationDto>.CreateSuccess(result.Data));
                }
                
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "创建诊疗记录", dto);
            }
        }

        /// <summary>
        /// 更新诊疗信息
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <param name="dto">诊疗更新信息</param>
        /// <returns>更新后的诊疗信息</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(Guid id, [FromBody] ConsultationUpdateDto dto)
        {
            try
            {
                var guidValidationResult = ValidateGuid<ConsultationDto>(id, "诊疗ID");
                if (guidValidationResult != null) return guidValidationResult;

                var modelValidationResult = ValidateModel<ConsultationDto>();
                if (modelValidationResult != null) return modelValidationResult;

                var result = await _consultationService.UpdateAsync(id, dto);
                
                if (result.IsSuccess)
                {
                    LogOperation("更新诊疗记录", dto, id);
                }
                
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "更新诊疗记录", new { ConsultationId = id, UpdateData = dto });
            }
        }

        /// <summary>
        /// 删除诊疗记录（软删除）
        /// </summary>
        /// <param name="id">诊疗ID</param>
        /// <returns>操作结果</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> DeleteConsultation(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "诊疗ID");
                if (validationResult != null) return validationResult;

                var result = await _consultationService.DeleteAsync(id);
                
                if (result.IsSuccess)
                {
                    LogOperation("删除诊疗记录", null, id);
                }
                
                return HandleServiceResult(result, "诊疗记录删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除诊疗记录", new { ConsultationId = id });
            }
        }
    }
}