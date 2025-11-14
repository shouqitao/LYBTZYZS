using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

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

        public PatientsController(IPatientService service, ILogger<PatientsController> logger)
            : base(logger)
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
                    return ValidationFail<PagedResult<PatientDto>>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _service.GetPagedAsync(page, pageSize, keyword);
                return Success(result.Data!, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PagedResult<PatientDto>>(ex, "获取患者列表", new { page, pageSize, keyword });
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
                if (id == Guid.Empty)
                {
                    return ValidationFail<PatientDto>("患者ID不能为空");
                }

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PatientDto>(result.ErrorMessage ?? "患者不存在");
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
        public async Task<ActionResult<ApiResponse<PatientDto>>> Add([FromBody] PatientInputDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationFail<PatientDto>("参数验证失败");
                }

                var result = await _service.CreateAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PatientDto>(result.ErrorMessage ?? "新增患者失败");
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
        public async Task<ActionResult<ApiResponse<PatientDto>>> Update(Guid id, [FromBody] PatientInputDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ValidationFail<PatientDto>("患者ID不能为空");
                }

                if (!ModelState.IsValid)
                {
                    return ValidationFail<PatientDto>("参数验证失败");
                }

                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PatientDto>(result.ErrorMessage ?? "更新患者失败");
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
                    return NotFound("患者不存在");
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
        /// 批量导入患者数据 (Epic #1934 FR-001)
        /// </summary>
        /// <param name="file">Excel文件（.xlsx格式，最大10MB）</param>
        /// <returns>导入结果，包含成功/失败/跳过数量和详细失败信息</returns>
        /// <response code="200">导入成功，返回导入统计结果</response>
        /// <response code="400">文件验证失败（文件为空、格式错误、大小超限）</response>
        /// <response code="500">服务器内部错误</response>
        [HttpPost("import")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 限制10MB
        [ProducesResponseType(typeof(ApiResponse<BatchImportResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BatchImportResultDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<BatchImportResultDto>>> Import(IFormFile file)
        {
            try
            {
                // 验证文件
                if (file == null || file.Length == 0)
                {
                    return ValidationFail<BatchImportResultDto>("文件不能为空");
                }

                // 验证文件扩展名
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    return ValidationFail<BatchImportResultDto>("仅支持.xlsx格式的Excel文件");
                }

                // 验证文件大小（10MB）
                if (file.Length > 10 * 1024 * 1024)
                {
                    return ValidationFail<BatchImportResultDto>("文件大小不能超过10MB");
                }

                // Epic #1934: 批量导入患者（支持BR-002失败恢复机制）
                using var stream = file.OpenReadStream();
                var result = await _service.BatchImportAsync(stream, file.FileName);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<BatchImportResultDto>(result.ErrorMessage ?? "导入失败");
                }

                // 记录操作日志
                LogOperation("批量导入患者",
                    new { FileName = file.FileName, SuccessCount = result.Data.SuccessCount, FailureCount = result.Data.FailureCount, SkippedCount = result.Data.SkippedCount },
                    null);

                return Success(result.Data);
            }
            catch (Exception ex)
            {
                return HandleException<BatchImportResultDto>(ex, "批量导入患者", new { FileName = file?.FileName });
            }
        }

        /// <summary>
        /// 下载患者导入模板 (Epic #1934 FR-002)
        /// </summary>
        /// <returns>包含示例数据的Excel模板文件</returns>
        /// <response code="200">返回Excel模板文件</response>
        /// <response code="500">生成模板失败</response>
        [HttpGet("import-template")]
        [AllowAnonymous] // 模板下载不需要认证
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<ActionResult> ExportTemplate()
        {
            try
            {
                var config = new ExportTemplateDto
                {
                    IncludeSampleData = true,
                    SampleRowCount = 3
                };
                var stream = await _service.ExportTemplateAsync(config);
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

        /// <summary>
        /// 导出患者数据到Excel (Epic #1934 FR-003)
        /// </summary>
        /// <param name="keyword">搜索关键词（可选），支持姓名、手机号、拼音码模糊查询</param>
        /// <returns>包含患者数据的Excel文件（最大10000条记录）</returns>
        /// <response code="200">返回包含患者数据的Excel文件</response>
        /// <response code="500">导出失败</response>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<ActionResult> ExportPatients([FromQuery] string? keyword = null)
        {
            try
            {
                var stream = await _service.ExportPatientsAsync(keyword);
                var fileName = string.IsNullOrWhiteSpace(keyword)
                    ? $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : $"患者数据_{keyword}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                LogOperation("导出患者数据", new { Keyword = keyword }, null);

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败，关键词：{Keyword}", keyword);
                return StatusCode(500);
            }
        }
    }
}
