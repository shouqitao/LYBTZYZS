using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 患者管理 API - 统一API响应格式
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
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
        public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? name = null,
            [FromQuery] string? phone = null,
            [FromQuery] string? idCard = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<PatientDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var query = new PatientSearchDto
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Name = name,
                    PhoneNumber = phone, // 使用正确的属性名
                    IDNumber = idCard // 使用正确的属性名

                    // 注意：IsActive属性在DTO中不存在，删除该字段
                };

                var result = await _service.GetPagedAsync(query);
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
                if (!result.IsSuccess || !result.Data)
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

        /// <summary>
        /// 启用患者
        /// </summary>
        [HttpPost("{id}/enable")]
        public async Task<ActionResult<ApiResponse>> Enable(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "患者ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.EnableAsync(id);
                if (!result.IsSuccess)
                {
                    return BusinessFail(result.ErrorMessage ?? "启用患者失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation("启用患者成功", null, id);
                return Success("启用成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "启用患者", id);
            }
        }

        /// <summary>
        /// 禁用患者
        /// </summary>
        [HttpPost("{id}/disable")]
        public async Task<ActionResult<ApiResponse>> Disable(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "患者ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.DisableAsync(id);
                if (!result.IsSuccess)
                {
                    return BusinessFail(result.ErrorMessage ?? "禁用患者失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation("禁用患者成功", null, id);
                return Success("禁用成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "禁用患者", id);
            }
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        [HttpGet("by-idcard/{idCard}")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> GetByIdCard(string idCard)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idCard))
                {
                    return ValidationFail<PatientDto>("身份证号不能为空");
                }

                var result = await _service.GetByIdCardAsync(idCard);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PatientDto>(result.ErrorMessage ?? "未找到对应患者", ApiErrorCodes.PATIENTNOTFOUND);
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PatientDto>(ex, "根据身份证查找患者", idCard);
            }
        }

        /// <summary>
        /// 根据电话号码查找患者
        /// </summary>
        [HttpGet("by-phone/{phone}")]
        public async Task<ActionResult<ApiResponse<List<PatientDto>>>> GetByPhone(string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return ValidationFail<List<PatientDto>>("电话号码不能为空");
                }

                var result = await _service.GetByPhoneAsync(phone);
                if (!result.IsSuccess || result.Data == null)
                {
                    return Success(new List<PatientDto>(), "未找到匹配患者");
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDto>>(ex, "根据电话查找患者", phone);
            }
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<PatientDto>>>> Search([FromQuery] string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ValidationFail<List<PatientDto>>("搜索关键词不能为空");
                }

                var result = await _service.SearchAsync(keyword);
                if (!result.IsSuccess || result.Data == null)
                {
                    return Success(new List<PatientDto>(), "未找到匹配患者");
                }

                return Success(result.Data, "搜索成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDto>>(ex, "搜索患者", keyword);
            }
        }
    }
}
