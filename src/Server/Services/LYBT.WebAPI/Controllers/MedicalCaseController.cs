using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理控制器 - 统一API响应格式
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class MedicalCaseController : BaseApiController
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
        /// 分页查询医疗案例 - 统一API响应格式
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetPaged([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFail<object>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _medicalCaseService.GetPagedAsync(pageIndex, pageSize);
                return Success<object>(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "分页查询医疗案例", new { pageIndex, pageSize });
            }
        }

        /// <summary>
        /// 获取医疗案例详情 - 统一API响应格式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> GetById(Guid id)
        {
            try
            {
                var validation = ValidateGuid<MedicalCaseDetailDto>(id, "医疗案例ID");
                if (validation != null) return validation;

                var result = await _medicalCaseService.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound<MedicalCaseDetailDto>("医疗案例不存在", ApiErrorCodes.MEDICAL_CASE_NOT_FOUND);
                }
                return Success(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDetailDto>(ex, "获取医疗案例详情", id);
            }
        }

        /// <summary>
        /// 创建医疗案例 - 统一API响应格式
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> Create([FromBody] MedicalCaseCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<MedicalCaseDetailDto>();
                if (validation != null) return validation;

                var result = await _medicalCaseService.CreateAsync(dto);
                if (result == null)
                {
                    return BusinessFail<MedicalCaseDetailDto>("创建医疗案例失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }
                
                LogOperation("创建医疗案例", result, result.Id);
                return Success(result, "医疗案例创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDetailDto>(ex, "创建医疗案例", dto);
            }
        }

        /// <summary>
        /// 更新医疗案例 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] MedicalCaseEditDto dto)
        {
            try
            {
                var idValidation = ValidateGuid(id, "医疗案例ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel();
                if (modelValidation != null) return modelValidation;

                // 确保DTO的ID与路由参数一致
                dto.Id = id;
                var result = await _medicalCaseService.UpdateAsync(dto);
                if (!result)
                {
                    return BusinessFail("医疗案例不存在", ApiErrorCodes.MEDICAL_CASE_NOT_FOUND);
                }
                
                LogOperation("更新医疗案例", null, id);
                return Success("更新成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新医疗案例", new { id, dto });
            }
        }

        /// <summary>
        /// 获取患者的医疗案例列表 - 统一API响应格式
        /// </summary>
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<ApiResponse<List<MedicalCaseDto>>>> GetByPatientId(Guid patientId)
        {
            try
            {
                var validation = ValidateGuid<List<MedicalCaseDto>>(patientId, "患者ID");
                if (validation != null) return validation;

                var result = await _medicalCaseService.GetByPatientIdAsync(patientId);
                return Success(result, $"查询成功，共{result.Count}条记录");
            }
            catch (Exception ex)
            {
                return HandleException<List<MedicalCaseDto>>(ex, "获取患者医疗案例列表", patientId);
            }
        }

        /// <summary>
        /// 获取今日医疗案例列表 - 统一API响应格式
        /// </summary>
        [HttpGet("user/{userId}/today")]
        public async Task<ActionResult<ApiResponse<object>>> GetTodayByUserId(Guid userId)
        {
            try
            {
                var validation = ValidateGuid<object>(userId, "用户ID");
                if (validation != null) return validation;

                var result = await _medicalCaseService.GetTodayByUserIdAsync(userId);
                return Success<object>(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取今日医疗案例列表", userId);
            }
        }

        /// <summary>
        /// 更新医疗案例状态 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] MedicalCaseStatus status)
        {
            try
            {
                var validation = ValidateGuid(id, "医疗案例ID");
                if (validation != null) return validation;

                var result = await _medicalCaseService.UpdateStatusAsync(id, status);
                if (!result)
                {
                    return NotFound("医疗案例不存在", ApiErrorCodes.MEDICAL_CASE_NOT_FOUND);
                }
                
                LogOperation("更新医疗案例状态", new { id, status });
                return Success("状态更新成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新医疗案例状态", new { id, status });
            }
        }

        /// <summary>
        /// 删除医疗案例（软删除） - 统一API响应格式
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "医疗案例ID");
                if (validation != null) return validation;

                var result = await _medicalCaseService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound("医疗案例不存在", ApiErrorCodes.MEDICAL_CASE_NOT_FOUND);
                }
                
                LogOperation("删除医疗案例", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除医疗案例", id);
            }
        }
    }
}