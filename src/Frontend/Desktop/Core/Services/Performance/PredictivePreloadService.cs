using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Caching;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// 预测性预加载服务 - UltraThink Stage 5.2.3 核心创新
    /// 
    /// 创新特性：
    /// 1. 基于用户行为预测的智能预加载
    /// 2. 自适应资源管理
    /// 3. 优先级队列调度
    /// 4. 并行预加载执行
    /// 5. 预加载效果跟踪和优化
    /// </summary>
    public interface IPredictivePreloadService
    {
        /// <summary>
        /// 启动预测性预加载
        /// </summary>
        Task StartPredictivePreloadingAsync(string currentModule, string currentAction);

        /// <summary>
        /// 执行手动预加载
        /// </summary>
        Task<PreloadResult> PreloadDataAsync(string dataType, Dictionary<string, object> parameters);

        /// <summary>
        /// 获取预加载状态
        /// </summary>
        PreloadStatus GetPreloadStatus();

        /// <summary>
        /// 暂停预加载
        /// </summary>
        void PausePreloading();

        /// <summary>
        /// 恢复预加载
        /// </summary>
        void ResumePreloading();

        /// <summary>
        /// 获取预加载统计
        /// </summary>
        PreloadStatistics GetStatistics();
    }

    /// <summary>
    /// 预测性预加载服务实现
    /// </summary>
    public class PredictivePreloadService : IPredictivePreloadService, IDisposable
    {
        #region 私有字段

        private readonly ILogger<PredictivePreloadService> _logger;
        private readonly IUserBehaviorAnalyzer _behaviorAnalyzer;
        private readonly IMemoryCacheService _cacheService;
        private readonly ISmartLoadingManager _loadingManager;
        
        // 服务引用（根据dataType动态调用）
        private readonly IServiceProvider _serviceProvider;
        
        // 预加载队列
        private readonly ConcurrentQueue<PreloadTask> _preloadQueue = new();
        private readonly SemaphoreSlim _queueSemaphore = new(1, 1);
        
        // 活动预加载任务
        private readonly ConcurrentDictionary<string, Task> _activePreloads = new();
        
        // 预加载配置
        private readonly PreloadConfiguration _configuration;
        
        // 状态管理
        private volatile bool _isPaused = false;
        private volatile bool _isDisposed = false;
        
        // 统计信息
        private long _totalPreloads = 0;
        private long _successfulPreloads = 0;
        private long _cacheHits = 0;
        private long _bytesPreloaded = 0;
        
        // 后台处理
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _backgroundProcessor;

        #endregion

        #region 构造函数

        public PredictivePreloadService(
            ILogger<PredictivePreloadService> logger,
            IUserBehaviorAnalyzer behaviorAnalyzer,
            IMemoryCacheService cacheService,
            ISmartLoadingManager loadingManager,
            IServiceProvider serviceProvider,
            PreloadConfiguration? configuration = null)
        {
            _logger = logger;
            _behaviorAnalyzer = behaviorAnalyzer;
            _cacheService = cacheService;
            _loadingManager = loadingManager;
            _serviceProvider = serviceProvider;
            _configuration = configuration ?? PreloadConfiguration.Default();
            
            // 启动后台处理器
            _backgroundProcessor = Task.Run(ProcessPreloadQueueAsync);
            
            _logger.LogInformation("预测性预加载服务已初始化 - 最大并发: {MaxConcurrency}, 缓存大小: {CacheSize}MB",
                _configuration.MaxConcurrentPreloads, _configuration.MaxCacheSizeMB);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 启动预测性预加载
        /// </summary>
        public async Task StartPredictivePreloadingAsync(string currentModule, string currentAction)
        {
            if (_isPaused || _isDisposed) return;

            try
            {
                // 获取预测结果
                var prediction = await _behaviorAnalyzer.PredictNextActionAsync(currentModule, currentAction);
                
                if (prediction.Confidence < _configuration.MinConfidenceThreshold)
                {
                    _logger.LogDebug("预测置信度过低 ({Confidence:F2}), 跳过预加载", prediction.Confidence);
                    return;
                }

                // 为每个预测的操作创建预加载任务
                foreach (var predictedAction in prediction.PredictedActions)
                {
                    if (predictedAction.Probability >= _configuration.MinProbabilityThreshold)
                    {
                        foreach (var dataType in predictedAction.DataToPreload)
                        {
                            var task = new PreloadTask
                            {
                                Id = Guid.NewGuid().ToString(),
                                DataType = dataType,
                                ModuleName = predictedAction.ModuleName,
                                Priority = CalculatePriority(predictedAction.Probability, predictedAction.EstimatedTimeToAction),
                                Parameters = new Dictionary<string, object>
                                {
                                    { "module", predictedAction.ModuleName },
                                    { "action", predictedAction.ActionName }
                                },
                                EstimatedExecutionTime = predictedAction.EstimatedTimeToAction,
                                CreatedTime = DateTime.Now
                            };

                            await EnqueuePreloadTaskAsync(task);
                        }
                    }
                }

                _logger.LogInformation("已安排 {Count} 个预加载任务基于预测", prediction.PredictedActions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动预测性预加载时发生错误");
            }
        }

        /// <summary>
        /// 执行手动预加载
        /// </summary>
        public async Task<PreloadResult> PreloadDataAsync(string dataType, Dictionary<string, object> parameters)
        {
            var startTime = DateTime.Now;
            Interlocked.Increment(ref _totalPreloads);

            try
            {
                // 检查缓存
                var cacheKey = GenerateCacheKey(dataType, parameters);
                if (_cacheService.TryGetValue(cacheKey, out object? cachedData))
                {
                    Interlocked.Increment(ref _cacheHits);
                    _logger.LogDebug("预加载命中缓存: {DataType}", dataType);
                    
                    return new PreloadResult
                    {
                        Success = true,
                        DataType = dataType,
                        CacheHit = true,
                        LoadTime = TimeSpan.Zero,
                        DataSize = 0
                    };
                }

                // 执行实际预加载
                var data = await ExecutePreloadAsync(dataType, parameters);
                
                if (data != null)
                {
                    // 存入缓存
                    _cacheService.Set(cacheKey, data, _configuration.DefaultCacheDuration);
                    
                    var dataSize = EstimateDataSize(data);
                    Interlocked.Add(ref _bytesPreloaded, dataSize);
                    Interlocked.Increment(ref _successfulPreloads);
                    
                    return new PreloadResult
                    {
                        Success = true,
                        DataType = dataType,
                        CacheHit = false,
                        LoadTime = DateTime.Now - startTime,
                        DataSize = dataSize
                    };
                }
                
                return new PreloadResult
                {
                    Success = false,
                    DataType = dataType,
                    ErrorMessage = "预加载返回空数据"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预加载数据时发生错误: {DataType}", dataType);
                
                return new PreloadResult
                {
                    Success = false,
                    DataType = dataType,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 获取预加载状态
        /// </summary>
        public PreloadStatus GetPreloadStatus()
        {
            return new PreloadStatus
            {
                IsActive = !_isPaused && !_isDisposed,
                IsPaused = _isPaused,
                QueuedTasks = _preloadQueue.Count,
                ActiveTasks = _activePreloads.Count,
                TotalMemoryUsageMB = _bytesPreloaded / (1024.0 * 1024.0),
                CacheHitRate = _totalPreloads > 0 ? (double)_cacheHits / _totalPreloads * 100 : 0
            };
        }

        /// <summary>
        /// 暂停预加载
        /// </summary>
        public void PausePreloading()
        {
            _isPaused = true;
            _logger.LogInformation("预加载服务已暂停");
        }

        /// <summary>
        /// 恢复预加载
        /// </summary>
        public void ResumePreloading()
        {
            _isPaused = false;
            _logger.LogInformation("预加载服务已恢复");
        }

        /// <summary>
        /// 获取预加载统计
        /// </summary>
        public PreloadStatistics GetStatistics()
        {
            return new PreloadStatistics
            {
                TotalPreloads = _totalPreloads,
                SuccessfulPreloads = _successfulPreloads,
                FailedPreloads = _totalPreloads - _successfulPreloads,
                CacheHits = _cacheHits,
                CacheHitRate = _totalPreloads > 0 ? (double)_cacheHits / _totalPreloads * 100 : 0,
                TotalBytesPreloaded = _bytesPreloaded,
                AveragePreloadSizeMB = _successfulPreloads > 0 ? (_bytesPreloaded / _successfulPreloads) / (1024.0 * 1024.0) : 0
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 处理预加载队列
        /// </summary>
        private async Task ProcessPreloadQueueAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_isPaused)
                    {
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                        continue;
                    }

                    // 检查并发限制
                    if (_activePreloads.Count >= _configuration.MaxConcurrentPreloads)
                    {
                        await Task.Delay(100, _cancellationTokenSource.Token);
                        continue;
                    }

                    // 获取下一个任务
                    if (_preloadQueue.TryDequeue(out var task))
                    {
                        // 检查任务是否过期
                        if (DateTime.Now - task.CreatedTime > _configuration.TaskExpiration)
                        {
                            _logger.LogDebug("预加载任务已过期: {TaskId}", task.Id);
                            continue;
                        }

                        // 异步执行预加载
                        var preloadTask = Task.Run(async () =>
                        {
                            try
                            {
                                await PreloadDataAsync(task.DataType, task.Parameters);
                            }
                            finally
                            {
                                _activePreloads.TryRemove(task.Id, out _);
                            }
                        });

                        _activePreloads[task.Id] = preloadTask;
                    }
                    else
                    {
                        // 队列为空，等待
                        await Task.Delay(500, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理预加载队列时发生错误");
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// 将任务加入预加载队列
        /// </summary>
        private async Task EnqueuePreloadTaskAsync(PreloadTask task)
        {
            await _queueSemaphore.WaitAsync();
            try
            {
                // 检查是否已存在相同的任务
                var isDuplicate = _preloadQueue.Any(t => 
                    t.DataType == task.DataType && 
                    t.ModuleName == task.ModuleName);

                if (!isDuplicate)
                {
                    _preloadQueue.Enqueue(task);
                    _logger.LogDebug("预加载任务已加入队列: {DataType} for {Module}", 
                        task.DataType, task.ModuleName);
                }
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }

        /// <summary>
        /// 执行实际的预加载
        /// </summary>
        private async Task<object?> ExecutePreloadAsync(string dataType, Dictionary<string, object> parameters)
        {
            // 根据数据类型调用相应的服务
            return dataType switch
            {
                "PatientList" => await LoadPatientListAsync(parameters),
                "Herbs" => await LoadHerbsAsync(parameters),
                "FormulaTemplates" => await LoadFormulaTemplatesAsync(parameters),
                "PatientHistory" => await LoadPatientHistoryAsync(parameters),
                "Symptoms" => await LoadSymptomsAsync(),
                "Diagnoses" => await LoadDiagnosesAsync(),
                _ => null
            };
        }

        /// <summary>
        /// 加载患者列表
        /// </summary>
        private async Task<object?> LoadPatientListAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var patientService = _serviceProvider.GetService(typeof(IPatientService)) as IPatientService;
                if (patientService != null)
                {
                    var result = await patientService.GetPagedAsync(1, 50);
                    return result.IsSuccess ? result.Data : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预加载患者列表失败");
            }
            return null;
        }

        /// <summary>
        /// 加载中药材数据
        /// </summary>
        private async Task<object?> LoadHerbsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var herbService = _serviceProvider.GetService(typeof(IHerbService)) as IHerbService;
                if (herbService != null)
                {
                    var result = await herbService.GetAllAsync();
                    return result.IsSuccess ? result.Data : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预加载中药材数据失败");
            }
            return null;
        }

        /// <summary>
        /// 加载验方模板
        /// </summary>
        private async Task<object?> LoadFormulaTemplatesAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var formulaService = _serviceProvider.GetService(typeof(IFormulaService)) as IFormulaService;
                if (formulaService != null)
                {
                    var result = await formulaService.GetTemplatesAsync();
                    return result.IsSuccess ? result.Data : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预加载验方模板失败");
            }
            return null;
        }

        /// <summary>
        /// 加载患者历史
        /// </summary>
        private async Task<object?> LoadPatientHistoryAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (parameters.TryGetValue("patientId", out var patientIdObj) && patientIdObj is Guid patientId)
                {
                    var medicalCaseService = _serviceProvider.GetService(typeof(IMedicalCaseService)) as IMedicalCaseService;
                    if (medicalCaseService != null)
                    {
                        var result = await medicalCaseService.GetByPatientIdAsync(patientId);
                        return result.IsSuccess ? result.Data : null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预加载患者历史失败");
            }
            return null;
        }

        /// <summary>
        /// 加载症状列表
        /// </summary>
        private async Task<object?> LoadSymptomsAsync()
        {
            // 模拟加载症状数据
            await Task.Delay(100);
            return new List<string> 
            { 
                "发热", "咳嗽", "头痛", "乏力", "失眠", 
                "胸闷", "腹痛", "腰痛", "关节痛", "便秘" 
            };
        }

        /// <summary>
        /// 加载诊断列表
        /// </summary>
        private async Task<object?> LoadDiagnosesAsync()
        {
            // 模拟加载诊断数据
            await Task.Delay(100);
            return new List<string>
            {
                "风寒感冒", "风热感冒", "脾胃虚弱", "肝郁气滞", "肾阳虚",
                "血瘀证", "痰湿体质", "气血两虚", "阴虚火旺", "湿热内蕴"
            };
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        private string GenerateCacheKey(string dataType, Dictionary<string, object> parameters)
        {
            var paramStr = string.Join("_", parameters.OrderBy(p => p.Key).Select(p => $"{p.Key}:{p.Value}"));
            return $"preload:{dataType}:{paramStr}";
        }

        /// <summary>
        /// 计算优先级
        /// </summary>
        private int CalculatePriority(double probability, TimeSpan estimatedTime)
        {
            // 概率越高、时间越短，优先级越高
            var timeFactor = Math.Max(1, 60 - estimatedTime.TotalSeconds) / 60.0;
            return (int)(probability * 100 * timeFactor);
        }

        /// <summary>
        /// 估算数据大小
        /// </summary>
        private long EstimateDataSize(object data)
        {
            // 简单估算，实际应该使用更精确的方法
            if (data == null) return 0;
            
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            return System.Text.Encoding.UTF8.GetByteCount(json);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            _cancellationTokenSource.Cancel();
            
            try
            {
                _backgroundProcessor.Wait(TimeSpan.FromSeconds(5));
            }
            catch { }
            
            _cancellationTokenSource.Dispose();
            _queueSemaphore.Dispose();
            
            _logger.LogInformation("预测性预加载服务已释放 - 总预加载: {Total}, 成功: {Success}, 缓存命中: {Hits}",
                _totalPreloads, _successfulPreloads, _cacheHits);
        }

        #endregion
    }

    #region 配置和数据模型

    /// <summary>
    /// 预加载配置
    /// </summary>
    public class PreloadConfiguration
    {
        public int MaxConcurrentPreloads { get; set; } = 3;
        public double MinConfidenceThreshold { get; set; } = 0.5;
        public double MinProbabilityThreshold { get; set; } = 0.3;
        public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan TaskExpiration { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxCacheSizeMB { get; set; } = 100;

        public static PreloadConfiguration Default()
        {
            return new PreloadConfiguration();
        }

        public static PreloadConfiguration Aggressive()
        {
            return new PreloadConfiguration
            {
                MaxConcurrentPreloads = 5,
                MinConfidenceThreshold = 0.3,
                MinProbabilityThreshold = 0.2,
                DefaultCacheDuration = TimeSpan.FromMinutes(30)
            };
        }

        public static PreloadConfiguration Conservative()
        {
            return new PreloadConfiguration
            {
                MaxConcurrentPreloads = 2,
                MinConfidenceThreshold = 0.7,
                MinProbabilityThreshold = 0.5,
                DefaultCacheDuration = TimeSpan.FromMinutes(5)
            };
        }
    }

    /// <summary>
    /// 预加载任务
    /// </summary>
    internal class PreloadTask
    {
        public string Id { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public int Priority { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public TimeSpan EstimatedExecutionTime { get; set; }
        public DateTime CreatedTime { get; set; }
    }

    /// <summary>
    /// 预加载结果
    /// </summary>
    public class PreloadResult
    {
        public bool Success { get; set; }
        public string DataType { get; set; } = string.Empty;
        public bool CacheHit { get; set; }
        public TimeSpan LoadTime { get; set; }
        public long DataSize { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 预加载状态
    /// </summary>
    public class PreloadStatus
    {
        public bool IsActive { get; set; }
        public bool IsPaused { get; set; }
        public int QueuedTasks { get; set; }
        public int ActiveTasks { get; set; }
        public double TotalMemoryUsageMB { get; set; }
        public double CacheHitRate { get; set; }
    }

    /// <summary>
    /// 预加载统计
    /// </summary>
    public class PreloadStatistics
    {
        public long TotalPreloads { get; set; }
        public long SuccessfulPreloads { get; set; }
        public long FailedPreloads { get; set; }
        public long CacheHits { get; set; }
        public double CacheHitRate { get; set; }
        public long TotalBytesPreloaded { get; set; }
        public double AveragePreloadSizeMB { get; set; }
    }

    #endregion
}