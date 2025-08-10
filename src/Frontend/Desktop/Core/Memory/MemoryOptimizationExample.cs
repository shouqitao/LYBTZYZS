using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Async;
using LYBT.WPF.Client.Core.Caching;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.ObjectPool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace LYBT.WPF.Client.Core.Memory
{
    /// <summary>
    /// 内存优化使用示例
    /// </summary>
    public class MemoryOptimizationExample
    {
        private readonly IMemoryCacheService _cache;
        private readonly IObjectPoolService _poolService;
        private readonly IEnhancedEventAggregator _eventAggregator;
        private readonly WeakEventManager<PatientChangedEventArgs> _patientChangedEvent;
        private readonly ILogger<MemoryOptimizationExample> _logger;

        public MemoryOptimizationExample(
            IMemoryCacheService cache,
            IObjectPoolService poolService,
            IEnhancedEventAggregator eventAggregator,
            ILogger<MemoryOptimizationExample> logger)
        {
            _cache = cache;
            _poolService = poolService;
            _eventAggregator = eventAggregator;
            _logger = logger;
            _patientChangedEvent = new WeakEventManager<PatientChangedEventArgs>();
        }

        /// <summary>
        /// 示例1：使用弱事件防止内存泄漏
        /// </summary>
        public class PatientViewModel : IDisposable
        {
            private readonly WeakEventManager<EventArgs> _propertyChangedManager = new();
            
            public event EventHandler<EventArgs> DataChanged
            {
                add => _propertyChangedManager.Subscribe(value);
                remove => _propertyChangedManager.Unsubscribe(value);
            }

            public void NotifyDataChanged()
            {
                _propertyChangedManager.Raise(this, EventArgs.Empty);
            }

            public void Dispose()
            {
                _propertyChangedManager.Clear();
            }
        }

        /// <summary>
        /// 示例2：智能缓存使用
        /// </summary>
        public async Task<PatientData> GetPatientWithCacheAsync(Guid patientId)
        {
            var cacheKey = CacheKeyGenerator.Generate<PatientData>("GetById", patientId);
            
            return await _cache.GetAsync(cacheKey, 
                async () =>
                {
                    _logger.LogInformation($"从数据库加载患者: {patientId}");
                    // 模拟数据库查询
                    await Task.Delay(100);
                    return new PatientData 
                    { 
                        Id = patientId, 
                        Name = "张三",
                        Age = 30 
                    };
                },
                CacheOptions.MediumTerm); // 30分钟缓存
        }

        /// <summary>
        /// 示例3：对象池减少GC压力
        /// </summary>
        public async Task ProcessPrescriptionItemsAsync(IEnumerable<string> herbNames)
        {
            // 使用对象池获取列表
            var pool = _poolService.GetPool<List<PrescriptionItem>>();
            
            await pool.UseAsync(async items =>
            {
                // 处理处方项
                foreach (var herbName in herbNames)
                {
                    items.Add(new PrescriptionItem 
                    { 
                        HerbName = herbName,
                        Dosage = 10,
                        Unit = "g"
                    });
                }
                
                // 批量保存
                await SavePrescriptionItemsAsync(items);
                
                return items.Count;
            }); // 自动归还列表到池
        }

        /// <summary>
        /// 示例4：增强事件聚合器
        /// </summary>
        public void SetupEnhancedEvents()
        {
            // 订阅事件（自动弱引用）
            _eventAggregator.GetEvent<PatientSelectedEvent>()
                .Subscribe(
                    OnPatientSelected,
                    ThreadOption.UIThread,
                    keepSubscriberReferenceAlive: false, // 弱引用
                    filter: p => p.PatientId != Guid.Empty, // 过滤无效消息
                    priority: 10); // 高优先级

            // 发布事件
            _eventAggregator.GetEvent<PatientSelectedEvent>()
                .Publish(new PatientSelectedPayload { PatientId = Guid.NewGuid() });
        }

        /// <summary>
        /// 示例5：异步操作优化
        /// </summary>
        public async Task<IEnumerable<PatientData>> LoadPatientsOptimizedAsync(IEnumerable<Guid> patientIds)
        {
            // 并行加载，限制并发度
            var patients = await AsyncOptimization.BatchAsync(
                patientIds,
                async id => await GetPatientWithCacheAsync(id),
                batchSize: 5); // 每批5个

            // 异步过滤
            var activePatients = await patients.WhereAsync(
                async p => await CheckPatientActiveAsync(p));

            return activePatients;
        }

        /// <summary>
        /// 示例6：数组池高性能操作
        /// </summary>
        public async Task ProcessLargeDataAsync(int dataSize)
        {
            var arrayPool = new ArrayPoolWrapper<byte>();
            
            // 使用数组池处理大数据
            var result = await arrayPool.Use(dataSize, async buffer =>
            {
                // 填充数据
                for (int i = 0; i < dataSize; i++)
                {
                    buffer[i] = (byte)(i % 256);
                }
                
                // 处理数据
                await ProcessBufferAsync(buffer, dataSize);
                
                return buffer.Length;
            }, clearArray: true); // 归还时清理数组
        }

        /// <summary>
        /// 示例7：缓存统计和监控
        /// </summary>
        public void MonitorPerformance()
        {
            // 缓存统计
            var cacheStats = _cache.GetStatistics();
            _logger.LogInformation($@"
缓存统计:
- 命中率: {cacheStats.HitRate:P}
- 当前项数: {cacheStats.CurrentItemCount}
- 估计大小: {cacheStats.EstimatedSize / 1024}KB
- 驱逐次数: {cacheStats.Evictions}");

            // 事件统计
            var eventStats = _eventAggregator.GetStatistics();
            _logger.LogInformation($@"
事件统计:
- 总事件数: {eventStats.TotalEvents}
- 活跃订阅: {eventStats.TotalSubscriptions}");

            // 对象池统计
            var poolStats = _poolService.GetStatistics<List<PrescriptionItem>>();
            _logger.LogInformation($@"
对象池统计:
- 租用次数: {poolStats.RentCount}
- 归还次数: {poolStats.ReturnCount}
- 归还率: {poolStats.ReturnRate:P}
- 活跃对象: {poolStats.ActiveCount}");
        }

        /// <summary>
        /// 示例8：内存压缩和清理
        /// </summary>
        public void OptimizeMemory()
        {
            // 压缩缓存（移除10%最少使用的项）
            _cache.Compact(0.1);
            
            // 清理事件死订阅
            _eventAggregator.Cleanup();
            
            // 强制GC（仅在必要时）
            if (GC.GetTotalMemory(false) > 100_000_000) // 100MB
            {
                GC.Collect(2, GCCollectionMode.Optimized);
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                _logger.LogInformation($"内存优化完成，当前内存: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
            }
        }

        #region 辅助方法和类

        private async Task SavePrescriptionItemsAsync(List<PrescriptionItem> items)
        {
            await Task.Delay(10); // 模拟保存
            _logger.LogDebug($"保存 {items.Count} 个处方项");
        }

        private async Task<bool> CheckPatientActiveAsync(PatientData patient)
        {
            await Task.Delay(1); // 模拟检查
            return patient.Age < 100;
        }

        private async Task ProcessBufferAsync(byte[] buffer, int length)
        {
            await Task.Delay(10); // 模拟处理
            _logger.LogDebug($"处理 {length} 字节数据");
        }

        private void OnPatientSelected(PatientSelectedPayload payload)
        {
            _logger.LogInformation($"选中患者: {payload.PatientId}");
        }

        public class PatientData
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        public class PrescriptionItem
        {
            public string HerbName { get; set; } = string.Empty;
            public decimal Dosage { get; set; }
            public string Unit { get; set; } = string.Empty;
        }

        public class PatientChangedEventArgs : EventArgs
        {
            public Guid PatientId { get; set; }
            public string ChangedProperty { get; set; } = string.Empty;
        }

        public class PatientSelectedEvent : EnhancedEvent<PatientSelectedPayload> { }

        public class PatientSelectedPayload
        {
            public Guid PatientId { get; set; }
        }

        #endregion
    }
}