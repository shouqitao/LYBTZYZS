using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Cache;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Herbs;

namespace LYBT.WPF.Client.Services.Cache
{
    /// <summary>
    /// 缓存服务使用示例
    /// 演示如何在各种场景中使用企业级缓存服务
    /// </summary>
    public class CacheServiceUsageExamples
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheServiceUsageExamples> _logger;

        public CacheServiceUsageExamples(ICacheService cacheService, ILogger<CacheServiceUsageExamples> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        #region 基础使用示例

        /// <summary>
        /// 示例1：基础缓存操作
        /// </summary>
        public void BasicCacheOperations()
        {
            var key = "user_profile_123";
            var userProfile = new { UserId = 123, Name = "张三", Role = "医生" };

            // 设置缓存 - 5分钟过期
            _cacheService.Set(key, userProfile, TimeSpan.FromMinutes(5));

            // 获取缓存
            var cachedProfile = _cacheService.Get<object>(key);
            if (cachedProfile != null)
            {
                _logger.LogInformation("缓存命中：用户资料已从缓存获取");
            }

            // 检查是否存在
            if (_cacheService.Exists(key))
            {
                _logger.LogInformation("缓存项存在");
            }

            // 移除缓存
            _cacheService.Remove(key);
        }

        /// <summary>
        /// 示例2：使用缓存策略
        /// </summary>
        public void CachePolicyExamples()
        {
            // 滑动过期策略：最后访问后30分钟过期
            var slidingPolicy = CachePolicy.Sliding(TimeSpan.FromMinutes(30));
            _cacheService.Set("herbs_list", new List<string> { "人参", "当归", "黄芪" }, slidingPolicy);

            // 绝对过期策略：在特定时间过期
            var absolutePolicy = CachePolicy.Absolute(DateTimeOffset.Now.AddHours(2));
            _cacheService.Set("daily_report", "今日诊疗统计数据", absolutePolicy);

            // 自定义策略：高优先级，带分区
            var customPolicy = new CachePolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(15),
                Priority = CachePriority.High,
                Partition = "medical_data"
            };
            _cacheService.Set("patient_history", new List<object>(), customPolicy);
        }

        #endregion

        #region 异步操作示例

        /// <summary>
        /// 示例3：异步获取或创建
        /// </summary>
        public async Task<List<PatientInfo>> GetOrCreatePatientsAsync()
        {
            var key = "active_patients";
            
            // 使用GetOrCreateAsync - 如果缓存不存在，会执行工厂方法
            var patients = await _cacheService.GetOrCreateAsync(key, async () =>
            {
                // 模拟从数据库或API加载数据
                _logger.LogInformation("从数据源加载患者列表");
                await Task.Delay(1000); // 模拟IO操作
                
                return new List<PatientInfo>
                {
                    new PatientInfo { Id = Guid.NewGuid(), Name = "李四", Age = 35 },
                    new PatientInfo { Id = Guid.NewGuid(), Name = "王五", Age = 42 }
                };
            }, TimeSpan.FromMinutes(10));

            return patients;
        }

        /// <summary>
        /// 示例4：缓存失效和刷新
        /// </summary>
        public async Task<List<HerbInfo>> RefreshHerbsCache()
        {
            var key = "herbs_catalog";
            
            // 强制刷新：先移除旧缓存
            _cacheService.Remove(key);
            
            // 重新加载并缓存
            var herbs = await _cacheService.GetOrCreateAsync(key, async () =>
            {
                _logger.LogInformation("重新加载中药材目录");
                // 实际应用中这里会调用API或数据库
                return new List<HerbInfo>();
            }, TimeSpan.FromHours(1));

            return herbs;
        }

        #endregion

        #region 批量操作示例

        /// <summary>
        /// 示例5：批量缓存操作
        /// </summary>
        public void BatchOperations()
        {
            // 批量设置
            var batchData = new Dictionary<string, object>
            {
                ["config_theme"] = "dark",
                ["config_language"] = "zh-CN",
                ["config_timeout"] = 30
            };
            _cacheService.SetMany(batchData, TimeSpan.FromHours(24));

            // 批量获取
            var keys = new[] { "config_theme", "config_language", "config_timeout" };
            var cachedConfigs = _cacheService.GetMany(keys);
            
            foreach (var config in cachedConfigs)
            {
                _logger.LogInformation("配置 {Key} = {Value}", config.Key, config.Value);
            }

            // 批量删除
            var keysToRemove = new[] { "temp_data_1", "temp_data_2", "temp_data_3" };
            var removedCount = _cacheService.RemoveMany(keysToRemove);
            _logger.LogInformation("批量删除了 {Count} 个缓存项", removedCount);
        }

        /// <summary>
        /// 示例6：模式匹配删除
        /// </summary>
        public void PatternBasedOperations()
        {
            // 设置一些测试数据
            _cacheService.Set("user_session_1", "用户1会话", TimeSpan.FromMinutes(30));
            _cacheService.Set("user_session_2", "用户2会话", TimeSpan.FromMinutes(30));
            _cacheService.Set("user_profile_1", "用户1资料", TimeSpan.FromHours(2));
            _cacheService.Set("system_config", "系统配置", TimeSpan.FromDays(1));

            // 删除所有用户会话（使用通配符）
            var removedSessions = _cacheService.RemoveByPattern("user_session_*");
            _logger.LogInformation("删除了 {Count} 个用户会话缓存", removedSessions);

            // 删除所有用户相关数据
            var removedUserData = _cacheService.RemoveByPattern("user_*");
            _logger.LogInformation("删除了 {Count} 个用户相关缓存", removedUserData);
        }

        #endregion

        #region 缓存管理示例

        /// <summary>
        /// 示例7：分区管理
        /// </summary>
        public void PartitionManagement()
        {
            // 将不同类型的数据放入不同分区
            var medicalPolicy = new CachePolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Partition = "medical"
            };

            var systemPolicy = new CachePolicy
            {
                SlidingExpiration = TimeSpan.FromHours(2),
                Partition = "system"
            };

            _cacheService.Set("patient_list", new List<object>(), medicalPolicy);
            _cacheService.Set("herb_catalog", new List<object>(), medicalPolicy);
            _cacheService.Set("app_settings", new Dictionary<string, object>(), systemPolicy);
            _cacheService.Set("user_preferences", new Dictionary<string, object>(), systemPolicy);

            // 清空特定分区
            _cacheService.ClearPartition("medical");
            _logger.LogInformation("已清空医疗数据分区");
        }

        /// <summary>
        /// 示例8：缓存监控和统计
        /// </summary>
        public void CacheMonitoring()
        {
            // 执行一些缓存操作
            _cacheService.Set("test1", "数据1", TimeSpan.FromMinutes(10));
            _cacheService.Set("test2", "数据2", TimeSpan.FromMinutes(10));
            
            _cacheService.Get<string>("test1"); // 命中
            _cacheService.Get<string>("test2"); // 命中
            _cacheService.Get<string>("test3"); // 未命中

            // 获取统计信息
            var stats = _cacheService.GetStatistics();
            
            _logger.LogInformation("缓存统计信息：");
            _logger.LogInformation("- 总项数: {ItemCount}", stats.ItemCount);
            _logger.LogInformation("- 命中率: {HitRate:P2}", stats.HitRate);
            _logger.LogInformation("- 总请求数: {TotalRequests}", stats.TotalRequests);
            _logger.LogInformation("- 内存使用: {MemoryUsage:F2} MB", stats.EstimatedMemoryUsage / 1024.0 / 1024.0);

            // 重置统计
            _cacheService.ResetStatistics();
        }

        /// <summary>
        /// 示例9：缓存清理
        /// </summary>
        public void CacheCleanup()
        {
            // 手动触发清理（移除过期项）
            var cleanedCount = _cacheService.Cleanup();
            _logger.LogInformation("手动清理了 {Count} 个过期项", cleanedCount);

            // 获取所有缓存键
            var allKeys = _cacheService.GetAllKeys();
            _logger.LogInformation("当前缓存键: {Keys}", string.Join(", ", allKeys));

            // 清空所有缓存
            _cacheService.Clear();
            _logger.LogInformation("已清空所有缓存，当前项数: {Count}", _cacheService.Count);
        }

        #endregion

        #region 实际业务场景示例

        /// <summary>
        /// 示例10：医疗业务场景 - 患者诊疗数据缓存
        /// </summary>
        public async Task<object> MedicalScenarioExample()
        {
            // 患者基本信息（较长缓存时间）
            var patientKey = "patient_123";
            var patientInfo = await _cacheService.GetOrCreateAsync(patientKey, async () =>
            {
                // 模拟从数据库加载患者信息
                await Task.Delay(100); // 模拟异步操作
                return new { Id = 123, Name = "张三", Age = 45, Gender = "男" };
            }, TimeSpan.FromHours(2));

            // 当日就诊记录（较短缓存时间）
            var consultationKey = "consultation_today_123";
            var todayConsultations = await _cacheService.GetOrCreateAsync(consultationKey, async () =>
            {
                // 模拟从数据库加载当日就诊记录
                await Task.Delay(50);
                return new List<object>();
            }, TimeSpan.FromMinutes(30));

            // 常用处方模板（长期缓存）
            var formulaKey = "common_formulas";
            var commonFormulas = await _cacheService.GetOrCreateAsync(formulaKey, async () =>
            {
                // 模拟加载常用处方模板
                await Task.Delay(200);
                return new List<object>();
            }, new CachePolicy
            {
                SlidingExpiration = TimeSpan.FromHours(4),
                Priority = CachePriority.High,
                Partition = "formulas"
            });

            return new
            {
                Patient = patientInfo,
                TodayConsultations = todayConsultations,
                CommonFormulas = commonFormulas
            };
        }

        /// <summary>
        /// 示例11：性能优化场景 - 预加载和缓存预热
        /// </summary>
        public async Task CacheWarmupExample()
        {
            _logger.LogInformation("开始缓存预热...");

            // 预加载常用数据
            var warmupTasks = new Task[]
            {
                _cacheService.GetOrCreateAsync("herbs_list", async () =>
                {
                    // 预加载中药材列表
                    await Task.Delay(500); // 模拟加载时间
                    return new List<string> { "人参", "当归", "黄芪", "甘草" };
                }, TimeSpan.FromHours(2)),

                _cacheService.GetOrCreateAsync("departments", async () =>
                {
                    // 预加载科室列表
                    await Task.Delay(300);
                    return new List<string> { "内科", "外科", "儿科" };
                }, TimeSpan.FromHours(1)),

                _cacheService.GetOrCreateAsync("doctors", async () =>
                {
                    // 预加载医生列表
                    await Task.Delay(400);
                    return new List<object>();
                }, TimeSpan.FromMinutes(30))
            };

            await Task.WhenAll(warmupTasks);
            _logger.LogInformation("缓存预热完成");
        }

        #endregion
    }
}