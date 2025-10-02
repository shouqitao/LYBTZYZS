using Asp.Versioning;
using LYBT.Core.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
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
        /// 获取处方列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "PrescriptionsCache")]
        public async Task<ActionResult<ApiResponse<PagedResult<PrescriptionDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<PrescriptionDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var pagedResult = await _service.GetPagedAsync(page, pageSize, keyword);
                return HandlePagedServiceResult(pagedResult, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PrescriptionDto>(ex, "获取处方列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PrescriptionDto>(result.ErrorMessage ?? "处方不存在", ApiErrorCodes.PRESCRIPTIONNOTFOUND);
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "获取处方详情", id);
            }
        }

        /// <summary>
        /// 新增处方
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Add([FromBody] PrescriptionCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<PrescriptionDto>();
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.CreateAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "新增处方失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("新增处方成功", result.Data, result.Data.Id);
                return Success(result.Data, "处方创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "新增处方", dto);
            }
        }

        /// <summary>
        /// 编辑处方
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Update(Guid id, [FromBody] PrescriptionUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<PrescriptionDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                // 使用路由参数中的ID
                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "编辑处方失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation("编辑处方成功", result.Data, id);
                return Success(result.Data, "处方更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "编辑处方", new { id, dto });
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "处方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound("处方不存在", ApiErrorCodes.PRESCRIPTIONNOTFOUND);
                }

                LogOperation("删除处方成功", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除处方", id);
            }
        }
    }
}
