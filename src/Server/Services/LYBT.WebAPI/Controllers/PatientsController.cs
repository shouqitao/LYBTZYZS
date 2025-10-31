using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Interfaces;
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

        /// <summary>
        /// 批量导入患者数据 (Issue #1165)
        /// </summary>
        /// <param name="file">Excel文件（.xlsx格式）</param>
        /// <returns>导入结果，包含成功/失败数量和详细错误信息</returns>
        [HttpPost("import")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 限制10MB
        public async Task<ActionResult<ApiResponse<ImportResultDto<PatientDto>>>> Import(IFormFile file)
        {
            try
            {
                // 验证文件
                if (file == null || file.Length == 0)
                {
                    return ValidationFail<ImportResultDto<PatientDto>>("文件不能为空");
                }

                // 验证文件扩展名
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    return ValidationFail<ImportResultDto<PatientDto>>("仅支持.xlsx格式的Excel文件");
                }

                // 验证文件大小（10MB）
                if (file.Length > 10 * 1024 * 1024)
                {
                    return ValidationFail<ImportResultDto<PatientDto>>("文件大小不能超过10MB");
                }

                // 导入数据
                using var stream = file.OpenReadStream();
                var result = await _service.ImportFromExcelAsync(stream, file.FileName);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<ImportResultDto<PatientDto>>(
                        result.ErrorMessage ?? "导入失败",
                        ApiErrorCodes.DATASAVEFAILED);
                }

                // 记录操作日志
                LogOperation("批量导入患者",
                    new { FileName = file.FileName, TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount },
                    null);

                return Success(result.Data, result.Data.Message);
            }
            catch (Exception ex)
            {
                return HandleException<ImportResultDto<PatientDto>>(ex, "批量导入患者", new { FileName = file?.FileName });
            }
        }

        /// <summary>
        /// 下载患者导入模板 (Issue #1165)
        /// </summary>
        /// <returns>包含示例数据的Excel模板文件</returns>
        [HttpGet("import-template")]
        [AllowAnonymous] // 模板下载不需要认证
        public ActionResult ExportTemplate()
        {
            try
            {
                var stream = _service.GenerateImportTemplate();
                var fileName = $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成患者导入模板失败");
                return StatusCode(500);
            }
        }
    }
}
