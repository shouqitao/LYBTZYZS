using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;


namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 缓存预热服务 - 在应用启动时预热关键缓存数据（简化版）
    /// </summary>
    public class CacheWarmupService : ICacheWarmupService
    {
        private readonly ILogger<CacheWarmupService> _logger;

        public CacheWarmupService(ILogger<CacheWarmupService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 执行缓存预热
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预热结果</returns>
        public async Task<CacheWarmupResult> WarmupAsync(CancellationToken cancellationToken = default)
        {
            var result = new CacheWarmupResult
            {
                StartTime = DateTime.UtcNow
            };

            _logger.LogInformation("开始缓存预热...");

            try
            {
                var tasks = new List<Task<WarmupTaskResult>>
                {
                    WarmupBasicDataAsync(cancellationToken)
                };

                var taskResults = await Task.WhenAll(tasks);

                foreach (var taskResult in taskResults)
                {
                    result.TaskResults.Add(taskResult);
                    if (taskResult.IsSuccess)
                        result.SuccessCount++;
                    else
                        result.FailureCount++;
                }

                result.EndTime = DateTime.UtcNow;
                result.TotalDuration = result.EndTime - result.StartTime;
                result.IsSuccess = result.FailureCount == 0;

                _logger.LogInformation(
                    "缓存预热完成: 成功 {SuccessCount}, 失败 {FailureCount}, 耗时 {Duration}ms",
                    result.SuccessCount, result.FailureCount, result.TotalDuration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存预热过程中发生错误");
                result.EndTime = DateTime.UtcNow;
                result.TotalDuration = result.EndTime - result.StartTime;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// 预热基础数据 - 架构重构后的简化版本
        /// </summary>
        private async Task<WarmupTaskResult> WarmupBasicDataAsync(CancellationToken cancellationToken)
        {
            var taskResult = new WarmupTaskResult
            {
                TaskName = "基础数据预热",
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug("开始预热基础数据...");

                // 预热基础缓存配置和静态数据
                await Task.Delay(50, cancellationToken); // 模拟预热操作

                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = true;
                taskResult.ItemCount = 1;

                _logger.LogDebug("基础数据预热完成: 耗时 {Duration}ms", taskResult.Duration.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = "预热被取消";
                _logger.LogWarning("基础数据预热被取消");
            }
            catch (Exception ex)
            {
                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "基础数据预热失败");
            }

            return taskResult;
        }
    }

    /// <summary>
    /// 缓存预热服务接口
    /// </summary>
    public interface ICacheWarmupService
    {
        /// <summary>
        /// 执行缓存预热
        /// </summary>
        Task<CacheWarmupResult> WarmupAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 缓存预热结果
    /// </summary>
    public class CacheWarmupResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<WarmupTaskResult> TaskResults { get; set; } = new();
    }

    /// <summary>
    /// 预热任务结果
    /// </summary>
    public class WarmupTaskResult
    {
        public string TaskName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int ItemCount { get; set; }
    }
}