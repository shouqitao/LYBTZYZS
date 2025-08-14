using System.Collections.Concurrent;

namespace LYBT.Infrastructure.Performance.Async
{
    /// <summary>
    /// 统一异步处理器接口 - UltraThink性能优化
    /// </summary>
    public interface IUnifiedAsyncProcessor
    {
        /// <summary>
        /// 提交异步任务
        /// </summary>
        Task<string> SubmitTaskAsync<T>(
            Func<T, CancellationToken, Task> taskFunc, 
            T parameter, 
            AsyncTaskOptions? options = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 提交有返回值的异步任务
        /// </summary>
        Task<string> SubmitTaskAsync<T, TResult>(
            Func<T, CancellationToken, Task<TResult>> taskFunc, 
            T parameter, 
            AsyncTaskOptions? options = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量提交任务
        /// </summary>
        Task<List<string>> SubmitBatchTasksAsync<T>(
            Func<T, CancellationToken, Task> taskFunc, 
            IEnumerable<T> parameters, 
            BatchTaskOptions? options = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取任务状态
        /// </summary>
        Task<AsyncTaskStatus> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取任务结果
        /// </summary>
        Task<T?> GetTaskResultAsync<T>(string taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消任务
        /// </summary>
        Task<bool> CancelTaskAsync(string taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取处理器统计信息
        /// </summary>
        Task<AsyncProcessorStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理已完成的任务
        /// </summary>
        Task<int> CleanupCompletedTasksAsync(TimeSpan? olderThan = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 重试失败的任务
        /// </summary>
        Task<bool> RetryTaskAsync(string taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 等待所有任务完成
        /// </summary>
        Task WaitForAllTasksAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 暂停处理器
        /// </summary>
        Task PauseProcessorAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 恢复处理器
        /// </summary>
        Task ResumeProcessorAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取活动任务列表
        /// </summary>
        Task<List<AsyncTaskInfo>> GetActiveTasksAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 异步任务选项
    /// </summary>
    public class AsyncTaskOptions
    {
        /// <summary>
        /// 任务优先级
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 重试延迟时间
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 任务超时时间
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// 任务标签（用于分类和查询）
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 任务描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 失败时是否保留结果
        /// </summary>
        public bool PreserveResultOnFailure { get; set; } = false;

        /// <summary>
        /// 是否启用进度报告
        /// </summary>
        public bool EnableProgressReporting { get; set; } = false;

        /// <summary>
        /// 任务依赖（必须等待这些任务完成后才能执行）
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    /// <summary>
    /// 批量任务选项
    /// </summary>
    public class BatchTaskOptions : AsyncTaskOptions
    {
        /// <summary>
        /// 批处理大小
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// 并发度
        /// </summary>
        public int MaxConcurrency { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 是否快速失败（任何一个任务失败就停止）
        /// </summary>
        public bool FailFast { get; set; } = false;

        /// <summary>
        /// 批处理间的延迟
        /// </summary>
        public TimeSpan? BatchDelay { get; set; }

        /// <summary>
        /// 是否保持顺序执行
        /// </summary>
        public bool PreserveOrder { get; set; } = false;
    }

    /// <summary>
    /// 任务优先级
    /// </summary>
    public enum TaskPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// 异步任务状态
    /// </summary>
    public class AsyncTaskStatus
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 当前状态
        /// </summary>
        public TaskState State { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 开始执行时间
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan? Duration => StartedAt.HasValue && CompletedAt.HasValue 
            ? CompletedAt.Value - StartedAt.Value 
            : null;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// 任务标签
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 任务描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public TaskPriority Priority { get; set; }
    }

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskState
    {
        Queued,         // 排队中
        Running,        // 执行中
        Completed,      // 已完成
        Failed,         // 失败
        Cancelled,      // 已取消
        Paused,         // 已暂停
        Retrying        // 重试中
    }

    /// <summary>
    /// 异步处理器统计信息
    /// </summary>
    public class AsyncProcessorStatistics
    {
        /// <summary>
        /// 总任务数
        /// </summary>
        public long TotalTasks { get; set; }

        /// <summary>
        /// 已完成任务数
        /// </summary>
        public long CompletedTasks { get; set; }

        /// <summary>
        /// 失败任务数
        /// </summary>
        public long FailedTasks { get; set; }

        /// <summary>
        /// 正在执行的任务数
        /// </summary>
        public int RunningTasks { get; set; }

        /// <summary>
        /// 排队中的任务数
        /// </summary>
        public int QueuedTasks { get; set; }

        /// <summary>
        /// 平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks : 0;

        /// <summary>
        /// 每分钟处理任务数
        /// </summary>
        public double TasksPerMinute { get; set; }

        /// <summary>
        /// 活动线程数
        /// </summary>
        public int ActiveThreads { get; set; }

        /// <summary>
        /// 最大并发数
        /// </summary>
        public int MaxConcurrency { get; set; }

        /// <summary>
        /// CPU使用率（百分比）
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// 内存使用量（MB）
        /// </summary>
        public long MemoryUsageMB { get; set; }

        /// <summary>
        /// 处理器状态
        /// </summary>
        public ProcessorState ProcessorState { get; set; }

        /// <summary>
        /// 统计时间
        /// </summary>
        public DateTime StatisticsTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 处理器状态
    /// </summary>
    public enum ProcessorState
    {
        Running,        // 运行中
        Paused,         // 暂停
        Stopping,       // 停止中
        Stopped         // 已停止
    }

    /// <summary>
    /// 异步任务信息
    /// </summary>
    public class AsyncTaskInfo
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 任务类型
        /// </summary>
        public string TaskType { get; set; } = string.Empty;

        /// <summary>
        /// 当前状态
        /// </summary>
        public TaskState State { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public TaskPriority Priority { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 开始执行时间
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 预计完成时间
        /// </summary>
        public DateTime? EstimatedCompletionTime { get; set; }

        /// <summary>
        /// 进度
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// 当前错误信息
        /// </summary>
        public string? CurrentError { get; set; }
    }

    /// <summary>
    /// 任务进度报告接口
    /// </summary>
    public interface ITaskProgressReporter
    {
        /// <summary>
        /// 报告进度
        /// </summary>
        void ReportProgress(double percentage, string? message = null);

        /// <summary>
        /// 设置总工作量
        /// </summary>
        void SetTotalWork(long totalWork);

        /// <summary>
        /// 报告已完成工作量
        /// </summary>
        void ReportCompleted(long completedWork);

        /// <summary>
        /// 报告状态消息
        /// </summary>
        void ReportStatus(string status);
    }

    /// <summary>
    /// 任务结果接口
    /// </summary>
    public interface ITaskResult<T>
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        string TaskId { get; }

        /// <summary>
        /// 是否成功
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// 结果值
        /// </summary>
        T? Value { get; }

        /// <summary>
        /// 错误信息
        /// </summary>
        Exception? Error { get; }

        /// <summary>
        /// 执行时间
        /// </summary>
        TimeSpan ExecutionTime { get; }
    }
}