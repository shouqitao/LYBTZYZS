using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 验方管理 API - 统一API响应格式
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
        /// 获取验方列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<FormulaDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? category = null,
            [FromQuery] string? formulaType = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<FormulaDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var query = new FormulaQueryDto
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Name = keyword, // 使用Name字段进行搜索
                    Effect = category // 使用Effect字段作为分类筛选
                };

                var result = await _service.GetPagedAsync(query);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<FormulaDto>(ex, "获取验方列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [HttpGet("{id}")]
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
                    return NotFound<FormulaDto>(result.ErrorMessage ?? "验方不存在", ApiErrorCodes.FORMULA_NOT_FOUND);
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
                    return BusinessFail<FormulaDto>(result.ErrorMessage ?? "新增验方失败", ApiErrorCodes.DATA_SAVE_FAILED);
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
                    return BusinessFail<FormulaDto>(result.ErrorMessage ?? "更新验方失败", ApiErrorCodes.DATA_UPDATE_FAILED);
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
                if (!result.IsSuccess || !result.Data)
                {
                    return NotFound("验方不存在", ApiErrorCodes.FORMULA_NOT_FOUND);
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
        /// 获取验方模板
        /// </summary>
        [HttpGet("templates")]
        public async Task<ActionResult<ApiResponse<List<FormulaDto>>>> GetTemplates()
        {
            try
            {
                var result = await _service.GetTemplatesAsync();
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<List<FormulaDto>>(result.ErrorMessage ?? "获取验方模板失败", ApiErrorCodes.INTERNAL_ERROR);
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "获取验方模板", null);
            }
        }

        /// <summary>
        /// 根据类型获取验方
        /// </summary>
        [HttpGet("by-type/{type}")]
        public async Task<ActionResult<ApiResponse<List<FormulaDto>>>> GetByType(string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(type))
                {
                    return ValidationFail<List<FormulaDto>>("验方类型不能为空");
                }

                var result = await _service.GetByTypeAsync(type);
                if (!result.IsSuccess || result.Data == null)
                {
                    return Success(new List<FormulaDto>(), "未找到匹配验方");
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "根据类型查询验方", type);
            }
        }

        /// <summary>
        /// 从处方创建验方
        /// </summary>
        [HttpPost("from-prescription/{prescriptionId}")]
        public async Task<ActionResult<ApiResponse<FormulaDto>>> CreateFromPrescription(Guid prescriptionId, [FromBody] CreateFromPrescriptionDto dto)
        {
            try
            {
                var validationResult = ValidateGuid<FormulaDto>(prescriptionId, "处方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ValidationFail<FormulaDto>("验方名称不能为空");
                }

                var result = await _service.CreateFromPrescriptionAsync(prescriptionId, dto.Name);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<FormulaDto>(result.ErrorMessage ?? "从处方创建验方失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                LogOperation("从处方创建验方成功", result.Data, prescriptionId);
                return Success(result.Data, "验方创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDto>(ex, "从处方创建验方", new { prescriptionId, dto });
            }
        }

        /// <summary>
        /// 分析验方
        /// </summary>
        [HttpPost("{id}/analyze")]
        public async Task<ActionResult<ApiResponse<FormulaAnalysisResult>>> AnalyzeFormula(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<FormulaAnalysisResult>(id, "验方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                // 接口简化后不再支持复杂分析功能
                return BusinessFail<FormulaAnalysisResult>("简单诊所版本不支持验方分析功能", ApiErrorCodes.FEATURE_NOT_IMPLEMENTED);
            }
            catch (Exception ex)
            {
                return HandleException<FormulaAnalysisResult>(ex, "分析验方", id);
            }
        }

        /// <summary>
        /// 获取验方推荐（按症候）
        /// </summary>
        [HttpGet("recommendations/syndrome/{syndrome}")]
        public async Task<ActionResult<ApiResponse<List<FormulaRecommendationDto>>>> GetRecommendationsBySyndrome(string syndrome)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(syndrome))
                {
                    return ValidationFail<List<FormulaRecommendationDto>>("症候不能为空");
                }

                // 接口简化后不再支持推荐功能
                return Success(new List<FormulaRecommendationDto>(), "简单诊所版本不支持验方推荐功能");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaRecommendationDto>>(ex, "获取验方推荐", syndrome);
            }
        }

        /// <summary>
        /// 获取验方推荐（按症状和诊断）
        /// </summary>
        [HttpGet("recommendations")]
        public async Task<ActionResult<ApiResponse<List<FormulaRecommendationDto>>>> GetRecommendations(
            [FromQuery] string symptoms,
            [FromQuery] string diagnosis,
            [FromQuery] Guid doctorId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symptoms) || string.IsNullOrWhiteSpace(diagnosis))
                {
                    return ValidationFail<List<FormulaRecommendationDto>>("症状和诊断不能为空");
                }

                // 接口简化后不再支持复杂推荐功能
                return Success(new List<FormulaRecommendationDto>(), "简单诊所版本不支持复杂验方推荐功能");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaRecommendationDto>>(ex, "获取验方推荐", new { symptoms, diagnosis, doctorId });
            }
        }

        /// <summary>
        /// 搜索验方
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<FormulaDto>>>> Search(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<FormulaDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var query = new PagedQueryBaseDto
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    Keyword = keyword
                };

                // 使用SearchAsync替代SearchFormulasAsync (接口简化后的调整)
                var searchResult = await _service.SearchAsync(keyword ?? "");
                if (!searchResult.IsSuccess || searchResult.Data == null)
                {
                    var emptyResult = new PagedResult<FormulaDto>
                    {
                        Items = [],
                        TotalCount = 0,
                        CurrentPage = page,
                        PageSize = pageSize
                    };
                    var result = ServiceResult<PagedResult<FormulaDto>>.Success(emptyResult);
                    return HandlePagedServiceResult(result, "搜索完成");
                }

                // 手动分页
                var totalCount = searchResult.Data.Count;
                var skip = (page - 1) * pageSize;
                var items = searchResult.Data.Skip(skip).Take(pageSize).ToList();

                var pagedData = new PagedResult<FormulaDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };
                var pagedResult = ServiceResult<PagedResult<FormulaDto>>.Success(pagedData);
                return HandlePagedServiceResult(pagedResult, "搜索成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<FormulaDto>(ex, "搜索验方", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 复制验方
        /// </summary>
        [HttpPost("{id}/copy")]
        public async Task<ActionResult<ApiResponse<FormulaDto>>> Copy(Guid id, [FromBody] CopyFormulaDto dto)
        {
            try
            {
                var validationResult = ValidateGuid<FormulaDto>(id, "验方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                if (string.IsNullOrWhiteSpace(dto.NewName))
                {
                    return ValidationFail<FormulaDto>("新验方名称不能为空");
                }

                // 接口简化后不再支持复制功能
                return BusinessFail<FormulaDto>("简单诊所版本不支持验方复制功能", ApiErrorCodes.FEATURE_NOT_IMPLEMENTED);
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDto>(ex, "复制验方", new { id, dto });
            }
        }

        /// <summary>
        /// 切换验方状态
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse>> ToggleStatus(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "验方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                // 接口简化后不再支持切换状态功能，建议使用EnableAsync/DisableAsync
                return BusinessFail("简单诊所版本不支持状态切换功能，请使用启用或禁用功能", ApiErrorCodes.FEATURE_NOT_IMPLEMENTED);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "切换验方状态", id);
            }
        }

        /// <summary>
        /// 获取验方分类
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
        {
            try
            {
                var result = await _service.GetCategoriesAsync();
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<List<string>>(result.ErrorMessage ?? "获取分类失败", ApiErrorCodes.INTERNAL_ERROR);
                }

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<string>>(ex, "获取验方分类", null);
            }
        }

        /// <summary>
        /// 分享验方
        /// </summary>
        [HttpPost("{id}/share")]
        public async Task<ActionResult<ApiResponse>> ShareFormula(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "验方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                // 接口简化后不再支持分享功能
                return BusinessFail("简单诊所版本不支持验方分享功能", ApiErrorCodes.FEATURE_NOT_IMPLEMENTED);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "分享验方", id);
            }
        }

        /// <summary>
        /// 取消分享验方
        /// </summary>
        [HttpPost("{id}/unshare")]
        public async Task<ActionResult<ApiResponse>> UnshareFormula(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "验方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                // 接口简化后不再支持分享功能
                return BusinessFail("简单诊所版本不支持验方分享功能", ApiErrorCodes.FEATURE_NOT_IMPLEMENTED);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "取消分享验方", id);
            }
        }

        /// <summary>
        /// 导入验方数据 - 批量创建验方
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<object>>> ImportFormulas([FromBody] List<FormulaCreateDto> formulas)
        {
            try
            {
                if (formulas == null || !formulas.Any())
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

                        var result = await _service.CreateAsync(formulaDto);
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
        [HttpGet("export")]
        public async Task<ActionResult<ApiResponse<List<FormulaDto>>>> ExportFormulas([FromQuery] string? category = null)
        {
            try
            {
                var query = new FormulaQueryDto
                {
                    PageIndex = 1,
                    PageSize = 10000, // 导出时获取大量数据
                    Keyword = string.Empty,
                    Effect = category // 按分类筛选
                };

                var result = await _service.GetPagedAsync(query);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<List<FormulaDto>>(result.ErrorMessage ?? "获取验方数据失败", ApiErrorCodes.DATA_QUERY_FAILED);
                }

                var formulas = result.Data.Items;
                LogOperation("导出验方数据", new { Count = formulas.Count, Category = category }, null);

                return Success(formulas, $"成功获取{formulas.Count}条验方数据");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "导出验方数据", category);
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
                if (formulas == null || !formulas.Any())
                {
                    return ValidationFail<object>("验证数据不能为空");
                }

                if (formulas.Count > 1000)
                {
                    return ValidationFail<object>("单次导入不能超过1000条记录");
                }

                var validationResult = new List<object>();
                var duplicateNames = new HashSet<string>();

                for (int i = 0; i < formulas.Count; i++)
                {
                    var formula = formulas[i];
                    var rowErrors = new List<string>();

                    // 必填字段验证
                    if (string.IsNullOrWhiteSpace(formula.Name))
                    {
                        rowErrors.Add("验方名称不能为空");
                    }
                    else
                    {
                        // 检查重名
                        if (duplicateNames.Contains(formula.Name))
                        {
                            rowErrors.Add($"验方名称 '{formula.Name}' 重复");
                        }
                        else
                        {
                            duplicateNames.Add(formula.Name);
                        }

                        // 检查数据库中是否已存在
                        var existingResult = await _service.SearchAsync(formula.Name);
                        if (existingResult.IsSuccess && existingResult.Data != null &&
                            existingResult.Data.Any(f => f.Name.Equals(formula.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            rowErrors.Add($"验方名称 '{formula.Name}' 在数据库中已存在");
                        }
                    }

                    // 字段长度验证
                    if (!string.IsNullOrEmpty(formula.Name) && formula.Name.Length > 100)
                    {
                        rowErrors.Add("验方名称长度不能超过100个字符");
                    }
                    if (!string.IsNullOrEmpty(formula.Effect) && formula.Effect.Length > 500)
                    {
                        rowErrors.Add("功效描述长度不能超过500个字符");
                    }

                    validationResult.Add(new
                    {
                        Row = i + 1,
                        Name = formula.Name,
                        IsValid = !rowErrors.Any(),
                        Errors = rowErrors
                    });
                }

                var summary = new
                {
                    TotalRows = formulas.Count,
                    ValidRows = validationResult.Count(r => (bool)((dynamic)r).IsValid),
                    InvalidRows = validationResult.Count(r => !((bool)((dynamic)r).IsValid)),
                    ValidationDetails = validationResult.Where(r => !((bool)((dynamic)r).IsValid)).ToList()
                };

                return Success<object>(summary, "数据验证完成");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "验证导入数据", formulas?.Count);
            }
        }
    }

    /// <summary>
    /// 从处方创建验方DTO
    /// </summary>
    public class CreateFromPrescriptionDto
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 复制验方DTO
    /// </summary>
    public class CopyFormulaDto
    {
        public string NewName { get; set; } = string.Empty;
    }
}
