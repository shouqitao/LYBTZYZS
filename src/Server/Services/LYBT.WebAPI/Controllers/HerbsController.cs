using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材管理 API - 基础CRUD功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class HerbsController : BaseApiController
    {
        private readonly IHerbService _herbService;

        public HerbsController(
            IHerbService herbService,
            ILogger<HerbsController> logger,
            IMemoryCache cache) : base(logger, cache)
        {
            _herbService = herbService;
        }

        /// <summary>
        /// 获取药材分页列表（Issue #1164: 扩展支持分类筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="category">分类筛选</param>
        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "HerbsCache")]
        public async Task<ActionResult<ApiResponse<PagedResult<HerbDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<HerbDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _herbService.GetPagedAsync(page, pageSize, keyword, category);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<HerbDto>(ex, "获取药材列表", new { page, pageSize, keyword, category });
            }
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<ApiResponse<HerbDto>>> GetById(Guid id)
        {
            try
            {
                var validation = ValidateGuid<HerbDto>(id, "药材ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _herbService.GetByIdAsync(id);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<HerbDto>(ex, "获取药材详情", id);
            }
        }

        /// <summary>
        /// 创建新药材
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<HerbDto>>> Create([FromBody] HerbCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<HerbDto>();
                if (validation != null)
                {
                    return validation;
                }

                var result = await _herbService.CreateAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建药材", result.Data, result.Data.Id);
                }

                return HandleServiceResult(result, "药材创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<HerbDto>(ex, "创建药材", dto);
            }
        }

        /// <summary>
        /// 更新药材信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<HerbDto>>> Update(Guid id, [FromBody] HerbUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<HerbDto>(id, "药材ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<HerbDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                // 确保使用路由中的ID
                dto.Id = id;

                var result = await _herbService.UpdateAsync(id, dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新药材信息", result.Data, id);
                }

                return HandleServiceResult(result, "药材信息更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<HerbDto>(ex, "更新药材信息", new { id, dto });
            }
        }

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "药材ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _herbService.DeleteAsync(id);
                return HandleServiceResult(result, "删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除药材", id);
            }
        }


        /// <summary>
        /// 批量删除药材（软删除）(Issue #1169)
        /// </summary>
        /// <param name="request">批量删除请求</param>
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<BatchOperationResultDto>>> BatchDeleteHerbs([FromBody] BatchDeleteRequestDto request)
        {
            try
            {
                // 验证请求
                if (request.Ids == null || request.Ids.Count == 0)
                {
                    return ValidationFail<BatchOperationResultDto>("ID列表不能为空");
                }

                if (request.Ids.Count > 100)
                {
                    return ValidationFail<BatchOperationResultDto>("批量操作最多支持100条记录");
                }

                var result = await _herbService.BatchDeleteAsync(request.Ids);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("批量删除药材", 
                        new { TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount }, 
                        null);
                }

                return HandleServiceResult(result, result.Data?.Message ?? "批量删除完成");
            }
            catch (Exception ex)
            {
                return HandleException<BatchOperationResultDto>(ex, "批量删除药材", new { IdCount = request.Ids?.Count });
            }
        }


        /// <summary>
        /// 批量导入药材数据 (Issue #1166)
        /// </summary>
        /// <param name="file">Excel文件（.xlsx格式）</param>
        /// <returns>导入结果，包含成功/失败数量和详细错误信息</returns>
        [HttpPost("import")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 限制10MB
        public async Task<ActionResult<ApiResponse<ImportResultDto<HerbDto>>>> Import(IFormFile file)
        {
            try
            {
                // 验证文件
                if (file == null || file.Length == 0)
                {
                    return ValidationFail<ImportResultDto<HerbDto>>("文件不能为空");
                }

                // 验证文件扩展名
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    return ValidationFail<ImportResultDto<HerbDto>>("仅支持.xlsx格式的Excel文件");
                }

                // 验证文件大小（10MB）
                if (file.Length > 10 * 1024 * 1024)
                {
                    return ValidationFail<ImportResultDto<HerbDto>>("文件大小不能超过10MB");
                }

                // 导入数据
                using var stream = file.OpenReadStream();
                var result = await _herbService.ImportFromExcelAsync(stream, file.FileName);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<ImportResultDto<HerbDto>>(
                        result.ErrorMessage ?? "导入失败",
                        ApiErrorCodes.DATASAVEFAILED);
                }

                // 记录操作日志
                LogOperation("批量导入药材",
                    new { FileName = file.FileName, TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount },
                    null);

                return Success(result.Data, result.Data.Message);
            }
            catch (Exception ex)
            {
                return HandleException<ImportResultDto<HerbDto>>(ex, "批量导入药材", new { FileName = file?.FileName });
            }
        }

        /// <summary>
        /// 导出药材数据到Excel (Issue #1166)
        /// </summary>
        /// <param name="category">可选的分类筛选参数</param>
        /// <returns>包含药材数据的Excel文件</returns>
        [HttpGet("export")]
        public async Task<ActionResult> Export([FromQuery] string? category = null)
        {
            try
            {
                var stream = await _herbService.ExportAsync(category);
                var fileName = string.IsNullOrWhiteSpace(category)
                    ? $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : $"药材数据_{category}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // 记录操作日志
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
        /// <returns>包含示例数据的Excel模板文件</returns>
        [HttpGet("import-template")]
        [AllowAnonymous] // 模板下载不需要认证
        public ActionResult ExportTemplate()
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
    }
}
