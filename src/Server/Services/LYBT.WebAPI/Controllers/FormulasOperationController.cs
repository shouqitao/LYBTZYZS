using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 验方操作API - 处理导入、导出、批量操作等
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/formulas/operation")]
    [Authorize]
    public class FormulasOperationController : BaseApiController
    {
        private readonly IFormulaBusinessService _businessService;
        private readonly IFormulaQueryService _queryService;

        public FormulasOperationController(
            IFormulaBusinessService businessService,
            IFormulaQueryService queryService,
            IMemoryCache cache,
            ILogger<FormulasOperationController> logger)
            : base(logger, cache)
        {
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        }

        /// <summary>
        /// 导入验方数据 - 批量创建验方
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<object>>> ImportFormulas([FromBody] List<FormulaCreateDto> formulas)
        {
            try
            {
                if (formulas == null || formulas.Count == 0)
                {
                    return ValidationFail<object>("导入数据不能为空");
                }

                if (formulas.Count > 1000)
                {
                    return ValidationFail<object>("单次导入不能超过1000条记录");
                }

                int successCount = 0;
                int failureCount = 0;
                var errors = new List<string>();

                foreach (var formulaDto in formulas)
                {
                    try
                    {
                        // 数据验证
                        if (string.IsNullOrWhiteSpace(formulaDto.Name))
                        {
                            errors.Add($"验方名称不能为空");
                            failureCount++;
                            continue;
                        }

                        var result = await _businessService.CreateAsync(formulaDto);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"验方 {formulaDto.Name}: {result.ErrorMessage}");
                            failureCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"验方 {formulaDto.Name}: {ex.Message}");
                        failureCount++;
                    }
                }

                var importResult = new
                {
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Errors = errors.Take(10).ToList(), // 最多返回10个错误
                    TotalErrors = errors.Count
                };

                LogOperation("批量导入验方", importResult, null);

                return Success<object>(importResult, $"导入完成: 成功{successCount}条, 失败{failureCount}条");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "导入验方数据", formulas?.Count);
            }
        }

        /// <summary>
        /// 导出验方数据 - 获取所有验方用于Excel导出
        /// </summary>
        [HttpPost("export")]
        public async Task<ActionResult<ApiResponse<List<FormulaDto>>>> ExportFormulas([FromBody] FormulaExportDto exportDto)
        {
            try
            {
                var query = new FormulaQueryDto
                {
                    PageIndex = 1,
                    PageSize = 10000, // 导出时获取大量数据
                    Keyword = string.Empty,
                    Effect = exportDto?.Category // 按分类筛选
                };

                var result = await _queryService.GetPagedAsync(query);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<List<FormulaDto>>(result.ErrorMessage ?? "获取验方数据失败", ApiErrorCodes.DATAQUERYFAILED);
                }

                var formulas = result.Data.Items;
                LogOperation("导出验方数据", new { Count = formulas.Count, Category = exportDto?.Category }, null);

                return Success(formulas, $"成功获取{formulas.Count}条验方数据");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "导出验方数据", exportDto);
            }
        }

        /// <summary>
        /// 获取验方导入模板 - 返回Excel导入的标准格式
        /// </summary>
        [HttpGet("template")]
        public async Task<ActionResult<ApiResponse<object>>> GetImportTemplate()
        {
            try
            {
                await Task.CompletedTask; // 满足async约定
                
                var template = new
                {
                    Columns = new[]
                    {
                        new { Field = "Name", Header = "验方名称", Required = true, Example = "桂枝汤" },
                        new { Field = "Category", Header = "分类", Required = false, Example = "解表剂" },
                        new { Field = "Effect", Header = "功效", Required = false, Example = "解肌发表，调和营卫" },
                        new { Field = "Usage", Header = "用法", Required = false, Example = "水煎服，日二服" },
                        new { Field = "Indications", Header = "主治症状", Required = false, Example = "外感风寒，营卫不和" },
                        new { Field = "Contraindications", Header = "禁忌症", Required = false, Example = "热病及阴虚内热者忌用" },
                        new { Field = "IsShared", Header = "是否共享", Required = false, Example = "是" }
                    },
                    SampleData = new[]
                    {
                        new
                        {
                            Name = "桂枝汤",
                            Category = "解表剂",
                            Effect = "解肌发表，调和营卫",
                            Usage = "水煎服，日二服",
                            Indications = "外感风寒，营卫不和",
                            Contraindications = "热病及阴虚内热者忌用",
                            IsShared = "是"
                        },
                        new
                        {
                            Name = "麻黄汤",
                            Category = "解表剂",
                            Effect = "发汗解表，宣肺平喘",
                            Usage = "水煎服，温服",
                            Indications = "外感风寒表实证",
                            Contraindications = "表虚有汗者忌用",
                            IsShared = "是"
                        }
                    },
                    Instructions = new[]
                    {
                        "1. 验方名称为必填字段",
                        "2. 是否共享字段填写：是/否、true/false、1/0",
                        "3. 导入后的验方暂无药材组成，需手动添加",
                        "4. 建议使用提供的示例数据作为参考格式",
                        "5. 单次导入不超过1000条记录"
                    }
                };

                return Success<object>(template, "获取导入模板成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取验方导入模板", null);
            }
        }

        /// <summary>
        /// 验证导入数据格式 - 在正式导入前进行数据验证
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<object>>> ValidateImportData([FromBody] List<FormulaCreateDto> formulas)
        {
            try
            {
                if (formulas == null || formulas.Count == 0)
                {
                    return ValidationFail<object>("验证数据不能为空");
                }

                await Task.CompletedTask; // 满足async约定

                var validationResults = new List<object>();
                int validCount = 0;
                int invalidCount = 0;

                for (int i = 0; i < formulas.Count; i++)
                {
                    var formula = formulas[i];
                    var errors = new List<string>();

                    // 验证必填字段
                    if (string.IsNullOrWhiteSpace(formula.Name))
                    {
                        errors.Add("验方名称不能为空");
                    }

                    // 验证名称长度
                    if (!string.IsNullOrWhiteSpace(formula.Name) && formula.Name.Length > 100)
                    {
                        errors.Add("验方名称长度不能超过100个字符");
                    }

                    bool isValid = errors.Count == 0;
                    if (isValid)
                    {
                        validCount++;
                    }
                    else
                    {
                        invalidCount++;
                    }

                    validationResults.Add(new
                    {
                        Index = i + 1,
                        Name = formula.Name ?? "未知",
                        IsValid = isValid,
                        Errors = errors
                    });
                }

                var result = new
                {
                    TotalCount = formulas.Count,
                    ValidCount = validCount,
                    InvalidCount = invalidCount,
                    Details = validationResults.Take(100) // 最多返回100条详细信息
                };

                return Success<object>(result, "验证完成");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "验证导入数据", formulas?.Count);
            }
        }

        /// <summary>
        /// 批量分享验方
        /// </summary>
        [HttpPost("share")]
        public async Task<ActionResult<ApiResponse<bool>>> ShareFormulas([FromBody] BatchShareDto dto)
        {
            try
            {
                if (dto == null || dto.FormulaIds == null || dto.FormulaIds.Count == 0)
                {
                    return ValidationFail<bool>("请选择要分享的验方");
                }

                int successCount = 0;
                int failureCount = 0;

                foreach (var formulaId in dto.FormulaIds)
                {
                    var result = await _businessService.ShareFormulaAsync(formulaId, dto.OperatorId, dto.OperatorName);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }

                LogOperation($"批量分享验方成功 {successCount} 条，失败 {failureCount} 条", dto);
                return Success(true, $"批量分享完成: 成功 {successCount} 条，失败 {failureCount} 条");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量分享验方", dto);
            }
        }

        /// <summary>
        /// 批量取消分享验方
        /// </summary>
        [HttpPost("unshare")]
        public async Task<ActionResult<ApiResponse<bool>>> UnshareFormulas([FromBody] BatchShareDto dto)
        {
            try
            {
                if (dto == null || dto.FormulaIds == null || dto.FormulaIds.Count == 0)
                {
                    return ValidationFail<bool>("请选择要取消分享的验方");
                }

                int successCount = 0;
                int failureCount = 0;

                foreach (var formulaId in dto.FormulaIds)
                {
                    var result = await _businessService.UnshareFormulaAsync(formulaId, dto.OperatorId, dto.OperatorName);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }

                LogOperation($"批量取消分享验方成功 {successCount} 条，失败 {failureCount} 条", dto);
                return Success(true, $"批量取消分享完成: 成功 {successCount} 条，失败 {failureCount} 条");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量取消分享验方", dto);
            }
        }

        /// <summary>
        /// 批量删除验方
        /// </summary>
        [HttpPost("delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteBatch([FromBody] List<Guid> formulaIds)
        {
            try
            {
                if (formulaIds == null || formulaIds.Count == 0)
                {
                    return ValidationFail<bool>("请选择要删除的验方");
                }

                if (formulaIds.Count > 100)
                {
                    return ValidationFail<bool>("单次删除数量不能超过100条");
                }

                int successCount = 0;
                int failureCount = 0;

                foreach (var id in formulaIds)
                {
                    var result = await _businessService.DeleteAsync(id);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }

                LogOperation($"批量删除验方成功 {successCount} 条，失败 {failureCount} 条", formulaIds);
                return Success(true, $"批量删除完成: 成功 {successCount} 条，失败 {failureCount} 条");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量删除验方", new { count = formulaIds?.Count });
            }
        }
    }

    /// <summary>
    /// 批量分享DTO
    /// </summary>
    public class BatchShareDto
    {
        /// <summary>
        /// 验方ID列表
        /// </summary>
        public List<Guid> FormulaIds { get; set; } = new();

        /// <summary>
        /// 操作者ID
        /// </summary>
        public Guid OperatorId { get; set; }

        /// <summary>
        /// 操作者姓名
        /// </summary>
        public string OperatorName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验方导出DTO
    /// </summary>
    public class FormulaExportDto
    {
        /// <summary>
        /// 分类筛选
        /// </summary>
        public string? Category { get; set; }
    }
}