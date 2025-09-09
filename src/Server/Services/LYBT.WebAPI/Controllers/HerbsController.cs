using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 药材管理 API 控制器 - 简化版本：只处理名称、剂量、价格
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
        /// 获取药材分页列表 - 统一API响应格式
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<HerbDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<HerbDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var query = new HerbPagedQueryDto
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword
                };

                var result = await _herbService.GetPagedAsync(query);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<HerbDto>(ex, "获取药材列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 根据ID获取药材详情 - 统一API响应格式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<HerbDto>>> GetById(Guid id)
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
        /// 创建新药材 - 统一API响应格式
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<HerbDto>>> Create([FromBody] HerbCreateDto dto)
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
        /// 更新药材信息 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<HerbDto>>> Update(Guid id, [FromBody] HerbUpdateDto dto)
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

                if (dto.Id != id)
                {
                    return ValidationFail<HerbDto>("URL中的ID与请求体中的ID不匹配");
                }

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
        /// 删除药材（软删除） - 统一API响应格式
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "药材ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _herbService.DeleteAsync(id);
                return HandleBoolServiceResult(result, "删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除药材", id);
            }
        }

        /// <summary>
        /// 获取药材分类（基于功效分类） - 统一API响应格式
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<string>>>> GetCategories()
        {
            try
            {
                // 获取所有启用的药材 (使用分页查询替代GetAvailableHerbsAsync)
                var query = new HerbPagedQueryDto
                {
                    PageIndex = 1,
                    PageSize = 10000,
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
                };
                var allHerbsResult = await _herbService.GetPagedAsync(query);
                if (!allHerbsResult.IsSuccess || allHerbsResult.Data?.Items == null)
                {
                    return Success(new List<string>(), "暂无药材分类");
                }

                // 提取所有非空的功效字段作为分类
                var categories = allHerbsResult.Data.Items
                    .Where(h => !string.IsNullOrWhiteSpace(h.Effect))
                    .Select(h => h.Effect!.Trim())
                    .Distinct()
                    .Where(effect => !string.IsNullOrWhiteSpace(effect))
                    .OrderBy(effect => effect)
                    .ToList();

                // 如果没有分类，返回默认的中医分类
                if (!categories.Any())
                {
                    categories = new List<string>
                    {
                        "清热类", "补益类", "解表类", "理气类",
                        "活血类", "止血类", "化痰类", "消食类", "其他"
                    };
                }

                return Success(categories, $"获取分类成功，共{categories.Count}个分类");
            }
            catch (Exception ex)
            {
                return HandleException<List<string>>(ex, "获取药材分类", null);
            }
        }

        /// <summary>
        /// 搜索药材（用于处方配药） - 统一API响应格式
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<HerbDto>>>> Search([FromQuery] string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ValidationFail<List<HerbDto>>("搜索关键词不能为空");
                }

                var result = await _herbService.SearchAsync(keyword);
                return HandleServiceResult(result, $"搜索完成，找到{result.Data?.Count ?? 0}条记录");
            }
            catch (Exception ex)
            {
                return HandleException<List<HerbDto>>(ex, "搜索药材", keyword);
            }
        }
    }
}
