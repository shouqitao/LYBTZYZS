using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理 API - 基础CRUD功能
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientsController : BaseApiController
    {
        private readonly IPatientService _service;

        public PatientsController(IPatientService service, IMemoryCache cache, ILogger<PatientsController> logger)
            : base(logger, cache)
        {
            _service = service;
        }

        /// <summary>
        /// 获取患者列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "PatientsCache")]
        public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<PatientDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _service.GetPagedAsync(page, pageSize, keyword);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PatientDto>(ex, "获取患者列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 900, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PatientDto>(id, "患者ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PatientDto>(result.ErrorMessage ?? "患者不存在", ApiErrorCodes.PATIENTNOTFOUND);
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PatientDto>(ex, "获取患者详情", id);
            }
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PatientDto>>> Add([FromBody] PatientCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<PatientDto>();
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.CreateAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PatientDto>(result.ErrorMessage ?? "新增患者失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("新增患者成功", result.Data, result.Data.Id);
                return Success(result.Data, "患者创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<PatientDto>(ex, "新增患者", dto);
            }
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> Update(Guid id, [FromBody] PatientUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<PatientDto>(id, "患者ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<PatientDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PatientDto>(result.ErrorMessage ?? "更新患者失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation("更新患者成功", result.Data, id);
                return Success(result.Data, "患者更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<PatientDto>(ex, "更新患者", new { id, dto });
            }
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "患者ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound("患者不存在", ApiErrorCodes.PATIENTNOTFOUND);
                }

                LogOperation("删除患者成功", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除患者", id);
            }
        }
    }
}
