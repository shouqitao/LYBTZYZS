using Asp.Versioning;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材库存管理 API 控制器 - UltraThink重构：专门负责库存管理功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/herbs")]
    [Authorize]
    public class HerbStockController : BaseController
    {
        private readonly IHerbService _herbService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// 构造方法，注入药材服务和缓存服务
        /// </summary>
        public HerbStockController(
            IHerbService herbService, 
            ICacheService cacheService,
            ILogger<HerbStockController> logger)
            : base(logger)
        {
            _herbService = herbService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// 获取库存预警药材列表
        /// </summary>
        [HttpGet("stock-warning")]
        public async Task<ActionResult<List<HerbStockWarningDto>>> GetStockWarning()
        {
            try
            {
                var cacheKey = _cacheService.GenerateListKey("herbs", "stock-warning");
                var list = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _herbService.GetStockWarningListAsync();
                }, TimeSpan.FromMinutes(5)); // 库存预警缓存时间较短

                LogOperation("获取库存预警列表", new { Count = list?.Count ?? 0 }, null);
                return Ok(list ?? new List<HerbStockWarningDto>());
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取库存预警药材列表");
            }
        }

        /// <summary>
        /// 获取库存统计信息
        /// </summary>
        [HttpGet("stock-statistics")]
        public async Task<ActionResult<HerbStockStatisticsDto>> GetStockStatistics()
        {
            try
            {
                var cacheKey = _cacheService.GenerateKey("herbs", "stock-statistics");
                var statistics = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _herbService.GetStockStatisticsAsync();
                }, TimeSpan.FromMinutes(10));

                LogOperation("获取库存统计信息", statistics, null);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取库存统计信息");
            }
        }

        /// <summary>
        /// 更新药材库存（供Pharmacy模块调用）
        /// </summary>
        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(Guid id, [FromBody] StockUpdateRequest request)
        {
            try
            {
                var validationResult = ValidateGuid(id, "药材ID");
                if (validationResult != null) return validationResult;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "参数错误",
                        Detail = "库存数量必须大于0",
                        Status = 400
                    });
                }

                var result = await _herbService.UpdateStockAsync(id, request.Quantity, request.IsIncrease);
                if (!result)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "操作失败",
                        Detail = request.IsIncrease ? "入库失败，请检查药材是否存在" : "库存不足或药材不存在",
                        Status = 400
                    });
                }

                // 清除相关缓存
                await ClearStockRelatedCache(id);

                var operation = request.IsIncrease ? "入库" : "出库";
                var message = $"{operation} {request.Quantity} 成功";
                LogOperation(message, new { HerbId = id, Quantity = request.Quantity, IsIncrease = request.IsIncrease }, id);
                
                return Ok(new { 
                    message = "库存更新成功", 
                    operation = operation,
                    quantity = request.Quantity 
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "操作无效",
                    Detail = ex.Message,
                    Status = 400
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新药材库存", new { HerbId = id });
            }
        }

        /// <summary>
        /// 批量更新库存（用于盘点）
        /// </summary>
        [HttpPatch("batch-stock")]
        public async Task<IActionResult> BatchUpdateStock([FromBody] List<HerbStockUpdateDto> updates)
        {
            try
            {
                if (updates == null || !updates.Any())
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "请求无效",
                        Detail = "更新数据不能为空",
                        Status = 400
                    });
                }

                // 验证数据
                var validationErrors = ValidateBatchStockUpdate(updates);
                if (validationErrors.Any())
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "数据验证失败",
                        Detail = "存在无效的库存更新数据",
                        Status = 400,
                        Extensions = { ["errors"] = validationErrors }
                    });
                }

                var count = await _herbService.BatchUpdateStockAsync(updates);

                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("herbs");

                LogOperation($"批量更新库存成功，更新 {count} 个药材", 
                    new { RequestCount = updates.Count, UpdatedCount = count }, null);

                return Ok(new { 
                    message = $"成功更新 {count} 个药材库存", 
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
                return HandleException(ex, "批量更新库存");
            }
        }

        /// <summary>
        /// 设置库存预警值
        /// </summary>
        [HttpPatch("{id}/warning-level")]
        public async Task<IActionResult> SetWarningLevel(Guid id, [FromBody] WarningLevelRequest request)
        {
            try
            {
                var validationResult = ValidateGuid(id, "药材ID");
                if (validationResult != null) return validationResult;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.WarningLevel < 0)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "参数错误",
                        Detail = "预警值不能为负数",
                        Status = 400
                    });
                }

                if (request.MaxStock.HasValue && request.MaxStock.Value < request.WarningLevel)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "参数错误",
                        Detail = "最大库存不能小于预警值",
                        Status = 400
                    });
                }

                var result = await _herbService.SetStockWarningLevelAsync(id, (decimal)request.WarningLevel, request.MaxStock ?? 0);
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
                await ClearStockRelatedCache(id);

                LogOperation("设置库存预警值", 
                    new { HerbId = id, WarningLevel = request.WarningLevel, MaxStock = request.MaxStock }, id);

                return Ok(new { 
                    message = "预警值设置成功",
                    warningLevel = request.WarningLevel,
                    maxStock = request.MaxStock
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "设置库存预警值", new { HerbId = id });
            }
        }

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        [HttpGet("expiry-warning")]
        public async Task<ActionResult<List<HerbExpiryWarningDto>>> GetExpiryWarning([FromQuery] int days = 30)
        {
            try
            {
                if (days <= 0)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "参数错误",
                        Detail = "天数必须大于0",
                        Status = 400
                    });
                }

                var cacheKey = _cacheService.GenerateKey("herbs", "expiry-warning", days.ToString());
                var list = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _herbService.GetExpiryWarningListAsync(days);
                }, TimeSpan.FromHours(1));

                LogOperation("获取过期预警列表", new { Days = days, Count = list?.Count ?? 0 }, null);
                return Ok(list ?? new List<HerbExpiryWarningDto>());
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取即将过期的药材", new { Days = days });
            }
        }


        #region 私有方法

        /// <summary>
        /// 验证批量库存更新数据
        /// </summary>
        private List<string> ValidateBatchStockUpdate(List<HerbStockUpdateDto> updates)
        {
            var errors = new List<string>();

            for (int i = 0; i < updates.Count; i++)
            {
                var update = updates[i];
                if (update.Id == Guid.Empty)
                {
                    errors.Add($"第{i + 1}条记录：药材ID无效");
                }

                if (update.NewStock < 0)
                {
                    errors.Add($"第{i + 1}条记录：库存数量不能为负数");
                }
            }

            return errors;
        }

        /// <summary>
        /// 清除库存相关缓存
        /// </summary>
        private async Task ClearStockRelatedCache(Guid herbId)
        {
            await _cacheService.RemoveAsync(_cacheService.GenerateKey("herbs", "detail", herbId));
            await _cacheService.RemoveByPatternAsync("herbs:stock");
            await _cacheService.RemoveByPatternAsync("herbs:list");
        }

        #endregion
    }

    #region DTO 类

    /// <summary>
    /// 库存更新请求
    /// </summary>
    public class StockUpdateRequest
    {
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 是否增加（true=入库，false=出库）
        /// </summary>
        public bool IsIncrease { get; set; }

        /// <summary>
        /// 操作备注
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 预警值设置请求
    /// </summary>
    public class WarningLevelRequest
    {
        /// <summary>
        /// 预警值
        /// </summary>
        public int WarningLevel { get; set; }

        /// <summary>
        /// 最大库存（可选）
        /// </summary>
        public decimal? MaxStock { get; set; }
    }

    #endregion
}