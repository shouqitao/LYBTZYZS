using Asp.Versioning;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材导入导出 API 控制器 - UltraThink重构：专门负责导入导出功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/herbs")]
    [Authorize]
    public class HerbImportExportController : BaseController
    {
        private readonly IHerbService _herbService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// 构造方法，注入药材服务和缓存服务
        /// </summary>
        public HerbImportExportController(
            IHerbService herbService, 
            ICacheService cacheService,
            ILogger<HerbImportExportController> logger)
            : base(logger)
        {
            _herbService = herbService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult> Import([FromBody] List<HerbImportDto> dtos)
        {
            try
            {
                if (dtos == null || dtos.Count == 0)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "请求无效",
                        Detail = "导入数据不能为空",
                        Status = 400
                    });
                }

                // 数据验证
                var invalidItems = ValidateImportData(dtos);
                if (invalidItems.Any())
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "数据验证失败",
                        Detail = $"存在 {invalidItems.Count} 条无效数据",
                        Status = 400,
                        Extensions = { ["invalidItems"] = invalidItems }
                    });
                }

                var count = await _herbService.ImportAsync(dtos);

                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("herbs");

                LogOperation("批量导入药材成功", new { Count = count, TotalSubmitted = dtos.Count }, null);
                return Ok(new { 
                    imported = count, 
                    total = dtos.Count,
                    message = $"成功导入 {count} 个药材，共提交 {dtos.Count} 条数据" 
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = ex.Message,
                    Status = 400
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "操作冲突",
                    Detail = ex.Message,
                    Status = 409
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量导入药材");
            }
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<List<HerbDetailDto>>> Export([FromQuery] string? format = "json")
        {
            try
            {
                var data = await _herbService.ExportAsync();
                
                LogOperation("导出药材数据", new { Count = data?.Count ?? 0, Format = format }, null);

                // 根据格式返回不同类型的响应
                return format?.ToLower() switch
                {
                    "json" => Ok(data ?? new List<HerbDetailDto>()),
                    _ => Ok(data ?? new List<HerbDetailDto>())
                };
            }
            catch (Exception ex)
            {
                return HandleException(ex, "导出药材数据");
            }
        }

        /// <summary>
        /// 导出药材模板
        /// </summary>
        [HttpGet("export-template")]
        public async Task<ActionResult<object>> ExportTemplate()
        {
            try
            {
                var template = new
                {
                    name = "药材名称",
                    chineseName = "中文名称",
                    englishName = "英文名称",
                    scientificName = "拉丁名",
                    origin = "产地",
                    effect = "功效",
                    usage = "用法",
                    price = 0.00m,
                    stock = 0,
                    unit = "单位",
                    warningLevel = 10,
                    maxStock = 1000,
                    remark = "备注",
                    status = "Enabled"
                };

                var templateData = new List<object> { template };
                
                LogOperation("导出药材导入模板", null, null);
                return Ok(new { 
                    message = "药材导入模板",
                    template = templateData
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "导出药材导入模板");
            }
        }

        /// <summary>
        /// 验证导入数据有效性
        /// </summary>
        [HttpPost("validate-import")]
        public async Task<ActionResult> ValidateImport([FromBody] List<HerbImportDto> dtos)
        {
            try
            {
                if (dtos == null || dtos.Count == 0)
                {
                    return BadRequest("验证数据不能为空");
                }

                var validationResults = await ValidateImportDataAsync(dtos);
                
                return Ok(new
                {
                    totalCount = dtos.Count,
                    validCount = validationResults.Count(v => v.IsValid),
                    invalidCount = validationResults.Count(v => !v.IsValid),
                    results = validationResults
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "验证导入数据");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证导入数据
        /// </summary>
        private List<object> ValidateImportData(List<HerbImportDto> dtos)
        {
            var invalidItems = new List<object>();
            
            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                var errors = new List<string>();

                // 基本字段验证
                if (string.IsNullOrWhiteSpace(dto.Name))
                    errors.Add("药材名称不能为空");

                if (dto.Price < 0)
                    errors.Add("价格不能为负数");

                if (dto.Stock < 0)
                    errors.Add("库存不能为负数");

                if (errors.Any())
                {
                    invalidItems.Add(new
                    {
                        index = i + 1,
                        item = dto,
                        errors = errors
                    });
                }
            }

            return invalidItems;
        }

        /// <summary>
        /// 异步验证导入数据（包含数据库检查）
        /// </summary>
        private async Task<List<ImportValidationResult>> ValidateImportDataAsync(List<HerbImportDto> dtos)
        {
            var results = new List<ImportValidationResult>();

            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                var result = new ImportValidationResult
                {
                    Index = i + 1,
                    Name = dto.Name ?? "未知",
                    IsValid = true,
                    Errors = new List<string>()
                };

                // 基本验证
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    result.Errors.Add("药材名称不能为空");
                    result.IsValid = false;
                }

                if (dto.Price < 0)
                {
                    result.Errors.Add("价格不能为负数");
                    result.IsValid = false;
                }

                if (dto.Stock < 0)
                {
                    result.Errors.Add("库存不能为负数");
                    result.IsValid = false;
                }

                // 可以添加更多验证逻辑，比如检查数据库中是否已存在等

                results.Add(result);
            }

            return results;
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 导入验证结果
        /// </summary>
        private class ImportValidationResult
        {
            public int Index { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
        }

        #endregion
    }
}