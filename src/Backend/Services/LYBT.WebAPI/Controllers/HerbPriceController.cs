using Asp.Versioning;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材价格管理 API 控制器 - UltraThink重构：专门负责价格管理功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/herbs")]
    [Authorize]
    public class HerbPriceController : BaseController
    {
        private readonly IHerbService _herbService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// 构造方法，注入药材服务和缓存服务
        /// </summary>
        public HerbPriceController(
            IHerbService herbService, 
            ICacheService cacheService,
            ILogger<HerbPriceController> logger)
            : base(logger)
        {
            _herbService = herbService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// 更新药材价格
        /// </summary>
        [HttpPatch("{id}/price")]
        public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] HerbPriceUpdateDto dto)
        {
            try
            {
                var validationResult = ValidateGuid(id, "药材ID");
                if (validationResult != null) return validationResult;

                dto.Id = id; // 确保ID一致

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (dto.Price < 0)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "参数错误",
                        Detail = "价格不能为负数",
                        Status = 400
                    });
                }

                var result = await _herbService.UpdatePriceAsync(dto);
                if (!result)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "资源未找到",
                        Detail = "药材不存在",
                        Status = 404
                    });
                }

                // 清除相关缓存
                await ClearPriceRelatedCache(id);

                LogOperation("更新药材价格", new { HerbId = id, NewPrice = dto.Price }, id);
                return Ok(new { 
                    message = "价格更新成功",
                    newPrice = dto.Price
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
            catch (Exception ex)
            {
                return HandleException(ex, "更新药材价格", new { HerbId = id });
            }
        }

        /// <summary>
        /// 批量更新价格
        /// </summary>
        [HttpPatch("batch-price")]
        public async Task<IActionResult> BatchUpdatePrice([FromBody] List<HerbPriceUpdateDto> updates)
        {
            try
            {
                if (updates == null || !updates.Any())
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "请求无效",
                        Detail = "价格更新数据不能为空",
                        Status = 400
                    });
                }

                // 验证数据
                var validationErrors = ValidateBatchPriceUpdate(updates);
                if (validationErrors.Any())
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "数据验证失败",
                        Detail = "存在无效的价格更新数据",
                        Status = 400,
                        Extensions = { ["errors"] = validationErrors }
                    });
                }

                var count = await _herbService.BatchUpdatePriceAsync(updates);

                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("herbs");

                LogOperation($"批量更新价格成功，更新 {count} 个药材", 
                    new { RequestCount = updates.Count, UpdatedCount = count }, null);

                return Ok(new { 
                    message = $"成功更新 {count} 个药材价格", 
                    updatedCount = count,
                    requestCount = updates.Count
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
            catch (Exception ex)
            {
                return HandleException(ex, "批量更新价格");
            }
        }

        /// <summary>
        /// 设置特价促销
        /// </summary>
        [HttpPost("{id}/special-price")]
        public async Task<IActionResult> SetSpecialPrice(Guid id, [FromBody] SpecialPriceRequest request)
        {
            try
            {
                var validationResult = ValidateGuid(id, "药材ID");
                if (validationResult != null) return validationResult;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 验证特价参数
                var priceValidationResult = ValidateSpecialPriceRequest(request);
                if (priceValidationResult != null)
                {
                    return BadRequest(priceValidationResult);
                }

                var result = await _herbService.SetSpecialPriceAsync(id, request.SpecialPrice, request.StartTime, request.EndTime);
                if (!result)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "资源未找到",
                        Detail = "药材不存在",
                        Status = 404
                    });
                }

                // 清除相关缓存
                await ClearPriceRelatedCache(id);

                LogOperation("设置特价促销", 
                    new { 
                        HerbId = id, 
                        SpecialPrice = request.SpecialPrice,
                        StartTime = request.StartTime,
                        EndTime = request.EndTime
                    }, id);

                return Ok(new { 
                    message = "特价设置成功",
                    specialPrice = request.SpecialPrice,
                    startTime = request.StartTime,
                    endTime = request.EndTime
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
                return HandleException(ex, "设置特价促销", new { HerbId = id });
            }
        }

        /// <summary>
        /// 取消特价促销
        /// </summary>
        [HttpDelete("{id}/special-price")]
        public async Task<IActionResult> CancelSpecialPrice(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "药材ID");
                if (validationResult != null) return validationResult;

                var result = await _herbService.CancelSpecialPriceAsync(id);
                if (!result)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "资源未找到",
                        Detail = "药材不存在或没有设置特价",
                        Status = 404
                    });
                }

                // 清除相关缓存
                await ClearPriceRelatedCache(id);

                LogOperation("取消特价促销", null, id);
                return Ok(new { message = "特价已取消" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "取消特价促销", new { HerbId = id });
            }
        }

        /// <summary>
        /// 获取当前特价药材列表
        /// </summary>
        [HttpGet("special-price")]
        public async Task<ActionResult<List<HerbDto>>> GetSpecialPriceHerbs()
        {
            try
            {
                var cacheKey = _cacheService.GenerateListKey("herbs", "special-price");
                var list = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _herbService.GetSpecialPriceHerbsAsync();
                }, TimeSpan.FromMinutes(5));

                LogOperation("获取特价药材列表", new { Count = list?.Count ?? 0 }, null);
                return Ok(list ?? new List<HerbDto>());
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取当前特价药材列表");
            }
        }

        /// <summary>
        /// 获取价格历史记录
        /// </summary>
        [HttpGet("{id}/price-history")]
        public async Task<ActionResult<List<HerbPriceHistoryDto>>> GetPriceHistory(
            Guid id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var validationResult = ValidateGuid(id, "药材ID");
                if (validationResult != null) return validationResult;

                if (pageSize > 100)
                {
                    return BadRequest("每页最多显示100条记录");
                }

                // 设置默认时间范围（最近3个月）
                var defaultEndDate = endDate ?? DateTime.Now;
                var defaultStartDate = startDate ?? defaultEndDate.AddMonths(-3);

                var cacheKey = _cacheService.GenerateKey("herbs", "price-history", id.ToString(), 
                    $"{defaultStartDate:yyyyMMdd}-{defaultEndDate:yyyyMMdd}-{page}-{pageSize}");
                
                var history = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _herbService.GetPriceHistoryAsync(id);
                }, TimeSpan.FromMinutes(30));

                LogOperation("获取价格历史记录", 
                    new { HerbId = id, StartDate = defaultStartDate, EndDate = defaultEndDate, Page = page }, id);

                return Ok(history ?? new List<HerbPriceHistoryDto>());
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取价格历史记录", new { HerbId = id });
            }
        }

        /// <summary>
        /// 按价格区间查询药材
        /// </summary>
        [HttpGet("by-price-range")]
        public async Task<ActionResult<List<HerbDto>>> GetByPriceRange(
            [FromQuery] decimal minPrice, 
            [FromQuery] decimal maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (minPrice < 0 || maxPrice < 0)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "参数错误",
                        Detail = "价格不能为负数",
                        Status = 400
                    });
                }

                if (minPrice > maxPrice)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "参数错误",
                        Detail = "最小价格不能大于最大价格",
                        Status = 400
                    });
                }

                if (pageSize > 100)
                {
                    return BadRequest("每页最多显示100条记录");
                }

                var cacheKey = _cacheService.GenerateKey("herbs", "price-range", 
                    $"{minPrice}-{maxPrice}-{page}-{pageSize}");

                var herbs = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _herbService.GetByPriceRangeAsync(minPrice, maxPrice);
                }, TimeSpan.FromMinutes(15));

                LogOperation("按价格区间查询药材", 
                    new { MinPrice = minPrice, MaxPrice = maxPrice, Count = herbs?.Count ?? 0 }, null);

                return Ok(herbs ?? new List<HerbDto>());
            }
            catch (Exception ex)
            {
                return HandleException(ex, "按价格区间查询药材", 
                    new { MinPrice = minPrice, MaxPrice = maxPrice });
            }
        }

        /// <summary>
        /// 获取价格统计信息
        /// </summary>
        [HttpGet("price-statistics")]
        public async Task<ActionResult<PriceStatisticsDto>> GetPriceStatistics()
        {
            try
            {
                var cacheKey = _cacheService.GenerateKey("herbs", "price-statistics");
                var statistics = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await CalculatePriceStatisticsAsync();
                }, TimeSpan.FromHours(1));

                LogOperation("获取价格统计信息", statistics, null);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取价格统计信息");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证批量价格更新数据
        /// </summary>
        private List<string> ValidateBatchPriceUpdate(List<HerbPriceUpdateDto> updates)
        {
            var errors = new List<string>();

            for (int i = 0; i < updates.Count; i++)
            {
                var update = updates[i];
                if (update.Id == Guid.Empty)
                {
                    errors.Add($"第{i + 1}条记录：药材ID无效");
                }

                if (update.Price < 0)
                {
                    errors.Add($"第{i + 1}条记录：价格不能为负数");
                }
            }

            return errors;
        }

        /// <summary>
        /// 验证特价请求参数
        /// </summary>
        private ProblemDetails? ValidateSpecialPriceRequest(SpecialPriceRequest request)
        {
            if (request.SpecialPrice < 0)
            {
                return new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "特价不能为负数",
                    Status = 400
                };
            }

            if (request.StartTime >= request.EndTime)
            {
                return new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "开始时间必须小于结束时间",
                    Status = 400
                };
            }

            if (request.EndTime <= DateTime.Now)
            {
                return new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "结束时间必须在未来",
                    Status = 400
                };
            }

            return null;
        }

        /// <summary>
        /// 清除价格相关缓存
        /// </summary>
        private async Task ClearPriceRelatedCache(Guid herbId)
        {
            await _cacheService.RemoveAsync(_cacheService.GenerateKey("herbs", "detail", herbId));
            await _cacheService.RemoveByPatternAsync("herbs:special");
            await _cacheService.RemoveByPatternAsync("herbs:list");
            await _cacheService.RemoveByPatternAsync("herbs:price");
        }

        /// <summary>
        /// 计算价格统计信息
        /// </summary>
        private async Task<PriceStatisticsDto> CalculatePriceStatisticsAsync()
        {
            // 获取所有药材价格数据
            var allHerbs = await _herbService.GetListAsync();
            
            if (allHerbs == null || !allHerbs.Any())
            {
                return new PriceStatisticsDto();
            }

            var prices = allHerbs.Select(h => h.Price).Where(p => p > 0).ToList();

            return new PriceStatisticsDto
            {
                TotalCount = allHerbs.Count,
                MinPrice = prices.Any() ? prices.Min() : 0,
                MaxPrice = prices.Any() ? prices.Max() : 0,
                AveragePrice = prices.Any() ? Math.Round(prices.Average(), 2) : 0,
                SpecialPriceCount = await GetSpecialPriceCountAsync()
            };
        }

        /// <summary>
        /// 获取特价药材数量
        /// </summary>
        private async Task<int> GetSpecialPriceCountAsync()
        {
            var specialPriceHerbs = await _herbService.GetSpecialPriceHerbsAsync();
            return specialPriceHerbs?.Count ?? 0;
        }

        #endregion
    }

    #region DTO 类

    /// <summary>
    /// 特价设置请求
    /// </summary>
    public class SpecialPriceRequest
    {
        /// <summary>
        /// 特价
        /// </summary>
        public decimal SpecialPrice { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 价格统计DTO
    /// </summary>
    public class PriceStatisticsDto
    {
        /// <summary>
        /// 总药材数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 最低价格
        /// </summary>
        public decimal MinPrice { get; set; }

        /// <summary>
        /// 最高价格
        /// </summary>
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// 平均价格
        /// </summary>
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// 特价药材数量
        /// </summary>
        public int SpecialPriceCount { get; set; }
    }

    #endregion
}