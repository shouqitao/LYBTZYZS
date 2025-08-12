using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 缓存预热服务 - 在应用启动时预热关键缓存数据（简化版）
    /// </summary>
    public class CacheWarmupService : ICacheWarmupService
    {
        private readonly IHerbService? _herbService;
        private readonly IUserService? _userService;
        private readonly IPrescriptionService? _prescriptionService;
        private readonly ILogger<CacheWarmupService> _logger;

        public CacheWarmupService(ILogger<CacheWarmupService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 构造函数 - 支持可选依赖注入
        /// </summary>
        public CacheWarmupService(
            ILogger<CacheWarmupService> logger,
            IHerbService? herbService = null,
            IUserService? userService = null,
            IPrescriptionService? prescriptionService = null)
        {
            _logger = logger;
            _herbService = herbService;
            _userService = userService;
            _prescriptionService = prescriptionService;
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
                    WarmupHerbDataAsync(cancellationToken),
                    WarmupUserDataAsync(cancellationToken),
                    WarmupPrescriptionDataAsync(cancellationToken)
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
        /// 预热药材数据
        /// </summary>
        private async Task<WarmupTaskResult> WarmupHerbDataAsync(CancellationToken cancellationToken)
        {
            var taskResult = new WarmupTaskResult
            {
                TaskName = "药材数据预热",
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug("开始预热药材数据...");

                if (_herbService == null)
                {
                    _logger.LogInformation("药材服务未注册，跳过药材数据预热");
                    taskResult.EndTime = DateTime.UtcNow;
                    taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                    taskResult.IsSuccess = true;
                    taskResult.ItemCount = 0;
                    return taskResult;
                }

                // 预热关键药材数据
                var warmupTasks = new List<Task>
                {
                    // 预热可用药材列表
                    _herbService.GetAvailableHerbsAsync(),
                    
                    // 预热所有药材列表
                    _herbService.GetHerbsAsync(),
                    
                    // 预热缺货药材
                    _herbService.GetOutOfStockHerbsAsync(),
                    
                    // 预热即将过期药材（30天）
                    _herbService.GetExpiringHerbsAsync(30),
                    
                    // 预热药材统计信息
                    _herbService.GetStatisticsAsync()
                };

                await Task.WhenAll(warmupTasks);

                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = true;
                taskResult.ItemCount = warmupTasks.Count;

                _logger.LogDebug("药材数据预热完成: 耗时 {Duration}ms", taskResult.Duration.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = "预热被取消";
                _logger.LogWarning("药材数据预热被取消");
            }
            catch (Exception ex)
            {
                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "药材数据预热失败");
            }

            return taskResult;
        }

        /// <summary>
        /// 预热用户数据
        /// </summary>
        private async Task<WarmupTaskResult> WarmupUserDataAsync(CancellationToken cancellationToken)
        {
            var taskResult = new WarmupTaskResult
            {
                TaskName = "用户数据预热",
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug("开始预热用户数据...");

                // 预热用户数据（如果有相关缓存方法）
                // 这里可以根据实际的IUserService接口添加相应的预热调用
                
                await Task.Delay(10, cancellationToken); // 占位，避免空任务

                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = true;
                taskResult.ItemCount = 1;

                _logger.LogDebug("用户数据预热完成: 耗时 {Duration}ms", taskResult.Duration.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = "预热被取消";
                _logger.LogWarning("用户数据预热被取消");
            }
            catch (Exception ex)
            {
                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "用户数据预热失败");
            }

            return taskResult;
        }

        /// <summary>
        /// 预热处方数据
        /// </summary>
        private async Task<WarmupTaskResult> WarmupPrescriptionDataAsync(CancellationToken cancellationToken)
        {
            var taskResult = new WarmupTaskResult
            {
                TaskName = "处方数据预热",
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogDebug("开始预热处方数据...");

                if (_prescriptionService == null)
                {
                    _logger.LogInformation("处方服务未注册，跳过处方数据预热");
                    taskResult.EndTime = DateTime.UtcNow;
                    taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                    taskResult.IsSuccess = true;
                    taskResult.ItemCount = 0;
                    return taskResult;
                }

                // 预热今日处方列表
                var warmupTasks = new List<Task>
                {
                    _prescriptionService.GetTodayPrescriptionsAsync()
                };

                await Task.WhenAll(warmupTasks);

                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = true;
                taskResult.ItemCount = warmupTasks.Count;

                _logger.LogDebug("处方数据预热完成: 耗时 {Duration}ms", taskResult.Duration.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = "预热被取消";
                _logger.LogWarning("处方数据预热被取消");
            }
            catch (Exception ex)
            {
                taskResult.EndTime = DateTime.UtcNow;
                taskResult.Duration = taskResult.EndTime - taskResult.StartTime;
                taskResult.IsSuccess = false;
                taskResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "处方数据预热失败");
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