using Asp.Versioning;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材基础管理 API 控制器 - UltraThink重构：仅保留核心CRUD操作
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class HerbsController : BaseController
    {
        private readonly IHerbService _herbService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// 构造方法，注入药材服务和缓存服务
        /// </summary>
        public HerbsController(
            IHerbService herbService, 
            ICacheService cacheService,
            ILogger<HerbsController> logger)
            : base(logger)
        {
            _herbService = herbService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// 获取药材列表 (RESTful GET /Herbs) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<HerbDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? name = null,
            [FromQuery] string? origin = null,
            [FromQuery] string? effect = null,
            [FromQuery] string? usage = null,
            [FromQuery] CommonStatus? status = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? hasStock = null)
        {
            // 如果没有任何查询条件且请求第一页，使用缓存的完整列表
            if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(name) &&
                string.IsNullOrEmpty(origin) && string.IsNullOrEmpty(effect) && string.IsNullOrEmpty(usage) &&
                !status.HasValue && !minPrice.HasValue && !maxPrice.HasValue && !hasStock.HasValue)
            {
                var cacheKey = _cacheService.GenerateListKey("herbs");
                var list = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _herbService.GetListAsync();
                }, TimeSpan.FromMinutes(10));

                var totalCount = list?.Count ?? 0;
                var pagedList = list?.Take(pageSize).ToList() ?? new List<HerbDto>();
                var result = new PaginatedResult<HerbDto>
                {
                    TotalCount = totalCount,
                    Items = pagedList,
                    CurrentPage = page,
                    PageSize = pageSize
                };
                return Ok(result);
            }

            // 使用分页查询服务，带缓存
            var query = new HerbPagedQueryDto
            {
                CurrentPage = page,
                PageSize = pageSize,
                SearchKeyword = keyword,
                Name = name,
                Origin = origin,
                Status = status,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var filterKey = $"{keyword}_{name}_{origin}_{status}_{minPrice}_{maxPrice}_{hasStock}";
            var pagedCacheKey = _cacheService.GeneratePagedKey("herbs", page, pageSize, filterKey);
            
            var pagedResult = await _cacheService.GetOrSetAsync(pagedCacheKey, async () =>
            {
                return await _herbService.GetPagedAsync(query);
            }, TimeSpan.FromMinutes(5));
            
            return Ok(pagedResult);
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<HerbDetailDto>> GetById(Guid id)
        {
            var validationResult = ValidateGuid(id, "药材ID");
            if (validationResult != null) return validationResult;

            var cacheKey = _cacheService.GenerateKey("herbs", "detail", id);
            var detail = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                return await _herbService.GetByIdAsync(id);
            }, TimeSpan.FromMinutes(15));
            
            if (detail == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }
            return Ok(detail);
        }

        /// <summary>
        /// 创建新药材 (RESTful POST /Herbs)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateHerb([FromBody] HerbCreateDto dto)
        {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _herbService.AddAsync(dto);
            if (result != null)
            {
                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("herbs");

                LogOperation("创建药材成功", result, result.Id);
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "操作失败",
                    Detail = "药材创建失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<HerbDto>> Update(Guid id, [FromBody] HerbUpdateDto dto)
        {
            dto.Id = id; // 确保ID一致
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _herbService.UpdateAsync(dto);
            if (!result)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "操作失败",
                    Detail = "编辑药材失败",
                    Status = 400
                });
            }

            // 清除相关缓存
            await _cacheService.RemoveByPatternAsync("herbs");
            await _cacheService.RemoveAsync(_cacheService.GenerateKey("herbs", "detail", dto.Id));

            // 获取更新后的资源
            var updated = await _herbService.GetByIdAsync(dto.Id);
            LogOperation("编辑药材成功", updated, dto.Id);
            return Ok(updated);
        }

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var validationResult = ValidateGuid(id, "药材ID");
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();

            // 先获取药材当前状态
            var herb = await _herbService.GetByIdAsync(id);
            if (herb == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            // 根据当前状态切换
            var newStatus = herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
            var result = await _herbService.SetStatusAsync(id, newStatus == CommonStatus.Enabled);
            if (!result)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "操作失败",
                    Detail = "状态切换失败",
                    Status = 400
                });
            }

            // 清除相关缓存
            await _cacheService.RemoveByPatternAsync("herbs");
            await _cacheService.RemoveAsync(_cacheService.GenerateKey("herbs", "detail", id));

            var message = herb.Status == CommonStatus.Enabled ? "药材已禁用" : "药材已启用";
            LogOperation(message, new { Id = id, IsActive = newStatus }, id);
            return Ok(new { message });
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        [HttpPatch("status")]
        public async Task<ActionResult> UpdateStatus([FromBody] CommonStatusUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _herbService.SetStatusAsync(dto.Id, dto.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled);
                if (!result)
                    return NotFound("药材不存在");

                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("herbs");
                await _cacheService.RemoveAsync(_cacheService.GenerateKey("herbs", "detail", dto.Id));

                LogOperation("更新药材状态成功", dto, dto.Id);
                return Ok("药材状态更新成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新药材状态", new { HerbId = dto.Id });
            }
        }

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<List<HerbDto>>> GetAvailable()
        {
            var cacheKey = _cacheService.GenerateListKey("herbs", "available");
            var list = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                return await _herbService.GetAvailableHerbsAsync();
            }, TimeSpan.FromMinutes(15));
            
            return Ok(list ?? new List<HerbDto>());
        }

        // 注意：本系统采用软删除策略，不提供DELETE接口
        // 请使用 PATCH /Herbs/{id}/toggle-status 来切换药材状态
    }
}