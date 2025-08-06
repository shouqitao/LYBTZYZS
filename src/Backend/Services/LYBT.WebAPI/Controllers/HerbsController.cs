using Asp.Versioning;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 药材管理 API 控制器 - 统一前后端接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class HerbsController : BaseController {
        private readonly IHerbService _herbService;

        /// <summary>
        /// 构造方法，注入药材服务
        /// </summary>
        public HerbsController(IHerbService herbService, IMemoryCache cache, ILogger<HerbsController> logger) 
            : base(logger, cache) {
            _herbService = herbService;
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
            [FromQuery] HerbStatus? status = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? hasStock = null) {
            // 如果没有任何查询条件且请求第一页，使用缓存的完整列表
            if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(name) && 
                string.IsNullOrEmpty(origin) && string.IsNullOrEmpty(effect) && string.IsNullOrEmpty(usage) &&
                !status.HasValue && !minPrice.HasValue && !maxPrice.HasValue && !hasStock.HasValue) {
                
                const string cacheKey = "herbs_list";
                if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                    list = await _herbService.GetListAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                
                var totalCount = list?.Count ?? 0;
                var pagedList = list?.Take(pageSize).ToList() ?? new List<HerbDto>();
                var result = new PaginatedResult<HerbDto> {
                    TotalCount = totalCount,
                    Items = pagedList,
                    CurrentPage = page,
                    PageSize = pageSize
                };
                return Ok(result);
            }

            // 使用分页查询服务
            var query = new HerbPagedQueryDto {
                CurrentPage = page,
                PageSize = pageSize,
                SearchKeyword = keyword,
                Name = name,
                Origin = origin,
                Status = status,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };
            
            var pagedResult = await _herbService.GetPagedAsync(query);
            return Ok(pagedResult);
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PaginatedResult<HerbDto>>> GetPaged([FromBody] HerbPagedQueryDto query) {
            if (query.CurrentPage <= 0)
                query.CurrentPage = 1;
            if (query.PageSize <= 0 || query.PageSize > 100)
                query.PageSize = 20;

            // 直接使用共享DTO调用服务
            var result = await _herbService.GetPagedAsync(query);
            return Ok(result);
        }

        /// <summary>
        /// 获取活跃药材列表（RESTful接口）
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<HerbDto>>> GetActive() {
            // 缓存活跃药材列表
            const string cacheKey = "active_herbs";
            if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                list = await _herbService.GetAvailableHerbsAsync();
                _cache.Set(cacheKey, list, TimeSpan.FromMinutes(15));
            }
            return Ok(list ?? new List<HerbDto>());
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<HerbDetailDto>> GetById(Guid id) {
            var validationResult = ValidateGuid(id, "药材ID");
            if (validationResult != null) return validationResult;

            var detail = await _herbService.GetByIdAsync(id);
            if (detail == null) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }
            return Ok(detail);
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] HerbCreateDto dto) {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _herbService.AddAsync(dto);
            if (result == null) {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "新增药材失败",
                    Status = 400
                });
            }

            // 清除缓存
            _cache.Remove("herbs_list");
            _cache.Remove("active_herbs");

            LogOperation("新增药材成功", result, result.Id);
            return Ok(result);
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<HerbDto>> Update(Guid id, [FromBody] HerbUpdateDto dto) {
            dto.Id = id; // 确保ID一致
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _herbService.UpdateAsync(dto);
            if (!result) {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "编辑药材失败",
                    Status = 400
                });
            }

            // 清除缓存
            _cache.Remove("herbs_list");
            _cache.Remove("active_herbs");
            _cache.Remove($"herb_detail_{dto.Id}");

            // 获取更新后的资源
            var updated = await _herbService.GetByIdAsync(dto.Id);
            LogOperation("编辑药材成功", updated, dto.Id);
            return Ok(updated);
        }

        /// <summary>
        /// 启用药材
        /// </summary>
        [HttpPatch("{id}/enable")]
        public async Task<IActionResult> Enable(Guid id) {
            var validationResult = ValidateGuid(id, "药材ID");
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _herbService.SetStatusAsync(id, true);
            if (!result) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            // 清除缓存
            _cache.Remove("herbs_list");
            _cache.Remove("active_herbs");
            _cache.Remove($"herb_detail_{id}");

            LogOperation("启用药材成功", new { Id = id, IsEnabled = true }, id);
            return Ok(new { message = "启用药材成功" });
        }

        /// <summary>
        /// 禁用药材（软删除）
        /// </summary>
        [HttpPatch("{id}/disable")]
        public async Task<IActionResult> Disable(Guid id) {
            var validationResult = ValidateGuid(id, "药材ID");
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _herbService.SetStatusAsync(id, false);
            if (!result) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            // 清除缓存
            _cache.Remove("herbs_list");
            _cache.Remove("active_herbs");
            _cache.Remove($"herb_detail_{id}");

            LogOperation("禁用药材成功", new { Id = id, IsEnabled = false }, id);
            return Ok(new { message = "禁用药材成功" });
        }

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id) {
            var validationResult = ValidateGuid(id, "药材ID");
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            
            // 先获取药材当前状态
            var herb = await _herbService.GetByIdAsync(id);
            if (herb == null) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            // 根据当前状态切换
            var newStatus = !herb.IsActive;
            var result = await _herbService.SetStatusAsync(id, newStatus);
            if (!result) {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "状态切换失败",
                    Status = 400
                });
            }

            // 清除缓存
            _cache.Remove("herbs_list");
            _cache.Remove("active_herbs");
            _cache.Remove($"herb_detail_{id}");

            var message = herb.IsActive ? "药材已禁用" : "药材已启用";
            LogOperation(message, new { Id = id, IsActive = newStatus }, id);
            return Ok(new { message });
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult> Import([FromBody] List<HerbImportDto> dtos) {
            try {
                if (dtos == null || dtos.Count == 0) {
                    return BadRequest("导入数据不能为空");
                }

                var count = await _herbService.ImportAsync(dtos);

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");

                LogOperation("批量导入药材成功", new { Count = count }, null);
                return Ok(new { Imported = count, Message = $"成功导入 {count} 个药材" });
            } catch (Exception ex) {
                return HandleException(ex, "批量导入药材");
            }
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<List<HerbDetailDto>>> Export() {
            try {
                var data = await _herbService.ExportAsync();
                return Ok(data);
            } catch (Exception ex) {
                return HandleException(ex, "导出药材数据");
            }
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        [HttpPatch("status")]
        public async Task<ActionResult> UpdateStatus([FromBody] HerbStatusUpdateDto dto) {
            try {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _herbService.SetStatusAsync(dto.Id, dto.Status == LYBT.Shared.Models.Enums.HerbStatus.Active);
                if (!result)
                    return NotFound("药材不存在");

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");
                _cache.Remove($"herb_detail_{dto.Id}");

                LogOperation("更新药材状态成功", dto, dto.Id);
                return Ok("药材状态更新成功");
            } catch (Exception ex) {
                return HandleException(ex, "更新药材状态", new { HerbId = dto.Id });
            }
        }

        // 批量更新功能已移除，请使用单个药材的启用/禁用接口

        // 根据状态获取药材功能已简化，请使用 GetAvailable 接口

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<List<HerbDto>>> GetAvailable() {
            // 缓存可用药材列表
            const string cacheKey = "active_herbs";
            if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                list = await _herbService.GetAvailableHerbsAsync();
                _cache.Set(cacheKey, list, TimeSpan.FromMinutes(15));
            }
            return Ok(list ?? new List<HerbDto>());
        }

        // ======================== 库存管理接口 ========================

        /// <summary>
        /// 获取库存预警药材列表
        /// </summary>
        [HttpGet("stock-warning")]
        public async Task<ActionResult<List<HerbStockWarningDto>>> GetStockWarning() {
            var list = await _herbService.GetStockWarningListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取库存统计信息
        /// </summary>
        [HttpGet("stock-statistics")]
        public async Task<ActionResult<HerbStockStatisticsDto>> GetStockStatistics() {
            var statistics = await _herbService.GetStockStatisticsAsync();
            return Ok(statistics);
        }

        /// <summary>
        /// 更新药材库存（供Pharmacy模块调用）
        /// </summary>
        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(Guid id, [FromBody] StockUpdateRequest request) {
            var result = await _herbService.UpdateStockAsync(id, request.Quantity, request.IsIncrease);
            if (!result) {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = request.IsIncrease ? "入库失败" : "库存不足",
                    Status = 400
                });
            }

            // 清除缓存
            _cache.Remove($"herb_detail_{id}");
            
            var message = request.IsIncrease ? $"入库 {request.Quantity}" : $"出库 {request.Quantity}";
            LogOperation(message, request, id);
            return Ok(new { message = "库存更新成功" });
        }

        /// <summary>
        /// 批量更新库存（用于盘点）
        /// </summary>
        [HttpPatch("batch-stock")]
        public async Task<IActionResult> BatchUpdateStock([FromBody] List<HerbStockUpdateDto> updates) {
            var count = await _herbService.BatchUpdateStockAsync(updates);
            
            // 清除缓存
            _cache.Remove("herbs_list");
            _cache.Remove("active_herbs");
            
            LogOperation($"批量更新库存成功，更新 {count} 个药材", updates, null);
            return Ok(new { message = $"成功更新 {count} 个药材库存", count });
        }

        /// <summary>
        /// 设置库存预警值
        /// </summary>
        [HttpPatch("{id}/warning-level")]
        public async Task<IActionResult> SetWarningLevel(Guid id, [FromBody] WarningLevelRequest request) {
            var result = await _herbService.SetStockWarningLevelAsync(id, request.WarningLevel, request.MaxStock);
            if (!result) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            LogOperation("设置库存预警值", request, id);
            return Ok(new { message = "预警值设置成功" });
        }

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        [HttpGet("expiry-warning")]
        public async Task<ActionResult<List<HerbExpiryWarningDto>>> GetExpiryWarning([FromQuery] int days = 30) {
            var list = await _herbService.GetExpiryWarningListAsync(days);
            return Ok(list);
        }

        // ======================== 价格管理接口 ========================

        /// <summary>
        /// 更新药材价格
        /// </summary>
        [HttpPatch("{id}/price")]
        public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] HerbPriceUpdateDto dto) {
            dto.Id = id;
            var result = await _herbService.UpdatePriceAsync(dto);
            if (!result) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            // 清除缓存
            _cache.Remove($"herb_detail_{id}");
            _cache.Remove("herbs_list");
            
            LogOperation("更新药材价格", dto, id);
            return Ok(new { message = "价格更新成功" });
        }

        /// <summary>
        /// 批量更新价格
        /// </summary>
        [HttpPatch("batch-price")]
        public async Task<IActionResult> BatchUpdatePrice([FromBody] List<HerbPriceUpdateDto> updates) {
            var count = await _herbService.BatchUpdatePriceAsync(updates);
            
            // 清除缓存
            _cache.Remove("herbs_list");
            _cache.Remove("active_herbs");
            
            LogOperation($"批量更新价格成功，更新 {count} 个药材", updates, null);
            return Ok(new { message = $"成功更新 {count} 个药材价格", count });
        }

        /// <summary>
        /// 设置特价促销
        /// </summary>
        [HttpPost("{id}/special-price")]
        public async Task<IActionResult> SetSpecialPrice(Guid id, [FromBody] SpecialPriceRequest request) {
            var result = await _herbService.SetSpecialPriceAsync(id, request.SpecialPrice, request.StartTime, request.EndTime);
            if (!result) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            // 清除缓存
            _cache.Remove($"herb_detail_{id}");
            _cache.Remove("special_price_herbs");
            
            LogOperation("设置特价促销", request, id);
            return Ok(new { message = "特价设置成功" });
        }

        /// <summary>
        /// 取消特价促销
        /// </summary>
        [HttpDelete("{id}/special-price")]
        public async Task<IActionResult> CancelSpecialPrice(Guid id) {
            var result = await _herbService.CancelSpecialPriceAsync(id);
            if (!result) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "药材不存在",
                    Status = 404
                });
            }

            // 清除缓存
            _cache.Remove($"herb_detail_{id}");
            _cache.Remove("special_price_herbs");
            
            LogOperation("取消特价促销", null, id);
            return Ok(new { message = "特价已取消" });
        }

        /// <summary>
        /// 获取当前特价药材列表
        /// </summary>
        [HttpGet("special-price")]
        public async Task<ActionResult<List<HerbDto>>> GetSpecialPriceHerbs() {
            const string cacheKey = "special_price_herbs";
            if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                list = await _herbService.GetSpecialPriceHerbsAsync();
                _cache.Set(cacheKey, list, TimeSpan.FromMinutes(5));
            }
            return Ok(list ?? new List<HerbDto>());
        }

        /// <summary>
        /// 获取价格历史记录
        /// </summary>
        [HttpGet("{id}/price-history")]
        public async Task<ActionResult<List<HerbPriceHistoryDto>>> GetPriceHistory(Guid id) {
            var history = await _herbService.GetPriceHistoryAsync(id);
            return Ok(history);
        }

        /// <summary>
        /// 按价格区间查询药材
        /// </summary>
        [HttpGet("by-price-range")]
        public async Task<ActionResult<List<HerbDto>>> GetByPriceRange([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice) {
            var herbs = await _herbService.GetByPriceRangeAsync(minPrice, maxPrice);
            return Ok(herbs);
        }

        // ======================== RESTful 标准接口 ========================

        /// <summary>
        /// 创建新药材 (RESTful POST /Herbs)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateHerb([FromBody] HerbCreateDto dto) {
            var validationResult = ValidateModel();
            if (validationResult != null) return validationResult;

            var (operatorId, operatorName, _) = GetOperator();
            var result = await _herbService.AddAsync(dto);
            if (result != null) {
                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");

                LogOperation("创建药材成功", result, result.Id);
                return Ok(result);
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "药材创建失败",
                    Status = 400
                });
            }
        }

        // 注意：本系统采用软删除策略，不提供DELETE接口
        // 请使用 PATCH /Herbs/{id}/disable 来禁用药材
        // 请使用 PATCH /Herbs/{id}/enable 来启用药材
    }
}