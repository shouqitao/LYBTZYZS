using Asp.Versioning;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 缓存管理 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CacheController : BaseController
    {
        private readonly ICacheService _cacheService;

        public CacheController(
            ICacheService cacheService,
            ILogger<CacheController> logger)
            : base(logger)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<CacheStatistics>> GetStatistics()
        {
            try
            {
                var stats = await _cacheService.GetStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存统计信息时发生错误");
                return StatusCode(500, new { message = "获取缓存统计失败" });
            }
        }

        /// <summary>
        /// 清除指定模式的缓存
        /// </summary>
        [HttpDelete("pattern/{pattern}")]
        public async Task<ActionResult> ClearByPattern(string pattern)
        {
            try
            {
                await _cacheService.RemoveByPatternAsync(pattern);
                _logger.LogInformation("清除缓存模式: {Pattern}", pattern);
                return Ok(new { message = $"已清除模式 '{pattern}' 的缓存" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存模式 {Pattern} 时发生错误", pattern);
                return StatusCode(500, new { message = "清除缓存失败" });
            }
        }

        /// <summary>
        /// 清除指定标签的缓存
        /// </summary>
        [HttpDelete("tag/{tag}")]
        public async Task<ActionResult> ClearByTag(string tag)
        {
            try
            {
                await _cacheService.RemoveByTagAsync(tag);
                _logger.LogInformation("清除缓存标签: {Tag}", tag);
                return Ok(new { message = $"已清除标签 '{tag}' 的缓存" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存标签 {Tag} 时发生错误", tag);
                return StatusCode(500, new { message = "清除缓存失败" });
            }
        }

        /// <summary>
        /// 清除指定键的缓存
        /// </summary>
        [HttpDelete("{key}")]
        public async Task<ActionResult> ClearKey(string key)
        {
            try
            {
                await _cacheService.RemoveAsync(key);
                _logger.LogInformation("清除缓存键: {Key}", key);
                return Ok(new { message = $"已清除缓存键 '{key}'" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存键 {Key} 时发生错误", key);
                return StatusCode(500, new { message = "清除缓存失败" });
            }
        }

        /// <summary>
        /// 缓存健康检查
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<object>> GetHealth()
        {
            try
            {
                var stats = await _cacheService.GetStatisticsAsync();
                
                var health = new
                {
                    Status = "Healthy",
                    HitRate = stats.HitRate,
                    TotalKeys = stats.TotalKeys,
                    MemoryUsageMB = stats.TotalMemoryUsed / 1024.0 / 1024.0,
                    Recommendation = GetHealthRecommendation(stats)
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存健康状态时发生错误");
                return Ok(new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        private static string GetHealthRecommendation(CacheStatistics stats)
        {
            if (stats.HitRate < 0.5)
                return "缓存命中率较低，建议检查缓存策略";
            
            if (stats.TotalMemoryUsed > 100 * 1024 * 1024) // > 100MB
                return "内存使用较高，建议优化缓存过期策略";
            
            if (stats.TotalKeys > 10000)
                return "缓存键数量较多，建议清理无效缓存";
            
            return "缓存状态良好";
        }
    }
}