using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 验方管理 API - 基础CRUD功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class FormulasController : BaseApiController
    {
        private readonly IFormulaService _service;

        public FormulasController(IFormulaService service, IMemoryCache cache, ILogger<FormulasController> logger)
            : base(logger, cache)
        {
            _service = service;
        }

        /// <summary>
        /// 获取验方列表 - 支持分页和查询（Issue #1164: 扩展支持分类筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="category">分类筛选</param>
        [HttpGet]
        [ResponseCache(Duration = 7200, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "FormulasCache")]
        public async Task<ActionResult<ApiResponse<PagedResult<FormulaDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<FormulaDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _service.GetPagedAsync(page, pageSize, keyword, category);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<FormulaDto>(ex, "获取验方列表", new { page, pageSize, keyword, category });
            }
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 1800, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<ApiResponse<FormulaDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<FormulaDto>(id, "验方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<FormulaDto>(result.ErrorMessage ?? "验方不存在", ApiErrorCodes.FORMULANOTFOUND);
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDto>(ex, "获取验方详情", id);
            }
        }

        /// <summary>
        /// 新增验方
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<FormulaDto>>> Add([FromBody] FormulaCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<FormulaDto>();
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.CreateAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<FormulaDto>(result.ErrorMessage ?? "新增验方失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("新增验方成功", result.Data, result.Data.Id);
                return Success(result.Data, "验方创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDto>(ex, "新增验方", dto);
            }
        }

        /// <summary>
        /// 更新验方
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<FormulaDto>>> Update(Guid id, [FromBody] FormulaUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<FormulaDto>(id, "验方ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<FormulaDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<FormulaDto>(result.ErrorMessage ?? "更新验方失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation("更新验方成功", result.Data, id);
                return Success(result.Data, "验方更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDto>(ex, "更新验方", new { id, dto });
            }
        }

        /// <summary>
        /// 删除验方
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "验方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound("验方不存在", ApiErrorCodes.FORMULANOTFOUND);
                }

                LogOperation("删除验方成功", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除验方", id);
            }
        }


        /// <summary>
        /// 批量删除验方（软删除）(Issue #1169)
        /// </summary>
        /// <param name="request">批量删除请求</param>
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<BatchOperationResultDto>>> BatchDeleteFormulas([FromBody] BatchDeleteRequestDto request)
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

                var result = await _service.BatchDeleteAsync(request.Ids);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("批量删除验方", 
                        new { TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount }, 
                        null);
                }

                return HandleServiceResult(result, result.Data?.Message ?? "批量删除完成");
            }
            catch (Exception ex)
            {
                return HandleException<BatchOperationResultDto>(ex, "批量删除验方", new { IdCount = request.Ids?.Count });
            }
        }


        /// <summary>
        /// 批量导入验方数据 (Issue #1166)
        /// </summary>
        /// <param name="file">Excel文件（.xlsx格式）</param>
        /// <returns>导入结果，包含成功/失败数量和详细错误信息</returns>
        [HttpPost("import")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 限制10MB
        public async Task<ActionResult<ApiResponse<FormulaImportResultDto>>> Import(IFormFile file)
        {
            try
            {
                // 验证文件
                if (file == null || file.Length == 0)
                {
                    return ValidationFail<FormulaImportResultDto>("文件不能为空");
                }

                // 验证文件扩展名
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    return ValidationFail<FormulaImportResultDto>("仅支持.xlsx格式的Excel文件");
                }

                // 验证文件大小（10MB）
                if (file.Length > 10 * 1024 * 1024)
                {
                    return ValidationFail<FormulaImportResultDto>("文件大小不能超过10MB");
                }

                // 导入数据
                using var stream = file.OpenReadStream();
                var result = await _service.ImportFromExcelAsync(stream, file.FileName);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<FormulaImportResultDto>(
                        result.ErrorMessage ?? "导入失败",
                        ApiErrorCodes.DATASAVEFAILED);
                }

                // 记录操作日志
                LogOperation("批量导入验方",
                    new { FileName = file.FileName, TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount },
                    null);

                return Success(result.Data, result.Data.Message);
            }
            catch (Exception ex)
            {
                return HandleException<FormulaImportResultDto>(ex, "批量导入验方", new { FileName = file?.FileName });
            }
        }

        /// <summary>
        /// 导出验方数据到Excel (Issue #1166)
        /// </summary>
        /// <param name="category">可选的分类筛选参数</param>
        /// <returns>包含验方数据的Excel文件</returns>
        [HttpGet("export")]
        public async Task<ActionResult> Export([FromQuery] string? category = null)
        {
            try
            {
                var stream = await _service.ExportAsync(category);
                var fileName = string.IsNullOrWhiteSpace(category)
                    ? $"验方数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : $"验方数据_{category}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // 记录操作日志
                LogOperation("导出验方数据", new { Category = category, FileName = fileName }, null);

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出验方数据失败，分类筛选：{Category}", category);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// 下载验方导入模板 (Issue #1166)
        /// </summary>
        /// <returns>包含示例数据的Excel模板文件</returns>
        [HttpGet("import-template")]
        [AllowAnonymous] // 模板下载不需要认证
        public ActionResult ExportTemplate()
        {
            try
            {
                var stream = _service.GenerateImportTemplate();
                var fileName = $"验方导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成验方导入模板失败");
                return StatusCode(500);
            }
        }


        /// <summary>
        /// 克隆验方 - 复制验方作为新验方 (Issue #1167)
        /// </summary>
        /// <param name="id">原验方ID</param>
        /// <returns>新创建的验方副本</returns>
        [HttpPost("{id}/copy")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<FormulaDto>>> CopyFormula(Guid id)
        {
            try
            {
                var validation = ValidateGuid<FormulaDto>(id, "验方ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _service.CloneFormulaAsync(id);

                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<FormulaDto>(
                        result.ErrorMessage ?? "验方不存在",
                        ApiErrorCodes.FORMULANOTFOUND);
                }

                // 记录操作日志
                LogOperation("克隆验方",
                    new { OriginalId = id, NewId = result.Data.Id },
                    result.Data.Id);

                return Success(result.Data, "验方克隆成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDto>(ex, "克隆验方", id);
            }
        }

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// 查询所有 ValidationStatus = Draft 的验方，包含未验证的药材项
        /// </summary>
        [HttpGet("pending-validation")]
        [ResponseCache(Duration = 60)] // 缓存60秒
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<FormulaDto>>>> GetPendingValidation()
        {
            try
            {
                var result = await _service.GetPendingValidationFormulasAsync();

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<List<FormulaDto>>(
                        result.ErrorMessage ?? "获取待校验验方列表失败",
                        ApiErrorCodes.DATAQUERYFAILED);
                }

                return Success(result.Data, $"查询成功，共{result.Data.Count}个待校验验方");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "获取待校验验方列表", null);
            }
        }

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <param name="herbItemId">待验证的药材项ID</param>
        /// <param name="selectedHerbId">选中的系统药材ID</param>
        [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> ValidateHerb(
            Guid formulaId,
            Guid herbItemId,
            [FromBody] Guid selectedHerbId)
        {
            try
            {
                var formulaValidation = ValidateGuid(formulaId, "验方ID");
                if (formulaValidation != null)
                {
                    return formulaValidation;
                }

                var herbItemValidation = ValidateGuid(herbItemId, "药材项ID");
                if (herbItemValidation != null)
                {
                    return herbItemValidation;
                }

                var selectedHerbValidation = ValidateGuid(selectedHerbId, "系统药材ID");
                if (selectedHerbValidation != null)
                {
                    return selectedHerbValidation;
                }

                var result = await _service.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

                if (!result.IsSuccess)
                {
                    return BusinessFail(result.ErrorMessage ?? "验证药材失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                // 记录操作日志
                LogOperation("验证验方药材",
                    new { FormulaId = formulaId, HerbItemId = herbItemId, SelectedHerbId = selectedHerbId },
                    formulaId);

                return Success(result.Message ?? "药材验证成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "验证验方药材", new { formulaId, herbItemId, selectedHerbId });
            }
        }
    }
}
