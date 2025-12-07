using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材管理 API - 基础CRUD功能
    /// </summary>
    /// optimize-api-permissions: 药材管理需Doctor或Admin角色
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "DoctorOrAdmin")]
    public class HerbsController : BaseApiController
    {
        private readonly IHerbService _herbService;

        public HerbsController(
            IHerbService herbService,
            ILogger<HerbsController> logger)
            : base(logger)
        {
            _herbService = herbService;
        }

        /// <summary>
        /// 获取药材分页列表（Issue #1164: 扩展支持分类筛选）
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "HerbsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HerbDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFail("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _herbService.GetPagedAsync(page, pageSize, keyword, category);
                return Success(result.Data!, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取药材列表", new { page, pageSize, keyword, category });
            }
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "药材ID") is { } error) return error;

                var result = await _herbService.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound(result.ErrorMessage ?? "药材不存在");
                }
                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取药材详情", id);
            }
        }

        /// <summary>
        /// 创建新药材
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<HerbDto>), 200)]
        public async Task<IActionResult> Create([FromBody] HerbInputDto dto)
        {
            try
            {
                var result = await _herbService.CreateAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建药材", result.Data, result.Data.Id);
                    return Success(result.Data, "药材创建成功");
                }

                return BusinessFail(result.ErrorMessage ?? "创建药材失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建药材", dto);
            }
        }

        /// <summary>
        /// 更新药材信息
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto dto)
        {
            try
            {
                if (ValidateGuid(id, "药材ID") is { } error) return error;

                dto.Id = id;

                var result = await _herbService.UpdateAsync(id, dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新药材信息", result.Data, id);
                    return Success(result.Data, "药材信息更新成功");
                }

                return BusinessFail(result.ErrorMessage ?? "更新药材信息失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新药材信息", new { id, dto });
            }
        }

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "药材ID") is { } error) return error;

                var result = await _herbService.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound(result.ErrorMessage ?? "药材不存在");
                }

                LogOperation("删除药材", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除药材", id);
            }
        }

        /// <summary>
        /// 批量导入药材数据 (Issue #1166)
        /// </summary>
        [HttpPost("import")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<ImportResultDto<HerbDto>>), 200)]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return ValidationFail("文件不能为空");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    return ValidationFail("仅支持.xlsx格式的Excel文件");
                }

                if (file.Length > 10 * 1024 * 1024)
                {
                    return ValidationFail("文件大小不能超过10MB");
                }

                using var stream = file.OpenReadStream();
                var result = await _herbService.ImportFromExcelAsync(stream, file.FileName);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "导入失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("批量导入药材",
                    new { FileName = file.FileName, TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount },
                    null);

                return Success(result.Data, result.Data.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量导入药材", new { FileName = file?.FileName });
            }
        }

        /// <summary>
        /// 导出药材数据到Excel (Issue #1166)
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> Export([FromQuery] string? category = null)
        {
            try
            {
                var stream = await _herbService.ExportAsync(category);
                var fileName = string.IsNullOrWhiteSpace(category)
                    ? $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : $"药材数据_{category}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                LogOperation("导出药材数据", new { Category = category, FileName = fileName }, null);

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据失败，分类筛选：{Category}", category);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// 下载药材导入模板 (Issue #1166)
        /// </summary>
        [HttpGet("import-template")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public IActionResult ExportTemplate()
        {
            try
            {
                var stream = _herbService.GenerateImportTemplate();
                var fileName = $"药材导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成药材导入模板失败");
                return StatusCode(500);
            }
        }

        // ========== Epic #1962: 批量导入/导出和引用检查端点 ==========

        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        [HttpPost("batch-import")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportRequestDto request)
        {
            try
            {
                if (request.Herbs == null || request.Herbs.Count == 0)
                {
                    return ValidationFail("药材列表不能为空");
                }

                if (request.Herbs.Count > 10000)
                {
                    return ValidationFail("批量导入最多支持10000条记录");
                }

                var result = await _herbService.BatchImportAsync(request.Herbs, request.Strategy);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("批量导入药材（Epic #1962）",
                        new {
                            TotalCount = result.Data.TotalCount,
                            SuccessCount = result.Data.SuccessCount,
                            FailureCount = result.Data.FailureCount,
                            SkippedCount = result.Data.SkippedCount,
                            Strategy = request.Strategy.ToString()
                        },
                        null);
                }

                return Success(result.Data!, $"批量导入完成: 成功{result.Data?.SuccessCount ?? 0}条");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量导入药材", new { HerbCount = request.Herbs?.Count, Strategy = request.Strategy });
            }
        }

        /// <summary>
        /// 导出药材数据（返回JSON列表，Desktop层负责Excel生成）
        /// </summary>
        [HttpGet("export-all")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbDto>>), 200)]
        public async Task<IActionResult> GetAllForExport([FromQuery] string? category = null)
        {
            try
            {
                var result = await _herbService.GetAllForExportAsync(category);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("导出药材数据（Epic #1962）",
                        new { Category = category, Count = result.Data.Count },
                        null);
                }

                return Success(result.Data!, "导出数据查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取导出数据", new { Category = category });
            }
        }

        /// <summary>
        /// 检查药材是否被处方引用
        /// </summary>
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<HerbReferenceCheckDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> CheckReference(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "药材ID") is { } error) return error;

                var result = await _herbService.CheckReferenceAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "引用检查失败");
                }
                return Success(result.Data, "引用检查完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "检查药材引用", id);
            }
        }

        /// <summary>
        /// 批量检查药材引用关系
        /// </summary>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbReferenceCheckDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchCheckReference([FromBody] BatchCheckReferenceRequestDto request)
        {
            try
            {
                if (request.HerbIds == null || request.HerbIds.Count == 0)
                {
                    return ValidationFail("药材ID列表不能为空");
                }

                if (request.HerbIds.Count > 100)
                {
                    return ValidationFail("批量检查最多支持100条记录");
                }

                var result = await _herbService.BatchCheckReferenceAsync(request.HerbIds);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("批量检查药材引用（Epic #1962）",
                        new { Count = result.Data.Count },
                        null);
                }

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "批量引用检查失败");
                }
                return Success(result.Data, "批量引用检查完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量检查药材引用", new { Count = request.HerbIds?.Count });
            }
        }
    }
}
