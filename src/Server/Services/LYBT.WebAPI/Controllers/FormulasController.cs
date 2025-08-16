using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 验方管理控制器 - 统一API响应格式
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class FormulasController : BaseApiController
    {
        private readonly IFormulaService _formulaService;

        public FormulasController(IFormulaService formulaService, ILogger<FormulasController> logger, IMemoryCache cache)
            : base(logger, cache)
        {
            _formulaService = formulaService;
        }

        /// <summary>
        /// 分页查询验方 - 统一API响应格式
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> GetPagedFormulas([FromBody] FormulaQueryDto query)
        {
            try
            {
                var validation = ValidateModel<object>();
                if (validation != null) return validation;

                var result = await _formulaService.GetPagedAsync(query);
                return Success<object>(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "分页查询验方", query);
            }
        }

        /// <summary>
        /// 获取验方列表 - 统一API响应格式
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<FormulaDto>>>> GetFormulas()
        {
            try
            {
                var result = await _formulaService.GetListAsync();
                return Success(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "获取验方列表", null);
            }
        }

        /// <summary>
        /// 根据ID获取验方详情 - 统一API响应格式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<FormulaDetailDto>>> GetFormulaById(Guid id)
        {
            try
            {
                var validation = ValidateGuid<FormulaDetailDto>(id, "验方ID");
                if (validation != null) return validation;

                var result = await _formulaService.GetByIdAsync(id);
                if (result == null)
                    return NotFound<FormulaDetailDto>("验方不存在", ApiErrorCodes.FORMULA_NOT_FOUND);

                return Success(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDetailDto>(ex, "根据ID获取验方详情", id);
            }
        }

        /// <summary>
        /// 创建验方 - 统一API响应格式
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<FormulaDto>>> CreateFormula([FromBody] FormulaCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<FormulaDto>();
                if (validation != null) return validation;

                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.CreateAsync(dto, operatorId, operatorName);
                if (result == null)
                    return BusinessFail<FormulaDto>("创建验方失败", ApiErrorCodes.DATA_SAVE_FAILED);

                LogOperation("创建验方", result, result.Id);
                return Success<FormulaDto>(result, "验方创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDto>(ex, "创建验方", dto);
            }
        }

        /// <summary>
        /// 更新验方 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<FormulaDetailDto>>> UpdateFormula(Guid id, [FromBody] FormulaUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<FormulaDetailDto>(id, "验方ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<FormulaDetailDto>();
                if (modelValidation != null) return modelValidation;

                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.UpdateAsync(id, dto, operatorId, operatorName);
                if (result == null)
                    return NotFound<FormulaDetailDto>("验方不存在或更新失败", ApiErrorCodes.FORMULA_NOT_FOUND);

                LogOperation("更新验方", result, id);
                return Success(result, "验方更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDetailDto>(ex, "更新验方", new { id, dto });
            }
        }

        /// <summary>
        /// 删除验方 - 统一API响应格式
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> DeleteFormula(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "验方ID");
                if (validation != null) return validation;

                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.DeleteAsync(id, operatorId, operatorName);
                if (!result)
                    return NotFound("验方不存在或删除失败", ApiErrorCodes.FORMULA_NOT_FOUND);

                LogOperation("删除验方", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除验方", id);
            }
        }

        /// <summary>
        /// 搜索验方 - 统一API响应格式
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<FormulaDto>>>> SearchFormulas([FromQuery] string keyword, [FromQuery] int maxResults = 50)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ValidationFail<List<FormulaDto>>("搜索关键词不能为空");

                if (maxResults <= 0 || maxResults > 100)
                    return ValidationFail<List<FormulaDto>>("搜索结果数量必须在1-100之间");

                var result = await _formulaService.SearchFormulasAsync(keyword, maxResults);
                return Success(result, $"搜索到{result.Count}个验方");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "搜索验方", new { keyword, maxResults });
            }
        }

        /// <summary>
        /// 获取共享验方列表 - 统一API响应格式
        /// </summary>
        [HttpGet("shared")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<FormulaDto>>>> GetSharedFormulas()
        {
            try
            {
                var result = await _formulaService.GetSharedFormulasAsync();
                return Success(result, $"查询成功，共{result.Count}个共享验方");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "获取共享验方列表", null);
            }
        }

        /// <summary>
        /// 获取个人验方列表 - 统一API响应格式
        /// </summary>
        [HttpGet("personal")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<FormulaDto>>>> GetPersonalFormulas()
        {
            try
            {
                var (doctorId, _, _) = GetOperator();
                var result = await _formulaService.GetPersonalFormulasAsync(doctorId);
                return Success(result, $"查询成功，共{result.Count}个个人验方");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "获取个人验方列表", null);
            }
        }

        /// <summary>
        /// 获取常用验方 - 统一API响应格式
        /// </summary>
        [HttpGet("frequently-used")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<FormulaDto>>>> GetFrequentlyUsedFormulas([FromQuery] int limit = 20)
        {
            try
            {
                if (limit <= 0 || limit > 100)
                    return ValidationFail<List<FormulaDto>>("查询数量必须在1-100之间");

                var (doctorId, _, _) = GetOperator();
                var result = await _formulaService.GetFrequentlyUsedFormulasAsync(doctorId, limit);
                return Success(result, $"查询成功，共{result.Count}个常用验方");
            }
            catch (Exception ex)
            {
                return HandleException<List<FormulaDto>>(ex, "获取常用验方", limit);
            }
        }

        /// <summary>
        /// 从处方创建验方 - 统一API响应格式
        /// </summary>
        [HttpPost("from-prescription")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<FormulaDetailDto>>> CreateFromPrescription([FromBody] CreateFormulaFromPrescriptionDto dto)
        {
            try
            {
                var validation = ValidateModel<FormulaDetailDto>();
                if (validation != null) return validation;

                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.CreateFromPrescriptionAsync(dto, operatorId, operatorName);
                if (result == null)
                    return BusinessFail<FormulaDetailDto>("从处方创建验方失败", ApiErrorCodes.DATA_SAVE_FAILED);

                LogOperation("从处方创建验方", result, result.Id);
                return Success(result, "从处方创建验方成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDetailDto>(ex, "从处方创建验方", dto);
            }
        }

        /// <summary>
        /// 复制验方 - 统一API响应格式
        /// </summary>
        [HttpPost("{id}/copy")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<FormulaDetailDto>>> CopyFormula(Guid id, [FromBody] CopyFormulaRequest request)
        {
            try
            {
                var idValidation = ValidateGuid<FormulaDetailDto>(id, "验方ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<FormulaDetailDto>();
                if (modelValidation != null) return modelValidation;

                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.CopyFormulaAsync(id, request.NewName, operatorId, operatorName);
                if (result == null)
                    return BusinessFail<FormulaDetailDto>("复制验方失败", ApiErrorCodes.DATA_SAVE_FAILED);

                LogOperation("复制验方", result, result.Id);
                return Success(result, "复制验方成功");
            }
            catch (Exception ex)
            {
                return HandleException<FormulaDetailDto>(ex, "复制验方", new { id, request });
            }
        }

        /// <summary>
        /// 分享验方 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}/share")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> ShareFormula(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "验方ID");
                if (validation != null) return validation;

                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.ShareFormulaAsync(id, operatorId, operatorName);
                if (!result)
                    return BusinessFail("分享验方失败", ApiErrorCodes.DATA_UPDATE_FAILED);

                LogOperation("分享验方", null, id);
                return Success("分享验方成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "分享验方", id);
            }
        }

        /// <summary>
        /// 取消分享验方 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}/unshare")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> UnshareFormula(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "验方ID");
                if (validation != null) return validation;

                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.UnshareFormulaAsync(id, operatorId, operatorName);
                if (!result)
                    return BusinessFail("取消分享验方失败", ApiErrorCodes.DATA_UPDATE_FAILED);

                LogOperation("取消分享验方", null, id);
                return Success("取消分享验方成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "取消分享验方", id);
            }
        }

        /// <summary>
        /// 获取验方推荐 - 统一API响应格式
        /// </summary>
        [HttpPost("recommendations")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> GetRecommendations([FromBody] FormulaRecommendationRequest request)
        {
            try
            {
                var validation = ValidateModel<object>();
                if (validation != null) return validation;

                var (doctorId, _, _) = GetOperator();
                var result = await _formulaService.GetRecommendationsAsync(request.Symptoms, request.Diagnosis, doctorId);
                return Success<object>(result, "推荐验方获取成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取验方推荐", request);
            }
        }

        /// <summary>
        /// 验证验方合理性 - 统一API响应格式
        /// </summary>
        [HttpPost("{id}/validate")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ValidateFormula(Guid id)
        {
            try
            {
                var validation = ValidateGuid<object>(id, "验方ID");
                if (validation != null) return validation;

                var result = await _formulaService.ValidateFormulaAsync(id);
                return Success<object>(result, "验证完成");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "验证验方合理性", id);
            }
        }

        /// <summary>
        /// 获取验方使用记录 - 统一API响应格式
        /// </summary>
        [HttpGet("{id}/usage-records")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> GetUsageRecords(Guid id)
        {
            try
            {
                var validation = ValidateGuid<object>(id, "验方ID");
                if (validation != null) return validation;

                var result = await _formulaService.GetUsageRecordsAsync(id);
                return Success<object>(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取验方使用记录", id);
            }
        }

        /// <summary>
        /// 获取验方统计 - 统一API响应格式
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> GetStatistics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? doctorId = null)
        {
            try
            {
                if (startDate > endDate)
                    return ValidationFail<object>("开始日期不能晚于结束日期");

                var result = await _formulaService.GetStatisticsAsync(startDate, endDate, doctorId);
                return Success<object>(result, "统计数据查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取验方统计", new { startDate, endDate, doctorId });
            }
        }
    }

    /// <summary>
    /// 复制验方请求
    /// </summary>
    public class CopyFormulaRequest
    {
        public string NewName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验方推荐请求
    /// </summary>
    public class FormulaRecommendationRequest
    {
        public string Symptoms { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
    }
}