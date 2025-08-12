using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 智能加载状态管理器 - UltraThink创新设计
    /// 
    /// 核心创新点：
    /// 1. 分层加载状态管理，避免嵌套冲突
    /// 2. 智能防抖，处理快速切换操作
    /// 3. 丰富的加载反馈（进度、提示、取消支持）
    /// 4. 自动状态清理和内存管理
    /// 5. 线程安全的状态同步
    /// </summary>
    public interface ISmartLoadingManager : INotifyPropertyChanged
    {
        /// <summary>
        /// 全局加载状态
        /// </summary>
        bool IsGlobalLoading { get; }
        
        /// <summary>
        /// 当前活跃的加载操作数量
        /// </summary>
        int ActiveLoadingCount { get; }
        
        /// <summary>
        /// 开始加载操作
        /// </summary>
        /// <param name="operationId">操作唯一标识</param>
        /// <param name="message">加载提示信息</param>
        /// <param name="layer">加载层级（0=全局，1=模块，2=组件）</param>
        /// <param name="supportProgress">是否支持进度跟踪</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>加载操作句柄</returns>
        ILoadingOperation StartLoading(string operationId, string message = "加载中...", 
            int layer = 1, bool supportProgress = false, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取指定层级的加载状态
        /// </summary>
        bool IsLoadingAtLayer(int layer);
        
        /// <summary>
        /// 获取当前加载消息
        /// </summary>
        string GetCurrentLoadingMessage(int layer = -1);
        
        /// <summary>
        /// 取消指定操作
        /// </summary>
        void CancelOperation(string operationId);
        
        /// <summary>
        /// 取消所有操作
        /// </summary>
        void CancelAllOperations();
        
        /// <summary>
        /// 清理过期的加载状态
        /// </summary>
        void CleanupExpiredStates();
    }

    /// <summary>
    /// 加载操作句柄
    /// </summary>
    public interface ILoadingOperation : IDisposable
    {
        /// <summary>
        /// 操作ID
        /// </summary>
        string OperationId { get; }
        
        /// <summary>
        /// 是否支持进度跟踪
        /// </summary>
        bool SupportsProgress { get; }
        
        /// <summary>
        /// 当前进度（0-100）
        /// </summary>
        int Progress { get; }
        
        /// <summary>
        /// 是否已取消
        /// </summary>
        bool IsCancelled { get; }
        
        /// <summary>
        /// 取消令牌
        /// </summary>
        CancellationToken CancellationToken { get; }
        
        /// <summary>
        /// 更新进度
        /// </summary>
        void UpdateProgress(int progress, string? message = null);
        
        /// <summary>
        /// 更新消息
        /// </summary>
        void UpdateMessage(string message);
        
        /// <summary>
        /// 完成操作
        /// </summary>
        void Complete();
    }

    /// <summary>
    /// 智能加载状态管理器实现
    /// </summary>
    public class SmartLoadingManager : ISmartLoadingManager, IDisposable
    {
        #region 私有字段

        private readonly ILogger<SmartLoadingManager> _logger;
        private readonly ConcurrentDictionary<string, LoadingOperationInfo> _activeOperations = new();
        private readonly Timer _cleanupTimer;
        private readonly object _stateLock = new object();
        
        // 防抖控制
        private readonly Dictionary<int, DateTime> _lastStateChangeTime = new();
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(100);
        
        // 状态缓存
        private volatile bool _cachedGlobalLoading;
        private volatile int _cachedActiveCount;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheInvalidationTime = TimeSpan.FromMilliseconds(50);

        #endregion

        #region 构造函数

        public SmartLoadingManager(ILogger<SmartLoadingManager> logger)
        {
            _logger = logger;
            
            // 定期清理过期状态（每30秒）
            _cleanupTimer = new Timer(PerformCleanup, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            
            _logger.LogDebug("智能加载管理器已初始化");
        }

        #endregion

        #region 公共属性

        public bool IsGlobalLoading
        {
            get
            {
                UpdateCacheIfNeeded();
                return _cachedGlobalLoading;
            }
        }

        public int ActiveLoadingCount
        {
            get
            {
                UpdateCacheIfNeeded();
                return _cachedActiveCount;
            }
        }

        #endregion

        #region 公共方法

        public ILoadingOperation StartLoading(string operationId, string message = "加载中...", 
            int layer = 1, bool supportProgress = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentException("操作ID不能为空", nameof(operationId));

            lock (_stateLock)
            {
                // 如果操作已存在，返回现有操作
                if (_activeOperations.TryGetValue(operationId, out var existing))
                {
                    _logger.LogWarning("操作 {OperationId} 已在进行中，返回现有操作", operationId);
                    return existing.Operation;
                }

                var operation = new LoadingOperation(operationId, message, layer, supportProgress, this, cancellationToken);
                var operationInfo = new LoadingOperationInfo
                {
                    Operation = operation,
                    StartTime = DateTime.UtcNow,
                    Layer = layer,
                    Message = message
                };

                _activeOperations[operationId] = operationInfo;
                
                // 记录状态变化时间（防抖用）
                _lastStateChangeTime[layer] = DateTime.UtcNow;
                
                _logger.LogDebug("开始加载操作: {OperationId}, 层级: {Layer}, 消息: {Message}", 
                    operationId, layer, message);

                // 延迟通知状态变化，实现防抖
                Task.Delay(_debounceDelay).ContinueWith(_ => NotifyStateChanged());

                return operation;
            }
        }

        public bool IsLoadingAtLayer(int layer)
        {
            return _activeOperations.Values.Any(op => op.Layer == layer && !op.Operation.IsCancelled);
        }

        public string GetCurrentLoadingMessage(int layer = -1)
        {
            var operations = _activeOperations.Values
                .Where(op => !op.Operation.IsCancelled && (layer == -1 || op.Layer == layer))
                .OrderBy(op => op.StartTime)
                .ToList();

            if (!operations.Any())
                return string.Empty;

            // 返回最新的加载消息
            var latest = operations.Last();
            return latest.Message;
        }

        public void CancelOperation(string operationId)
        {
            if (_activeOperations.TryGetValue(operationId, out var operationInfo))
            {
                operationInfo.Operation.Cancel();
                _logger.LogDebug("取消加载操作: {OperationId}", operationId);
            }
        }

        public void CancelAllOperations()
        {
            var operations = _activeOperations.Values.ToList();
            foreach (var operationInfo in operations)
            {
                operationInfo.Operation.Cancel();
            }
            _logger.LogDebug("取消所有加载操作，共 {Count} 个", operations.Count);
        }

        public void CleanupExpiredStates()
        {
            PerformCleanup(null);
        }

        #endregion

        #region 内部方法

        internal void CompleteOperation(string operationId)
        {
            lock (_stateLock)
            {
                if (_activeOperations.TryRemove(operationId, out var operationInfo))
                {
                    _logger.LogDebug("完成加载操作: {OperationId}, 耗时: {Duration}ms", 
                        operationId, (DateTime.UtcNow - operationInfo.StartTime).TotalMilliseconds);
                    
                    // 记录状态变化时间
                    _lastStateChangeTime[operationInfo.Layer] = DateTime.UtcNow;
                    
                    // 延迟通知状态变化
                    Task.Delay(_debounceDelay).ContinueWith(_ => NotifyStateChanged());
                }
            }
        }

        private void UpdateCacheIfNeeded()
        {
            var now = DateTime.UtcNow;
            if (now - _lastCacheUpdate < _cacheInvalidationTime)
                return;

            lock (_stateLock)
            {
                var activeOps = _activeOperations.Values
                    .Where(op => !op.Operation.IsCancelled)
                    .ToList();

                _cachedGlobalLoading = activeOps.Any();
                _cachedActiveCount = activeOps.Count;
                _lastCacheUpdate = now;
            }
        }

        private void NotifyStateChanged()
        {
            // 检查是否应该发送通知（防抖）
            var now = DateTime.UtcNow;
            var shouldNotify = _lastStateChangeTime.Values.Any(lastChange => 
                now - lastChange >= _debounceDelay);

            if (shouldNotify)
            {
                InvalidateCache();
                OnPropertyChanged(nameof(IsGlobalLoading));
                OnPropertyChanged(nameof(ActiveLoadingCount));
            }
        }

        private void InvalidateCache()
        {
            _lastCacheUpdate = DateTime.MinValue;
        }

        private void PerformCleanup(object? state)
        {
            try
            {
                var expiredThreshold = DateTime.UtcNow.AddMinutes(-5); // 5分钟过期
                var expiredOperations = _activeOperations.Values
                    .Where(op => op.StartTime < expiredThreshold || op.Operation.IsCancelled)
                    .ToList();

                foreach (var expired in expiredOperations)
                {
                    if (_activeOperations.TryRemove(expired.Operation.OperationId, out _))
                    {
                        expired.Operation.Dispose();
                        _logger.LogDebug("清理过期操作: {OperationId}", expired.Operation.OperationId);
                    }
                }

                if (expiredOperations.Any())
                {
                    NotifyStateChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期加载状态时发生错误");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            CancelAllOperations();
            _activeOperations.Clear();
            _logger.LogDebug("智能加载管理器已释放");
        }

        #endregion

        #region 私有类型

        private class LoadingOperationInfo
        {
            public LoadingOperation Operation { get; set; } = null!;
            public DateTime StartTime { get; set; }
            public int Layer { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        #endregion
    }

    /// <summary>
    /// 加载操作实现
    /// </summary>
    internal class LoadingOperation : ILoadingOperation
    {
        private readonly SmartLoadingManager _manager;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _externalCancellationToken;
        
        private volatile bool _disposed;
        private volatile int _progress;
        private volatile string _message;
        private volatile bool _completed;

        public string OperationId { get; }
        public bool SupportsProgress { get; }
        public int Layer { get; }

        public int Progress => _progress;
        public bool IsCancelled => _cancellationTokenSource.Token.IsCancellationRequested || _externalCancellationToken.IsCancellationRequested;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        internal LoadingOperation(string operationId, string message, int layer, bool supportsProgress,
            SmartLoadingManager manager, CancellationToken externalCancellationToken)
        {
            OperationId = operationId;
            _message = message;
            Layer = layer;
            SupportsProgress = supportsProgress;
            _manager = manager;
            _externalCancellationToken = externalCancellationToken;
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        }

        public void UpdateProgress(int progress, string? message = null)
        {
            if (!SupportsProgress || _disposed || _completed)
                return;

            _progress = Math.Max(0, Math.Min(100, progress));
            
            if (!string.IsNullOrEmpty(message))
            {
                _message = message;
            }
        }

        public void UpdateMessage(string message)
        {
            if (_disposed || _completed)
                return;

            _message = message ?? string.Empty;
        }

        public void Complete()
        {
            if (_disposed || _completed)
                return;

            _completed = true;
            _manager.CompleteOperation(OperationId);
        }

        internal void Cancel()
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            if (!_completed)
            {
                Complete();
            }
            
            _cancellationTokenSource.Dispose();
        }
    }
}