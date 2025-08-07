using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 验方管理控制器
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class FormulasController : BaseController
    {
        private readonly IFormulaService _formulaService;

        public FormulasController(IFormulaService formulaService, ILogger<FormulasController> logger, IMemoryCache cache)
            : base(logger, cache)
        {
            _formulaService = formulaService;
        }

        /// <summary>
        /// 分页查询验方
        /// </summary>
        [HttpPost("paged")]
        public async Task<IActionResult> GetPagedFormulas([FromBody] FormulaQueryDto query)
        {
            try
            {
                var result = await _formulaService.GetPagedAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取验方列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFormulas()
        {
            try
            {
                var result = await _formulaService.GetListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFormulaById(Guid id)
        {
            try
            {
                var result = await _formulaService.GetByIdAsync(id);
                if (result == null)
                    return NotFound(new { message = "验方不存在" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 创建验方
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFormula([FromBody] FormulaCreateDto dto)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.CreateAsync(dto, operatorId, operatorName);
                if (result == null)
                    return BadRequest(new { message = "创建验方失败" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 更新验方
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFormula(Guid id, [FromBody] FormulaUpdateDto dto)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.UpdateAsync(id, dto, operatorId, operatorName);
                if (result == null)
                    return NotFound(new { message = "验方不存在或更新失败" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 删除验方
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFormula(Guid id)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.DeleteAsync(id, operatorId, operatorName);
                if (!result)
                    return NotFound(new { message = "验方不存在或删除失败" });

                return Ok(new { message = "删除验方成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 搜索验方
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchFormulas([FromQuery] string keyword, [FromQuery] int maxResults = 50)
        {
            try
            {
                var result = await _formulaService.SearchFormulasAsync(keyword, maxResults);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取共享验方列表
        /// </summary>
        [HttpGet("shared")]
        public async Task<IActionResult> GetSharedFormulas()
        {
            try
            {
                var result = await _formulaService.GetSharedFormulasAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取个人验方列表
        /// </summary>
        [HttpGet("personal")]
        public async Task<IActionResult> GetPersonalFormulas()
        {
            try
            {
                var (doctorId, _, _) = GetOperator();
                var result = await _formulaService.GetPersonalFormulasAsync(doctorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取常用验方
        /// </summary>
        [HttpGet("frequently-used")]
        public async Task<IActionResult> GetFrequentlyUsedFormulas([FromQuery] int limit = 20)
        {
            try
            {
                var (doctorId, _, _) = GetOperator();
                var result = await _formulaService.GetFrequentlyUsedFormulasAsync(doctorId, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 从处方创建验方
        /// </summary>
        [HttpPost("from-prescription")]
        public async Task<IActionResult> CreateFromPrescription([FromBody] CreateFormulaFromPrescriptionDto dto)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.CreateFromPrescriptionAsync(dto, operatorId, operatorName);
                if (result == null)
                    return BadRequest(new { message = "从处方创建验方失败" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 复制验方
        /// </summary>
        [HttpPost("{id}/copy")]
        public async Task<IActionResult> CopyFormula(Guid id, [FromBody] CopyFormulaRequest request)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.CopyFormulaAsync(id, request.NewName, operatorId, operatorName);
                if (result == null)
                    return BadRequest(new { message = "复制验方失败" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 分享验方
        /// </summary>
        [HttpPut("{id}/share")]
        public async Task<IActionResult> ShareFormula(Guid id)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.ShareFormulaAsync(id, operatorId, operatorName);
                if (!result)
                    return BadRequest(new { message = "分享验方失败" });

                return Ok(new { message = "分享验方成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 取消分享验方
        /// </summary>
        [HttpPut("{id}/unshare")]
        public async Task<IActionResult> UnshareFormula(Guid id)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();

                var result = await _formulaService.UnshareFormulaAsync(id, operatorId, operatorName);
                if (!result)
                    return BadRequest(new { message = "取消分享验方失败" });

                return Ok(new { message = "取消分享验方成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取验方推荐
        /// </summary>
        [HttpPost("recommendations")]
        public async Task<IActionResult> GetRecommendations([FromBody] FormulaRecommendationRequest request)
        {
            try
            {
                var (doctorId, _, _) = GetOperator();
                var result = await _formulaService.GetRecommendationsAsync(request.Symptoms, request.Diagnosis, doctorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 验证验方合理性
        /// </summary>
        [HttpPost("{id}/validate")]
        public async Task<IActionResult> ValidateFormula(Guid id)
        {
            try
            {
                var result = await _formulaService.ValidateFormulaAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取验方使用记录
        /// </summary>
        [HttpGet("{id}/usage-records")]
        public async Task<IActionResult> GetUsageRecords(Guid id)
        {
            try
            {
                var result = await _formulaService.GetUsageRecordsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 获取验方统计
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? doctorId = null)
        {
            try
            {
                var result = await _formulaService.GetStatisticsAsync(startDate, endDate, doctorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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