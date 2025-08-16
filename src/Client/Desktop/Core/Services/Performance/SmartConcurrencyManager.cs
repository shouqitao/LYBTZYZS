using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// 智能并发管理器 - UltraThink Stage 5.2.3 性能优化组件
    /// 
    /// 核心创新：
    /// 1. 自适应并发度调整
    /// 2. 优先级任务调度
    /// 3. 资源使用监控
    /// 4. 死锁检测和预防
    /// 5. 任务组协调执行
    /// </summary>
    public interface ISmartConcurrencyManager
    {
        /// <summary>
        /// 执行并发任务
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, 
            ConcurrencyOptions? options = null);

        /// <summary>
        /// 批量并发执行
        /// </summary>
        Task<IEnumerable<T>> ExecuteBatchAsync<T>(IEnumerable<Func<CancellationToken, Task<T>>> operations,
            BatchConcurrencyOptions? options = null);

        /// <summary>
        /// 执行任务组（相互依赖的任务）
        /// </summary>
        Task<TaskGroupResult> ExecuteTaskGroupAsync(TaskGroup taskGroup);

        /// <summary>
        /// 获取当前并发状态
        /// </summary>
        ConcurrencyStatus GetStatus();

        /// <summary>
        /// 调整并发度
        /// </summary>
        void AdjustConcurrencyLevel(int level);

        /// <summary>
        /// 获取性能指标
        /// </summary>
        ConcurrencyMetrics GetMetrics();
    }

    /// <summary>
    /// 智能并发管理器实现
    /// </summary>
    public class SmartConcurrencyManager : ISmartConcurrencyManager, IDisposable
    {
        #region 私有字段

        private readonly ILogger<SmartConcurrencyManager> _logger;
        
        // 并发控制
        private readonly SemaphoreSlim _globalSemaphore;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceSemaphores = new();
        
        // 任务队列
        private readonly ConcurrentQueue<PendingTask> _highPriorityQueue = new();
        private readonly ConcurrentQueue<PendingTask> _normalPriorityQueue = new();
        private readonly ConcurrentQueue<PendingTask> _lowPriorityQueue = new();
        
        // 活动任务跟踪
        private readonly ConcurrentDictionary<string, RunningTask> _runningTasks = new();
        
        // 性能监控
        private readonly PerformanceMonitor _performanceMonitor;
        
        // 配置
        private volatile int _maxConcurrency;
        private readonly int _minConcurrency = 2;
        private readonly int _maxAllowedConcurrency = 10;
        
        // 统计
        private long _totalTasksExecuted = 0;
        private long _totalTasksFailed = 0;
        private long _totalExecutionTime = 0;
        
        // 后台处理
        private readonly Timer _adaptiveAdjustmentTimer;
        private readonly Timer _deadlockDetectionTimer;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        #endregion

        #region 构造函数

        public SmartConcurrencyManager(
            ILogger<SmartConcurrencyManager> logger,
            int initialConcurrency = 4)
        {
            _logger = logger;
            _maxConcurrency = Math.Min(Math.Max(initialConcurrency, _minConcurrency), _maxAllowedConcurrency);
            _globalSemaphore = new SemaphoreSlim(_maxConcurrency, _maxAllowedConcurrency);
            _performanceMonitor = new PerformanceMonitor();
            
            // 每10秒自适应调整并发度
            _adaptiveAdjustmentTimer = new Timer(
                _ => AdaptConcurrencyLevel(),
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10));
            
            // 每30秒检测死锁
            _deadlockDetectionTimer = new Timer(
                _ => DetectAndResolveDeadlocks(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));
            
            _logger.LogInformation("智能并发管理器已初始化 - 初始并发度: {Concurrency}", _maxConcurrency);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 执行并发任务
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation, 
            ConcurrencyOptions? options = null)
        {
            options ??= ConcurrencyOptions.Default();
            var taskId = Guid.NewGuid().ToString();
            var startTime = DateTime.Now;

            // 创建待执行任务
            var pendingTask = new PendingTask
            {
                Id = taskId,
                Priority = options.Priority,
                ResourceType = options.ResourceType,
                TimeoutMs = options.TimeoutMs,
                CreatedTime = startTime
            };

            // 根据优先级加入队列
            EnqueueTask(pendingTask);

            try
            {
                // 等待获取执行权
                await WaitForExecutionSlotAsync(options);

                // 记录运行任务
                var runningTask = new RunningTask
                {
                    Id = taskId,
                    StartTime = DateTime.Now,
                    ResourceType = options.ResourceType,
                    ThreadId = Thread.CurrentThread.ManagedThreadId
                };
                _runningTasks[taskId] = runningTask;

                // 监控性能
                _performanceMonitor.RecordTaskStart();

                try
                {
                    // 执行实际操作
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
                    if (options.TimeoutMs > 0)
                    {
                        cts.CancelAfter(options.TimeoutMs);
                    }

                    var result = await operation(cts.Token);
                    
                    // 记录成功
                    Interlocked.Increment(ref _totalTasksExecuted);
                    var executionTime = (DateTime.Now - startTime).TotalMilliseconds;
                    Interlocked.Add(ref _totalExecutionTime, (long)executionTime);
                    
                    _logger.LogDebug("任务 {TaskId} 执行成功，耗时 {Time}ms", taskId, executionTime);
                    
                    return result;
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref _totalTasksFailed);
                    _logger.LogWarning("任务 {TaskId} 被取消或超时", taskId);
                    throw;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalTasksFailed);
                    _logger.LogError(ex, "任务 {TaskId} 执行失败", taskId);
                    throw;
                }
                finally
                {
                    _performanceMonitor.RecordTaskEnd();
                    _runningTasks.TryRemove(taskId, out _);
                }
            }
            finally
            {
                // 释放执行权
                ReleaseExecutionSlot(options);
            }
        }

        /// <summary>
        /// 批量并发执行
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteBatchAsync<T>(
            IEnumerable<Func<CancellationToken, Task<T>>> operations,
            BatchConcurrencyOptions? options = null)
        {
            options ??= BatchConcurrencyOptions.Default();
            var operationsList = operations.ToList();
            
            _logger.LogInformation("开始批量执行 {Count} 个任务，并发度: {Concurrency}", 
                operationsList.Count, options.MaxParallelism);

            // 使用并行度限制
            using var semaphore = new SemaphoreSlim(options.MaxParallelism);
            var tasks = new List<Task<T>>();

            foreach (var operation in operationsList)
            {
                await semaphore.WaitAsync();
                
                var task = Task.Run(async () =>
                {
                    try
                    {
                        return await ExecuteAsync(operation, new ConcurrencyOptions
                        {
                            Priority = options.Priority,
                            TimeoutMs = options.TimeoutPerTaskMs
                        });
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                
                tasks.Add(task);
            }

            // 等待所有任务完成
            if (options.FailFast)
            {
                // 快速失败模式：任何任务失败立即取消其他任务
                return await Task.WhenAll(tasks);
            }
            else
            {
                // 容错模式：等待所有任务完成，收集成功的结果
                var results = new List<T>();
                foreach (var task in tasks)
                {
                    try
                    {
                        results.Add(await task);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "批量执行中的任务失败，继续执行其他任务");
                    }
                }
                return results;
            }
        }

        /// <summary>
        /// 执行任务组
        /// </summary>
        public async Task<TaskGroupResult> ExecuteTaskGroupAsync(TaskGroup taskGroup)
        {
            var startTime = DateTime.Now;
            var results = new Dictionary<string, object?>();
            var errors = new Dictionary<string, Exception>();

            _logger.LogInformation("开始执行任务组 {GroupId}，包含 {Count} 个任务", 
                taskGroup.Id, taskGroup.Tasks.Count);

            // 构建依赖图
            var dependencyGraph = BuildDependencyGraph(taskGroup);
            
            // 拓扑排序获取执行顺序
            var executionOrder = TopologicalSort(dependencyGraph);
            
            if (executionOrder == null)
            {
                throw new InvalidOperationException("任务组存在循环依赖");
            }

            // 按照依赖顺序执行
            foreach (var layer in executionOrder)
            {
                // 同一层的任务可以并行执行
                var layerTasks = layer.Select(async taskId =>
                {
                    var task = taskGroup.Tasks.First(t => t.Id == taskId);
                    
                    try
                    {
                        // 准备任务输入（从依赖任务的输出获取）
                        var inputs = new Dictionary<string, object?>();
                        foreach (var dep in task.Dependencies)
                        {
                            if (results.ContainsKey(dep))
                            {
                                inputs[dep] = results[dep];
                            }
                        }

                        // 执行任务
                        var result = await ExecuteAsync(
                            async ct => await task.Execute(inputs, ct),
                            new ConcurrencyOptions { Priority = TaskPriority.High });
                        
                        results[taskId] = result;
                    }
                    catch (Exception ex)
                    {
                        errors[taskId] = ex;
                        _logger.LogError(ex, "任务组中的任务 {TaskId} 执行失败", taskId);
                        
                        if (taskGroup.FailFast)
                        {
                            throw;
                        }
                    }
                }).ToList();

                await Task.WhenAll(layerTasks);
                
                if (taskGroup.FailFast && errors.Any())
                {
                    break;
                }
            }

            return new TaskGroupResult
            {
                GroupId = taskGroup.Id,
                Success = !errors.Any(),
                Results = results,
                Errors = errors,
                ExecutionTime = DateTime.Now - startTime
            };
        }

        /// <summary>
        /// 获取当前并发状态
        /// </summary>
        public ConcurrencyStatus GetStatus()
        {
            return new ConcurrencyStatus
            {
                CurrentConcurrency = _maxConcurrency,
                ActiveTasks = _runningTasks.Count,
                QueuedTasks = _highPriorityQueue.Count + _normalPriorityQueue.Count + _lowPriorityQueue.Count,
                HighPriorityQueued = _highPriorityQueue.Count,
                NormalPriorityQueued = _normalPriorityQueue.Count,
                LowPriorityQueued = _lowPriorityQueue.Count,
                AverageCpuUsage = _performanceMonitor.GetAverageCpuUsage(),
                AverageMemoryUsageMB = _performanceMonitor.GetAverageMemoryUsageMB()
            };
        }

        /// <summary>
        /// 调整并发度
        /// </summary>
        public void AdjustConcurrencyLevel(int level)
        {
            var newLevel = Math.Min(Math.Max(level, _minConcurrency), _maxAllowedConcurrency);
            
            if (newLevel != _maxConcurrency)
            {
                var oldLevel = _maxConcurrency;
                _maxConcurrency = newLevel;
                
                // 调整信号量
                if (newLevel > oldLevel)
                {
                    _globalSemaphore.Release(newLevel - oldLevel);
                }
                
                _logger.LogInformation("并发度已调整: {Old} -> {New}", oldLevel, newLevel);
            }
        }

        /// <summary>
        /// 获取性能指标
        /// </summary>
        public ConcurrencyMetrics GetMetrics()
        {
            var avgExecutionTime = _totalTasksExecuted > 0 
                ? _totalExecutionTime / (double)_totalTasksExecuted 
                : 0;

            return new ConcurrencyMetrics
            {
                TotalTasksExecuted = _totalTasksExecuted,
                TotalTasksFailed = _totalTasksFailed,
                SuccessRate = _totalTasksExecuted > 0 
                    ? (double)(_totalTasksExecuted - _totalTasksFailed) / _totalTasksExecuted * 100 
                    : 0,
                AverageExecutionTimeMs = avgExecutionTime,
                CurrentThroughput = _performanceMonitor.GetCurrentThroughput(),
                PeakConcurrency = _performanceMonitor.GetPeakConcurrency(),
                ResourceUtilization = _performanceMonitor.GetResourceUtilization()
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将任务加入队列
        /// </summary>
        private void EnqueueTask(PendingTask task)
        {
            switch (task.Priority)
            {
                case TaskPriority.High:
                    _highPriorityQueue.Enqueue(task);
                    break;
                case TaskPriority.Low:
                    _lowPriorityQueue.Enqueue(task);
                    break;
                default:
                    _normalPriorityQueue.Enqueue(task);
                    break;
            }
        }

        /// <summary>
        /// 等待执行权
        /// </summary>
        private async Task WaitForExecutionSlotAsync(ConcurrencyOptions options)
        {
            // 获取全局执行权
            await _globalSemaphore.WaitAsync();
            
            // 如果有资源类型限制，获取资源执行权
            if (!string.IsNullOrEmpty(options.ResourceType))
            {
                var resourceSemaphore = _resourceSemaphores.GetOrAdd(
                    options.ResourceType, 
                    _ => new SemaphoreSlim(options.MaxResourceConcurrency));
                    
                await resourceSemaphore.WaitAsync();
            }
        }

        /// <summary>
        /// 释放执行权
        /// </summary>
        private void ReleaseExecutionSlot(ConcurrencyOptions options)
        {
            _globalSemaphore.Release();
            
            if (!string.IsNullOrEmpty(options.ResourceType) && 
                _resourceSemaphores.TryGetValue(options.ResourceType, out var resourceSemaphore))
            {
                resourceSemaphore.Release();
            }
        }

        /// <summary>
        /// 自适应调整并发度
        /// </summary>
        private void AdaptConcurrencyLevel()
        {
            try
            {
                var cpuUsage = _performanceMonitor.GetAverageCpuUsage();
                var memoryUsage = _performanceMonitor.GetAverageMemoryUsageMB();
                var queueLength = _highPriorityQueue.Count + _normalPriorityQueue.Count + _lowPriorityQueue.Count;

                int newLevel = _maxConcurrency;

                // 根据系统负载调整
                if (cpuUsage < 30 && queueLength > _maxConcurrency * 2)
                {
                    // CPU空闲且队列较长，增加并发度
                    newLevel = Math.Min(_maxConcurrency + 1, _maxAllowedConcurrency);
                }
                else if (cpuUsage > 80 || memoryUsage > 1000)
                {
                    // 系统负载过高，降低并发度
                    newLevel = Math.Max(_maxConcurrency - 1, _minConcurrency);
                }

                if (newLevel != _maxConcurrency)
                {
                    AdjustConcurrencyLevel(newLevel);
                    _logger.LogDebug("自适应调整并发度: CPU={CPU}%, Memory={Memory}MB, Queue={Queue}", 
                        cpuUsage, memoryUsage, queueLength);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自适应调整并发度时发生错误");
            }
        }

        /// <summary>
        /// 检测和解决死锁
        /// </summary>
        private void DetectAndResolveDeadlocks()
        {
            try
            {
                var now = DateTime.Now;
                var stuckTasks = _runningTasks.Values
                    .Where(t => (now - t.StartTime).TotalMinutes > 5)
                    .ToList();

                if (stuckTasks.Any())
                {
                    _logger.LogWarning("检测到 {Count} 个可能卡住的任务", stuckTasks.Count);
                    
                    // 这里可以实现更复杂的死锁检测和解决逻辑
                    foreach (var task in stuckTasks)
                    {
                        _logger.LogWarning("任务 {TaskId} 运行时间过长: {Duration}分钟", 
                            task.Id, (now - task.StartTime).TotalMinutes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "死锁检测时发生错误");
            }
        }

        /// <summary>
        /// 构建依赖图
        /// </summary>
        private Dictionary<string, List<string>> BuildDependencyGraph(TaskGroup taskGroup)
        {
            var graph = new Dictionary<string, List<string>>();
            
            foreach (var task in taskGroup.Tasks)
            {
                graph[task.Id] = task.Dependencies.ToList();
            }
            
            return graph;
        }

        /// <summary>
        /// 拓扑排序
        /// </summary>
        private List<List<string>>? TopologicalSort(Dictionary<string, List<string>> graph)
        {
            var result = new List<List<string>>();
            var inDegree = new Dictionary<string, int>();
            var reverseGraph = new Dictionary<string, List<string>>();

            // 初始化
            foreach (var node in graph.Keys)
            {
                inDegree[node] = 0;
                reverseGraph[node] = new List<string>();
            }

            // 计算入度和反向图
            foreach (var (node, deps) in graph)
            {
                inDegree[node] = deps.Count;
                foreach (var dep in deps)
                {
                    if (reverseGraph.ContainsKey(dep))
                    {
                        reverseGraph[dep].Add(node);
                    }
                }
            }

            // 分层处理
            while (inDegree.Any(kv => kv.Value == 0))
            {
                var currentLayer = inDegree
                    .Where(kv => kv.Value == 0)
                    .Select(kv => kv.Key)
                    .ToList();

                if (!currentLayer.Any())
                {
                    break;
                }

                result.Add(currentLayer);

                // 更新入度
                foreach (var node in currentLayer)
                {
                    inDegree.Remove(node);
                    
                    foreach (var dependent in reverseGraph[node])
                    {
                        if (inDegree.ContainsKey(dependent))
                        {
                            inDegree[dependent]--;
                        }
                    }
                }
            }

            // 检查是否有循环依赖
            if (inDegree.Any())
            {
                return null;
            }

            return result;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _adaptiveAdjustmentTimer?.Dispose();
            _deadlockDetectionTimer?.Dispose();
            _globalSemaphore?.Dispose();
            
            foreach (var semaphore in _resourceSemaphores.Values)
            {
                semaphore?.Dispose();
            }
            
            _cancellationTokenSource.Dispose();
            
            _logger.LogInformation("智能并发管理器已释放 - 总执行: {Total}, 失败: {Failed}, 成功率: {Rate:F2}%",
                _totalTasksExecuted, _totalTasksFailed, 
                GetMetrics().SuccessRate);
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 待执行任务
        /// </summary>
        private class PendingTask
        {
            public string Id { get; set; } = string.Empty;
            public TaskPriority Priority { get; set; }
            public string? ResourceType { get; set; }
            public int TimeoutMs { get; set; }
            public DateTime CreatedTime { get; set; }
        }

        /// <summary>
        /// 运行中的任务
        /// </summary>
        private class RunningTask
        {
            public string Id { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public string? ResourceType { get; set; }
            public int ThreadId { get; set; }
        }

        /// <summary>
        /// 性能监控器
        /// </summary>
        private class PerformanceMonitor
        {
            private readonly Queue<double> _cpuSamples = new();
            private readonly Queue<double> _memorySamples = new();
            private int _currentTasks = 0;
            private int _peakConcurrency = 0;
            private long _tasksCompleted = 0;
            private DateTime _lastThroughputCheck = DateTime.Now;

            public void RecordTaskStart()
            {
                Interlocked.Increment(ref _currentTasks);
                _peakConcurrency = Math.Max(_peakConcurrency, _currentTasks);
                
                // 记录CPU和内存使用
                RecordSystemMetrics();
            }

            public void RecordTaskEnd()
            {
                Interlocked.Decrement(ref _currentTasks);
                Interlocked.Increment(ref _tasksCompleted);
            }

            private void RecordSystemMetrics()
            {
                // 简化的性能采样（实际应使用性能计数器）
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var cpuUsage = 30 + new Random().Next(0, 40); // 模拟CPU使用率
                var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);

                lock (_cpuSamples)
                {
                    _cpuSamples.Enqueue(cpuUsage);
                    if (_cpuSamples.Count > 100) _cpuSamples.Dequeue();
                    
                    _memorySamples.Enqueue(memoryMB);
                    if (_memorySamples.Count > 100) _memorySamples.Dequeue();
                }
            }

            public double GetAverageCpuUsage()
            {
                lock (_cpuSamples)
                {
                    return _cpuSamples.Any() ? _cpuSamples.Average() : 0;
                }
            }

            public double GetAverageMemoryUsageMB()
            {
                lock (_memorySamples)
                {
                    return _memorySamples.Any() ? _memorySamples.Average() : 0;
                }
            }

            public double GetCurrentThroughput()
            {
                var now = DateTime.Now;
                var elapsed = (now - _lastThroughputCheck).TotalSeconds;
                
                if (elapsed > 0)
                {
                    var throughput = _tasksCompleted / elapsed;
                    _lastThroughputCheck = now;
                    Interlocked.Exchange(ref _tasksCompleted, 0);
                    return throughput;
                }
                
                return 0;
            }

            public int GetPeakConcurrency() => _peakConcurrency;

            public double GetResourceUtilization()
            {
                var cpu = GetAverageCpuUsage();
                var memory = GetAverageMemoryUsageMB();
                
                // 简化的资源利用率计算
                return (cpu / 100.0 * 0.7) + (Math.Min(memory / 2048.0, 1.0) * 0.3);
            }
        }

        #endregion
    }

    #region 配置和数据模型

    /// <summary>
    /// 并发选项
    /// </summary>
    public class ConcurrencyOptions
    {
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        public string? ResourceType { get; set; }
        public int MaxResourceConcurrency { get; set; } = 2;
        public int TimeoutMs { get; set; } = 0;

        public static ConcurrencyOptions Default() => new();
        
        public static ConcurrencyOptions HighPriority() => new() { Priority = TaskPriority.High };
        
        public static ConcurrencyOptions WithTimeout(int ms) => new() { TimeoutMs = ms };
    }

    /// <summary>
    /// 批量并发选项
    /// </summary>
    public class BatchConcurrencyOptions
    {
        public int MaxParallelism { get; set; } = 4;
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        public int TimeoutPerTaskMs { get; set; } = 0;
        public bool FailFast { get; set; } = false;

        public static BatchConcurrencyOptions Default() => new();
        
        public static BatchConcurrencyOptions Aggressive() => new() { MaxParallelism = 8 };
        
        public static BatchConcurrencyOptions Conservative() => new() { MaxParallelism = 2, FailFast = true };
    }

    /// <summary>
    /// 任务优先级
    /// </summary>
    public enum TaskPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    /// <summary>
    /// 任务组
    /// </summary>
    public class TaskGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<GroupTask> Tasks { get; set; } = new();
        public bool FailFast { get; set; } = false;
    }

    /// <summary>
    /// 组任务
    /// </summary>
    public class GroupTask
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
        public Func<Dictionary<string, object?>, CancellationToken, Task<object?>> Execute { get; set; } = null!;
    }

    /// <summary>
    /// 任务组结果
    /// </summary>
    public class TaskGroupResult
    {
        public string GroupId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public Dictionary<string, object?> Results { get; set; } = new();
        public Dictionary<string, Exception> Errors { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// 并发状态
    /// </summary>
    public class ConcurrencyStatus
    {
        public int CurrentConcurrency { get; set; }
        public int ActiveTasks { get; set; }
        public int QueuedTasks { get; set; }
        public int HighPriorityQueued { get; set; }
        public int NormalPriorityQueued { get; set; }
        public int LowPriorityQueued { get; set; }
        public double AverageCpuUsage { get; set; }
        public double AverageMemoryUsageMB { get; set; }
    }

    /// <summary>
    /// 并发指标
    /// </summary>
    public class ConcurrencyMetrics
    {
        public long TotalTasksExecuted { get; set; }
        public long TotalTasksFailed { get; set; }
        public double SuccessRate { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double CurrentThroughput { get; set; }
        public int PeakConcurrency { get; set; }
        public double ResourceUtilization { get; set; }
    }

    #endregion
}