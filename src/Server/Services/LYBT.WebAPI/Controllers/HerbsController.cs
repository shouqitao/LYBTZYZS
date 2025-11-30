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
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
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
        /// 修复：仅在无搜索参数时使用缓存，有搜索时实时查询
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="category">分类筛选</param>
        [HttpGet]
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
                    return ValidationFail<PagedResult<HerbDto>>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _herbService.GetPagedAsync(page, pageSize, keyword, category);
                return Success(result.Data!, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PagedResult<HerbDto>>(ex, "获取药材列表", new { page, pageSize, keyword, category });
            }
        }

    
        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<HerbDto>>> GetById(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ValidationFail<HerbDto>("药材ID不能为空");
                }

                var result = await _herbService.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<HerbDto>(result.ErrorMessage ?? "药材不存在");
                }
                return Success(result.Data, "查询成功");
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
        public async Task<ActionResult<ApiResponse<HerbDto>>> Create([FromBody] HerbInputDto dto)
        {
            try
            {
                // FluentValidation自动验证已在全局配置，无需手动检查ModelState
                var result = await _herbService.CreateAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建药材", result.Data, result.Data.Id);
                    return Success(result.Data, "药材创建成功");
                }

                return BusinessFail<HerbDto>(result.ErrorMessage ?? "创建药材失败");
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
        public async Task<ActionResult<ApiResponse<HerbDto>>> Update(Guid id, [FromBody] HerbInputDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ValidationFail<HerbDto>("药材ID不能为空");
                }

                // FluentValidation自动验证已在全局配置，无需手动检查ModelState
                // 确保使用路由中的ID
                dto.Id = id;

                var result = await _herbService.UpdateAsync(id, dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新药材信息", result.Data, id);
                    return Success(result.Data, "药材信息更新成功");
                }

                return BusinessFail<HerbDto>(result.ErrorMessage ?? "更新药材信息失败");
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
                if (id == Guid.Empty)
                {
                    return ValidationFail("药材ID不能为空");
                }

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
        /// 批量删除药材（软删除）(Issue #1169)
        /// </summary>
        /// <param name="request">批量删除请求</param>
        /// <summary>
        /// 批量删除药材（软删除）- 已废弃
        /// </summary>
        /// <remarks>
        /// 此端点从未被Client调用，Client使用循环单删模式。
        /// 根据 OpenSpec refactor-webapi-layer 决策，此端点已移除。
        /// </remarks>
        [Obsolete("此端点未被Client使用，已在 OpenSpec refactor-webapi-layer 中标记废弃")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(400)]
        public ActionResult<ApiResponse<BatchOperationResultDto>> BatchDeleteHerbs([FromBody] BatchDeleteRequestDto request)
        {
            return ValidationFail<BatchOperationResultDto>("此端点已废弃，请使用单个删除API循环调用");
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

        // ========== Epic #1962: 批量导入/导出和引用检查端点 ==========

        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        /// <remarks>
        /// <para><strong>功能说明</strong></para>
        /// <list type="bullet">
        ///   <item>支持从Desktop层接收DTO列表（Excel解析在Desktop层完成）</item>
        ///   <item>自动生成拼音码（调用Shared层PinYinHelper）</item>
        ///   <item>支持3种重复策略：Skip（跳过）、Update（更新）、Error（报错）</item>
        /// </list>
        ///
        /// <para><strong>业务规则</strong></para>
        /// <list type="bullet">
        ///   <item><strong>BR-006</strong>: 单次导入最多10000条，超出返回400错误</item>
        ///   <item><strong>BR-001</strong>: 药材名称1-50字符，必填</item>
        ///   <item><strong>BR-002</strong>: 药材名称唯一性检查（重复策略控制）</item>
        /// </list>
        ///
        /// <para><strong>性能要求</strong></para>
        /// <list type="bullet">
        ///   <item>1000条记录 &lt; 10秒</item>
        ///   <item>使用数据库事务保证一致性</item>
        /// </list>
        /// </remarks>
        /// <param name="request">批量导入请求（包含药材DTO列表和重复处理策略）</param>
        /// <returns>批量导入结果（成功数、跳过数、失败数、错误详情）</returns>
        /// <response code="200">导入成功，返回统计信息</response>
        /// <response code="400">请求参数错误（超出数量限制、DTO验证失败）</response>
        /// <response code="500">服务器内部错误</response>
        [HttpPost("batch-import")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<HerbBatchImportResultDto>>> BatchImport([FromBody] HerbBatchImportRequestDto request)
        {
            try
            {
                // 验证请求
                if (request.Herbs == null || request.Herbs.Count == 0)
                {
                    return ValidationFail<HerbBatchImportResultDto>("药材列表不能为空");
                }

                // BR-006: 批量导入数量限制
                if (request.Herbs.Count > 10000)
                {
                    return ValidationFail<HerbBatchImportResultDto>("批量导入最多支持10000条记录");
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
                return HandleException<HerbBatchImportResultDto>(ex, "批量导入药材", new { HerbCount = request.Herbs?.Count, Strategy = request.Strategy });
            }
        }

        /// <summary>
        /// 导出药材数据（返回JSON列表，Desktop层负责Excel生成）
        /// </summary>
        /// <remarks>
        /// <para><strong>功能说明</strong></para>
        /// <list type="bullet">
        ///   <item>返回JSON格式的药材列表（不返回Excel文件）</item>
        ///   <item>Desktop层使用EPPlus生成Excel</item>
        ///   <item>支持按分类过滤（可选）</item>
        /// </list>
        ///
        /// <para><strong>性能要求</strong></para>
        /// <list type="bullet">
        ///   <item>10000条记录 &lt; 2秒</item>
        ///   <item>使用AsNoTracking()提升查询性能</item>
        /// </list>
        /// </remarks>
        /// <param name="category">分类筛选（可选），例如"补血药"</param>
        /// <returns>药材DTO列表</returns>
        /// <response code="200">查询成功，返回药材列表（可能为空数组）</response>
        /// <response code="500">服务器内部错误</response>
        [HttpGet("export-all")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<List<HerbDto>>>> GetAllForExport([FromQuery] string? category = null)
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
                return HandleException<List<HerbDto>>(ex, "获取导出数据", new { Category = category });
            }
        }

        /// <summary>
        /// 检查药材是否被处方引用
        /// </summary>
        /// <remarks>
        /// <para><strong>功能说明</strong></para>
        /// <list type="bullet">
        ///   <item>查询Prescription模块的引用关系（跨模块依赖）</item>
        ///   <item>返回引用统计和最近引用记录（最多10条）</item>
        ///   <item><strong>BR-007</strong>: CanDelete始终为true（支持软删除）</item>
        /// </list>
        ///
        /// <para><strong>业务规则</strong></para>
        /// <list type="bullet">
        ///   <item>有引用时：提示"药材被X个处方引用，删除后将软删除"</item>
        ///   <item>无引用时：正常删除提示</item>
        /// </list>
        ///
        /// <para><strong>性能要求</strong></para>
        /// <list type="bullet">
        ///   <item>单次检查 &lt; 500ms</item>
        /// </list>
        /// </remarks>
        /// <param name="id">药材ID</param>
        /// <returns>引用检查结果DTO</returns>
        /// <response code="200">检查成功，返回引用信息</response>
        /// <response code="400">请求参数错误（ID格式无效）</response>
        /// <response code="404">药材不存在</response>
        /// <response code="500">服务器内部错误</response>
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<HerbReferenceCheckDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<HerbReferenceCheckDto>>> CheckReference(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ValidationFail<HerbReferenceCheckDto>("药材ID不能为空");
                }

                var result = await _herbService.CheckReferenceAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<HerbReferenceCheckDto>(result.ErrorMessage ?? "引用检查失败");
                }
                return Success(result.Data, "引用检查完成");
            }
            catch (Exception ex)
            {
                return HandleException<HerbReferenceCheckDto>(ex, "检查药材引用", id);
            }
        }

        /// <summary>
        /// 批量检查药材引用关系
        /// </summary>
        /// <remarks>
        /// <para><strong>功能说明</strong></para>
        /// <list type="bullet">
        ///   <item>批量查询多个药材的引用关系（批量删除前调用）</item>
        ///   <item>返回每个药材的引用统计和最近引用记录</item>
        ///   <item><strong>BR-007</strong>: 所有药材的CanDelete均为true（支持软删除）</item>
        /// </list>
        ///
        /// <para><strong>业务规则</strong></para>
        /// <list type="bullet">
        ///   <item><strong>BR-006</strong>: 单次检查最多100条，超出返回400错误</item>
        ///   <item>有引用的药材会在结果中标注引用数量</item>
        /// </list>
        ///
        /// <para><strong>性能要求</strong></para>
        /// <list type="bullet">
        ///   <item>100条记录检查 &lt; 5秒</item>
        /// </list>
        /// </remarks>
        /// <param name="request">批量检查请求（包含药材ID列表，≤100）</param>
        /// <returns>引用检查结果列表</returns>
        /// <response code="200">检查成功，返回引用信息列表</response>
        /// <response code="400">请求参数错误（超出数量限制、ID列表为空）</response>
        /// <response code="500">服务器内部错误</response>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbReferenceCheckDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<List<HerbReferenceCheckDto>>>> BatchCheckReference([FromBody] BatchCheckReferenceRequestDto request)
        {
            try
            {
                // 验证请求
                if (request.HerbIds == null || request.HerbIds.Count == 0)
                {
                    return ValidationFail<List<HerbReferenceCheckDto>>("药材ID列表不能为空");
                }

                // BR-006: 批量检查数量限制
                if (request.HerbIds.Count > 100)
                {
                    return ValidationFail<List<HerbReferenceCheckDto>>("批量检查最多支持100条记录");
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
                    return BusinessFail<List<HerbReferenceCheckDto>>(result.ErrorMessage ?? "批量引用检查失败");
                }
                return Success(result.Data, "批量引用检查完成");
            }
            catch (Exception ex)
            {
                return HandleException<List<HerbReferenceCheckDto>>(ex, "批量检查药材引用", new { Count = request.HerbIds?.Count });
            }
        }
    }
}
