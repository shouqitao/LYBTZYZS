using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LYBT.Infrastructure.Performance.Async
{
    /// <summary>
    /// 统一异步处理器实现 - UltraThink性能优化核心
    /// 职责单一：专注异步任务管理和调度
    /// 代码干净：清晰的任务状态管理和错误处理
    /// 性能出色：智能队列、并发控制和资源优化
    /// </summary>
    public class UnifiedAsyncProcessor : IUnifiedAsyncProcessor, IHostedService, IDisposable
    {
        private readonly ILogger<UnifiedAsyncProcessor> _logger;
        private readonly ConcurrentDictionary<string, AsyncTaskItem> _tasks = new();
        private readonly ConcurrentQueue<AsyncTaskItem> _taskQueue = new();
        private readonly SemaphoreSlim _workerSemaphore;
        private readonly Timer _cleanupTimer;
        private readonly Timer _statisticsTimer;
        
        private ProcessorState _processorState = ProcessorState.Stopped;
        private readonly CancellationTokenSource _globalCancellationSource = new();
        private readonly object _stateLock = new object();
        
        // 统计信息
        private long _totalTasks = 0;
        private long _completedTasks = 0;
        private long _failedTasks = 0;
        private readonly List<double> _executionTimes = new();
        private readonly object _statsLock = new object();

        // 配置参数
        private readonly int _maxConcurrency;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _statisticsInterval = TimeSpan.FromSeconds(30);

        public UnifiedAsyncProcessor(ILogger<UnifiedAsyncProcessor> logger, int? maxConcurrency = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxConcurrency = maxConcurrency ?? Environment.ProcessorCount * 2;
            _workerSemaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
            
            _cleanupTimer = new Timer(CleanupCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _statisticsTimer = new Timer(StatisticsCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            
            _logger.LogInformation("异步处理器初始化完成，最大并发数: {MaxConcurrency}", _maxConcurrency);
        }

        /// <summary>
        /// 提交异步任务
        /// </summary>
        public async Task<string> SubmitTaskAsync<T>(
            Func<T, CancellationToken, Task> taskFunc, 
            T parameter, 
            AsyncTaskOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(taskFunc);
            
            options ??= new AsyncTaskOptions();
            var taskId = GenerateTaskId();
            
            var taskItem = new AsyncTaskItem
            {
                TaskId = taskId,
                TaskType = typeof(T).Name,
                Priority = options.Priority,
                MaxRetries = options.MaxRetries,
                RetryDelay = options.RetryDelay,
                Timeout = options.Timeout,
                Tags = new List<string>(options.Tags),
                Description = options.Description,
                Dependencies = new List<string>(options.Dependencies),
                EnableProgressReporting = options.EnableProgressReporting,
                PreserveResultOnFailure = options.PreserveResultOnFailure,
                TaskDelegate = async (ct) => await taskFunc(parameter, ct),
                CreatedAt = DateTime.UtcNow,
                State = TaskState.Queued
            };

            _tasks.TryAdd(taskId, taskItem);
            Interlocked.Increment(ref _totalTasks);

            // 检查依赖是否满足
            if (await AreDependenciesSatisfiedAsync(taskItem.Dependencies, cancellationToken))
            {
                _taskQueue.Enqueue(taskItem);
                _ = Task.Run(() => ProcessTaskQueueAsync(_globalCancellationSource.Token), _globalCancellationSource.Token);
            }

            _logger.LogDebug("任务已提交: {TaskId}, 类型: {TaskType}, 优先级: {Priority}", 
                taskId, taskItem.TaskType, taskItem.Priority);

            return taskId;
        }

        /// <summary>
        /// 提交有返回值的异步任务
        /// </summary>
        public async Task<string> SubmitTaskAsync<T, TResult>(
            Func<T, CancellationToken, Task<TResult>> taskFunc, 
            T parameter, 
            AsyncTaskOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(taskFunc);

            return await SubmitTaskAsync<T>(async (p, ct) =>
            {
                var result = await taskFunc(p, ct);
                // 在任务项中存储结果
                var taskId = GetCurrentTaskId(); // 需要实现获取当前执行任务ID的方法
                if (_tasks.TryGetValue(taskId, out var taskItem))
                {
                    taskItem.Result = result;
                }
            }, parameter, options, cancellationToken);
        }

        /// <summary>
        /// 批量提交任务
        /// </summary>
        public async Task<List<string>> SubmitBatchTasksAsync<T>(
            Func<T, CancellationToken, Task> taskFunc, 
            IEnumerable<T> parameters, 
            BatchTaskOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(taskFunc);
            ArgumentNullException.ThrowIfNull(parameters);

            options ??= new BatchTaskOptions();
            var parametersList = parameters.ToList();
            var taskIds = new List<string>();

            if (parametersList.Count == 0)
            {
                _logger.LogWarning("批量任务提交：参数列表为空");
                return taskIds;
            }

            _logger.LogInformation("开始批量任务提交: {Count}个任务, 批大小: {BatchSize}", 
                parametersList.Count, options.BatchSize);

            // 分批处理
            for (int i = 0; i < parametersList.Count; i += options.BatchSize)
            {
                var batch = parametersList.Skip(i).Take(options.BatchSize);
                var batchTaskIds = new List<string>();

                foreach (var parameter in batch)
                {
                    var taskOptions = new AsyncTaskOptions
                    {
                        Priority = options.Priority,
                        MaxRetries = options.MaxRetries,
                        RetryDelay = options.RetryDelay,
                        Timeout = options.Timeout,
                        Tags = new List<string>(options.Tags) { "Batch", $"Batch_{i / options.BatchSize}" },
                        Description = options.Description
                    };

                    var taskId = await SubmitTaskAsync(taskFunc, parameter, taskOptions, cancellationToken);
                    batchTaskIds.Add(taskId);
                }

                taskIds.AddRange(batchTaskIds);

                // 批处理间延迟
                if (options.BatchDelay.HasValue && i + options.BatchSize < parametersList.Count)
                {
                    await Task.Delay(options.BatchDelay.Value, cancellationToken);
                }
            }

            _logger.LogInformation("批量任务提交完成: {Count}个任务已排队", taskIds.Count);
            return taskIds;
        }

        /// <summary>
        /// 获取任务状态
        /// </summary>
        public async Task<AsyncTaskStatus> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(taskId);

            if (_tasks.TryGetValue(taskId, out var taskItem))
            {
                return await Task.FromResult(new AsyncTaskStatus
                {
                    TaskId = taskItem.TaskId,
                    State = taskItem.State,
                    CreatedAt = taskItem.CreatedAt,
                    StartedAt = taskItem.StartedAt,
                    CompletedAt = taskItem.CompletedAt,
                    ErrorMessage = taskItem.ErrorMessage,
                    RetryCount = taskItem.RetryCount,
                    Progress = taskItem.Progress,
                    Tags = new List<string>(taskItem.Tags),
                    Description = taskItem.Description,
                    Priority = taskItem.Priority
                });
            }

            throw new ArgumentException($"任务不存在: {taskId}");
        }

        /// <summary>
        /// 获取任务结果
        /// </summary>
        public async Task<T?> GetTaskResultAsync<T>(string taskId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(taskId);

            if (_tasks.TryGetValue(taskId, out var taskItem))
            {
                // 等待任务完成
                while (taskItem.State == TaskState.Queued || taskItem.State == TaskState.Running)
                {
                    await Task.Delay(100, cancellationToken);
                }

                if (taskItem.State == TaskState.Completed && taskItem.Result is T result)
                {
                    return result;
                }

                if (taskItem.State == TaskState.Failed)
                {
                    throw new InvalidOperationException($"任务失败: {taskItem.ErrorMessage}");
                }
            }

            return default(T);
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public async Task<bool> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(taskId);

            if (_tasks.TryGetValue(taskId, out var taskItem))
            {
                if (taskItem.State == TaskState.Queued || taskItem.State == TaskState.Running)
                {
                    taskItem.CancellationTokenSource.Cancel();
                    taskItem.State = TaskState.Cancelled;
                    taskItem.CompletedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("任务已取消: {TaskId}", taskId);
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// 获取处理器统计信息
        /// </summary>
        public Task<AsyncProcessorStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            lock (_statsLock)
            {
                var runningTasks = _tasks.Values.Count(t => t.State == TaskState.Running);
                var queuedTasks = _tasks.Values.Count(t => t.State == TaskState.Queued);
                
                var avgExecutionTime = _executionTimes.Count > 0 ? _executionTimes.Average() : 0;

                var stats = new AsyncProcessorStatistics
                {
                    TotalTasks = Interlocked.Read(ref _totalTasks),
                    CompletedTasks = Interlocked.Read(ref _completedTasks),
                    FailedTasks = Interlocked.Read(ref _failedTasks),
                    RunningTasks = runningTasks,
                    QueuedTasks = queuedTasks,
                    AverageExecutionTimeMs = avgExecutionTime,
                    TasksPerMinute = CalculateTasksPerMinute(),
                    ActiveThreads = _maxConcurrency - _workerSemaphore.CurrentCount,
                    MaxConcurrency = _maxConcurrency,
                    CpuUsagePercent = GetCpuUsage(),
                    MemoryUsageMB = GetMemoryUsage(),
                    ProcessorState = _processorState
                };

                return Task.FromResult(stats);
            }
        }

        /// <summary>
        /// 清理已完成的任务
        /// </summary>
        public async Task<int> CleanupCompletedTasksAsync(TimeSpan? olderThan = null, CancellationToken cancellationToken = default)
        {
            var cleanupTime = DateTime.UtcNow - (olderThan ?? TimeSpan.FromHours(1));
            var cleanedCount = 0;

            var tasksToRemove = _tasks.Values
                .Where(t => (t.State == TaskState.Completed || t.State == TaskState.Failed || t.State == TaskState.Cancelled)
                           && t.CompletedAt.HasValue && t.CompletedAt.Value < cleanupTime)
                .ToList();

            foreach (var task in tasksToRemove)
            {
                if (_tasks.TryRemove(task.TaskId, out _))
                {
                    cleanedCount++;
                    task.Dispose();
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation("清理已完成任务: {Count}个任务被清理", cleanedCount);
            }

            return await Task.FromResult(cleanedCount);
        }

        /// <summary>
        /// 重试失败的任务
        /// </summary>
        public async Task<bool> RetryTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(taskId);

            if (_tasks.TryGetValue(taskId, out var taskItem) && taskItem.State == TaskState.Failed)
            {
                if (taskItem.RetryCount < taskItem.MaxRetries)
                {
                    taskItem.State = TaskState.Queued;
                    taskItem.RetryCount++;
                    taskItem.ErrorMessage = null;
                    taskItem.StartedAt = null;
                    taskItem.CompletedAt = null;
                    
                    // 创建新的取消令牌
                    taskItem.CancellationTokenSource = new CancellationTokenSource();
                    
                    _taskQueue.Enqueue(taskItem);
                    
                    _logger.LogInformation("任务已重新排队: {TaskId}, 重试次数: {RetryCount}", taskId, taskItem.RetryCount);
                    return await Task.FromResult(true);
                }
                else
                {
                    _logger.LogWarning("任务重试次数已达上限: {TaskId}", taskId);
                }
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// 等待所有任务完成
        /// </summary>
        public async Task WaitForAllTasksAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var waitTimeout = timeout ?? TimeSpan.FromMinutes(30);
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("等待所有任务完成，超时时间: {Timeout}", waitTimeout);

            while (DateTime.UtcNow - startTime < waitTimeout)
            {
                var activeTasks = _tasks.Values.Count(t => t.State == TaskState.Queued || t.State == TaskState.Running);
                if (activeTasks == 0)
                {
                    _logger.LogInformation("所有任务已完成");
                    return;
                }

                await Task.Delay(1000, cancellationToken);
            }

            _logger.LogWarning("等待任务完成超时");
            throw new TimeoutException("等待任务完成超时");
        }

        /// <summary>
        /// 暂停处理器
        /// </summary>
        public async Task PauseProcessorAsync(CancellationToken cancellationToken = default)
        {
            lock (_stateLock)
            {
                if (_processorState == ProcessorState.Running)
                {
                    _processorState = ProcessorState.Paused;
                    _logger.LogInformation("异步处理器已暂停");
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 恢复处理器
        /// </summary>
        public async Task ResumeProcessorAsync(CancellationToken cancellationToken = default)
        {
            lock (_stateLock)
            {
                if (_processorState == ProcessorState.Paused)
                {
                    _processorState = ProcessorState.Running;
                    _logger.LogInformation("异步处理器已恢复");
                    
                    // 重新启动任务队列处理
                    _ = Task.Run(() => ProcessTaskQueueAsync(_globalCancellationSource.Token), _globalCancellationSource.Token);
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取活动任务列表
        /// </summary>
        public async Task<List<AsyncTaskInfo>> GetActiveTasksAsync(CancellationToken cancellationToken = default)
        {
            var activeTasks = _tasks.Values
                .Where(t => t.State == TaskState.Queued || t.State == TaskState.Running || t.State == TaskState.Retrying)
                .Select(t => new AsyncTaskInfo
                {
                    TaskId = t.TaskId,
                    TaskType = t.TaskType,
                    State = t.State,
                    Priority = t.Priority,
                    CreatedAt = t.CreatedAt,
                    StartedAt = t.StartedAt,
                    Progress = t.Progress,
                    Description = t.Description,
                    Tags = new List<string>(t.Tags),
                    RetryCount = t.RetryCount,
                    MaxRetries = t.MaxRetries,
                    CurrentError = t.ErrorMessage
                })
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToList();

            return await Task.FromResult(activeTasks);
        }

        #region IHostedService 实现

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("启动异步处理器服务");
            
            _processorState = ProcessorState.Running;
            
            // 启动清理定时器
            _cleanupTimer.Change(_cleanupInterval, _cleanupInterval);
            
            // 启动统计定时器
            _statisticsTimer.Change(_statisticsInterval, _statisticsInterval);
            
            // 启动任务队列处理
            _ = Task.Run(() => ProcessTaskQueueAsync(_globalCancellationSource.Token), _globalCancellationSource.Token);
            
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("停止异步处理器服务");
            
            _processorState = ProcessorState.Stopping;
            
            // 停止定时器
            await _cleanupTimer.DisposeAsync();
            await _statisticsTimer.DisposeAsync();
            
            // 安全取消所有任务
            try
            {
                if (!_globalCancellationSource.IsCancellationRequested)
                {
                    _globalCancellationSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource已经被释放，忽略异常
            }
            
            // 等待所有任务完成或取消
            try
            {
                await WaitForAllTasksAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("停止时等待任务完成超时");
            }
            
            _processorState = ProcessorState.Stopped;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 处理任务队列
        /// </summary>
        private async Task ProcessTaskQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _processorState == ProcessorState.Running)
            {
                try
                {
                    if (_taskQueue.TryDequeue(out var taskItem))
                    {
                        // 获取工作线程
                        await _workerSemaphore.WaitAsync(cancellationToken);
                        
                        // 在后台线程中执行任务
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ExecuteTaskAsync(taskItem, cancellationToken);
                            }
                            finally
                            {
                                _workerSemaphore.Release();
                            }
                        }, cancellationToken);
                    }
                    else
                    {
                        // 队列为空，等待一段时间
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "任务队列处理异常");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 执行单个任务
        /// </summary>
        private async Task ExecuteTaskAsync(AsyncTaskItem taskItem, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("开始执行任务: {TaskId}", taskItem.TaskId);
                
                taskItem.State = TaskState.Running;
                taskItem.StartedAt = DateTime.UtcNow;
                
                // 设置超时
                using var timeoutCts = taskItem.Timeout.HasValue 
                    ? new CancellationTokenSource(taskItem.Timeout.Value) 
                    : new CancellationTokenSource();
                
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, 
                    taskItem.CancellationTokenSource.Token, 
                    timeoutCts.Token);

                // 执行任务
                await taskItem.TaskDelegate(combinedCts.Token);
                
                // 任务成功完成
                taskItem.State = TaskState.Completed;
                taskItem.CompletedAt = DateTime.UtcNow;
                taskItem.Progress = 100.0;
                
                Interlocked.Increment(ref _completedTasks);
                
                _logger.LogDebug("任务执行完成: {TaskId}, 耗时: {ElapsedMs}ms", 
                    taskItem.TaskId, stopwatch.ElapsedMilliseconds);
                
                // 记录执行时间统计
                lock (_statsLock)
                {
                    _executionTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                    if (_executionTimes.Count > 1000) // 限制统计数据量
                    {
                        _executionTimes.RemoveAt(0);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                taskItem.State = TaskState.Cancelled;
                taskItem.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("任务被取消: {TaskId}", taskItem.TaskId);
            }
            catch (Exception ex)
            {
                taskItem.State = TaskState.Failed;
                taskItem.CompletedAt = DateTime.UtcNow;
                taskItem.ErrorMessage = ex.Message;
                
                Interlocked.Increment(ref _failedTasks);
                
                _logger.LogError(ex, "任务执行失败: {TaskId}", taskItem.TaskId);
                
                // 检查是否需要重试
                if (taskItem.RetryCount < taskItem.MaxRetries)
                {
                    _logger.LogInformation("任务将在 {Delay} 后重试: {TaskId}", taskItem.RetryDelay, taskItem.TaskId);
                    
                    // 延迟后重新排队
                    _ = Task.Delay(taskItem.RetryDelay, cancellationToken).ContinueWith(async _ =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await RetryTaskAsync(taskItem.TaskId, cancellationToken);
                        }
                    }, cancellationToken);
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// 生成任务ID
        /// </summary>
        private string GenerateTaskId()
        {
            return $"task_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// 检查依赖是否满足
        /// </summary>
        private Task<bool> AreDependenciesSatisfiedAsync(List<string> dependencies, CancellationToken cancellationToken)
        {
            if (dependencies.Count == 0)
                return Task.FromResult(true);

            foreach (var dependency in dependencies)
            {
                if (_tasks.TryGetValue(dependency, out var depTask))
                {
                    if (depTask.State != TaskState.Completed)
                        return Task.FromResult(false);
                }
                else
                {
                    return Task.FromResult(false); // 依赖任务不存在
                }
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// 获取当前任务ID（简化实现）
        /// </summary>
        private string GetCurrentTaskId()
        {
            // 这里需要实现获取当前执行上下文中的任务ID
            // 实际实现可能需要使用AsyncLocal或其他机制
            return string.Empty;
        }

        /// <summary>
        /// 计算每分钟处理任务数
        /// </summary>
        private double CalculateTasksPerMinute()
        {
            // 这里应该实现基于时间窗口的统计
            // 简化实现返回0
            return 0;
        }

        /// <summary>
        /// 获取CPU使用率
        /// </summary>
        private double GetCpuUsage()
        {
            // 这里应该实现CPU使用率监控
            // 简化实现返回0
            return 0;
        }

        /// <summary>
        /// 获取内存使用量
        /// </summary>
        private long GetMemoryUsage()
        {
            return GC.GetTotalMemory(false) / 1024 / 1024; // MB
        }

        /// <summary>
        /// 清理定时器回调
        /// </summary>
        private async void CleanupCallback(object? state)
        {
            try
            {
                await CleanupCompletedTasksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时清理任务异常");
            }
        }

        /// <summary>
        /// 统计定时器回调
        /// </summary>
        private async void StatisticsCallback(object? state)
        {
            try
            {
                var stats = await GetStatisticsAsync();
                _logger.LogDebug("处理器统计: 总任务={Total}, 完成={Completed}, 失败={Failed}, 运行中={Running}, 排队={Queued}", 
                    stats.TotalTasks, stats.CompletedTasks, stats.FailedTasks, stats.RunningTasks, stats.QueuedTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "统计信息收集异常");
            }
        }

        #endregion

        #region IDisposable 实现

        public void Dispose()
        {
            try
            {
                // 防止重复调用Cancel导致ObjectDisposedException
                if (_globalCancellationSource != null && !_globalCancellationSource.IsCancellationRequested)
                {
                    _globalCancellationSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource已经被释放，忽略异常
            }
            
            _globalCancellationSource?.Dispose();
            _workerSemaphore?.Dispose();
            _cleanupTimer?.Dispose();
            _statisticsTimer?.Dispose();
            
            // 清理所有任务
            foreach (var task in _tasks.Values)
            {
                task.Dispose();
            }
            _tasks.Clear();
            
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// 异步任务项
    /// </summary>
    internal class AsyncTaskItem : IDisposable
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public TaskState State { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public TimeSpan? Timeout { get; set; }
        public double Progress { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public string? Description { get; set; }
        public bool EnableProgressReporting { get; set; }
        public bool PreserveResultOnFailure { get; set; }
        public object? Result { get; set; }
        
        public Func<CancellationToken, Task> TaskDelegate { get; set; } = null!;
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        public void Dispose()
        {
            CancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}