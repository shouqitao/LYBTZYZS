using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
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
        public async Task<ActionResult<ApiResponse<PagedData<FormulaDto>>>> GetList(
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
                    PageIndex = page,
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
                if (validationResult != null) return validationResult;

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
                if (validationResult != null) return validationResult;

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
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<FormulaDto>();
                if (modelValidation != null) return modelValidation;

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
                if (validationResult != null) return validationResult;

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
                if (validationResult != null) return validationResult;

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
                if (validationResult != null) return validationResult;

                var result = await _service.AnalyzeFormulaAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<FormulaAnalysisResult>(result.ErrorMessage ?? "验方分析失败", ApiErrorCodes.INTERNAL_ERROR);
                }

                return Success(result.Data, "分析完成");
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

                var result = await _service.GetRecommendationsAsync(syndrome);
                if (!result.IsSuccess || result.Data == null)
                {
                    return Success(new List<FormulaRecommendationDto>(), "未找到推荐验方");
                }

                return Success(result.Data, "推荐成功");
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

                var result = await _service.GetRecommendationsAsync(symptoms, diagnosis, doctorId);
                if (!result.IsSuccess || result.Data == null)
                {
                    return Success(new List<FormulaRecommendationDto>(), "未找到推荐验方");
                }

                return Success(result.Data, "推荐成功");
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
        public async Task<ActionResult<ApiResponse<PagedData<FormulaDto>>>> Search(
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
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword
                };

                var result = await _service.SearchFormulasAsync(query);
                return HandlePagedServiceResult(result, "搜索成功");
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
                if (validationResult != null) return validationResult;

                if (string.IsNullOrWhiteSpace(dto.NewName))
                {
                    return ValidationFail<FormulaDto>("新验方名称不能为空");
                }

                var result = await _service.CopyAsync(id, dto.NewName);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<FormulaDto>(result.ErrorMessage ?? "复制验方失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                LogOperation("复制验方成功", result.Data, id);
                return Success(result.Data, "复制成功");
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
                if (validationResult != null) return validationResult;

                var result = await _service.ToggleStatusAsync(id);
                if (!result.IsSuccess || !result.Data)
                {
                    return BusinessFail(result.ErrorMessage ?? "切换状态失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation("切换验方状态成功", null, id);
                return Success("状态切换成功");
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
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.ShareFormulaAsync(id, operatorId, operatorName);
                if (!result.IsSuccess || !result.Data)
                {
                    return BusinessFail(result.ErrorMessage ?? "分享验方失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation("分享验方成功", null, id);
                return Success("分享成功");
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
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.UnshareFormulaAsync(id, operatorId, operatorName);
                if (!result.IsSuccess || !result.Data)
                {
                    return BusinessFail(result.ErrorMessage ?? "取消分享失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation("取消分享验方成功", null, id);
                return Success("取消分享成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "取消分享验方", id);
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