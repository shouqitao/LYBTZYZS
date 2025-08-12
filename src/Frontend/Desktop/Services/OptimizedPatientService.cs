using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Desktop.Core.Caching;
using LYBT.Desktop.Core.Models;
using Refit;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 优化后的患者服务 - UltraThink业务代码优化
    /// 
    /// 优化特性：
    /// 1. 智能搜索策略
    /// 2. 多级缓存机制
    /// 3. 预测性数据加载
    /// 4. 并发查询优化
    /// 5. 代码复用和模块化
    /// </summary>
    public class OptimizedPatientService : IPatientService
    {
        #region 依赖注入

        private readonly ILogger<OptimizedPatientService> _logger;
        private readonly IPatientApi _patientApi;
        private readonly ISmartLoadingService _smartLoadingService;
        private readonly IPredictivePreloadService _predictivePreloadService;
        private readonly IUserBehaviorAnalyzer _behaviorAnalyzer;
        private readonly IMemoryCacheService _cacheService;
        private readonly ISmartConcurrencyManager _concurrencyManager;

        // 缓存配置
        private const string CACHE_PREFIX = "patient:";
        private const string SEARCH_CACHE_PREFIX = "patient:search:";
        private const string RECENT_CACHE_KEY = "patient:recent";
        
        // 搜索优化
        private readonly SearchOptimizer _searchOptimizer;
        private readonly SemaphoreSlim _searchThrottle;

        #endregion

        #region 构造函数

        public OptimizedPatientService(
            ILogger<OptimizedPatientService> logger,
            IPatientApi patientApi,
            ISmartLoadingService smartLoadingService,
            IPredictivePreloadService predictivePreloadService,
            IUserBehaviorAnalyzer behaviorAnalyzer,
            IMemoryCacheService cacheService,
            ISmartConcurrencyManager concurrencyManager)
        {
            _logger = logger;
            _patientApi = patientApi;
            _smartLoadingService = smartLoadingService;
            _predictivePreloadService = predictivePreloadService;
            _behaviorAnalyzer = behaviorAnalyzer;
            _cacheService = cacheService;
            _concurrencyManager = concurrencyManager;

            // 初始化搜索优化器
            _searchOptimizer = new SearchOptimizer(_cacheService, _logger);
            _searchThrottle = new SemaphoreSlim(5, 5); // 限制并发搜索数

            _logger.LogInformation("优化后的患者服务已初始化");
        }

        #endregion

        #region 核心CRUD操作

        /// <summary>
        /// 获取患者列表（智能加载）
        /// </summary>
        public async Task<ApiResponse<PagedResult<PatientInfo>>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "GetPaged");

            return await _smartLoadingService.LoadAsync(
                $"{CACHE_PREFIX}list:{pageNumber}:{pageSize}",
                async (ct) =>
                {
                    var result = await _patientApi.GetPagedAsync(pageNumber, pageSize, ct);
                    
                    // 智能预加载
                    if (result.IsSuccess && result.Data?.Items?.Any() == true)
                    {
                        await PreloadFrequentPatients(result.Data.Items, ct);
                    }
                    
                    return result;
                },
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromMinutes(10),
                    EnablePredictivePreload = true
                },
                cancellationToken);
        }

        /// <summary>
        /// 根据ID获取患者
        /// </summary>
        public async Task<ApiResponse<PatientInfo>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "GetById", id);

            return await _smartLoadingService.LoadAsync(
                $"{CACHE_PREFIX}{id}",
                async (ct) =>
                {
                    var result = await _patientApi.GetByIdAsync(id, ct);
                    
                    // 预加载相关数据
                    if (result.IsSuccess)
                    {
                        await PreloadPatientRelatedData(id, ct);
                    }
                    
                    return result;
                },
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromMinutes(30),
                    EnableBackgroundRefresh = true
                },
                cancellationToken);
        }

        /// <summary>
        /// 创建患者
        /// </summary>
        public async Task<ApiResponse<PatientInfo>> CreateAsync(
            PatientInfo patient,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "Create");

            try
            {
                var result = await _patientApi.CreateAsync(patient, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // 缓存新患者
                    _cacheService.Set($"{CACHE_PREFIX}{result.Data.Id}", result, TimeSpan.FromMinutes(30));
                    
                    // 更新最近患者缓存
                    await UpdateRecentPatientsCache(result.Data);
                    
                    // 失效列表缓存
                    InvalidateListCache();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者失败");
                throw;
            }
        }

        /// <summary>
        /// 更新患者
        /// </summary>
        public async Task<ApiResponse<PatientInfo>> UpdateAsync(
            Guid id,
            PatientInfo patient,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "Update", id);

            try
            {
                var result = await _patientApi.UpdateAsync(id, patient, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // 更新缓存
                    _cacheService.Set($"{CACHE_PREFIX}{id}", result, TimeSpan.FromMinutes(30));
                    
                    // 失效搜索缓存
                    InvalidateSearchCache();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "Delete", id);

            try
            {
                var result = await _patientApi.DeleteAsync(id, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // 清理所有相关缓存
                    ClearPatientCache(id);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败: {Id}", id);
                throw;
            }
        }

        #endregion

        #region 智能搜索优化

        /// <summary>
        /// 统一搜索接口（智能路由）
        /// </summary>
        public async Task<ApiResponse<List<PatientInfo>>> SmartSearchAsync(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "SmartSearch");

            // 使用搜索优化器判断最佳搜索策略
            var strategy = _searchOptimizer.DetermineStrategy(criteria);
            
            switch (strategy)
            {
                case SearchStrategy.ById:
                    return await SearchByIdInternalAsync(criteria.Id.Value, cancellationToken);
                    
                case SearchStrategy.ByPhone:
                    return await SearchByPhoneInternalAsync(criteria.Phone, cancellationToken);
                    
                case SearchStrategy.ByName:
                    return await SearchByNameInternalAsync(criteria.Name, cancellationToken);
                    
                case SearchStrategy.Combined:
                    return await CombinedSearchAsync(criteria, cancellationToken);
                    
                default:
                    return await FullSearchAsync(criteria, cancellationToken);
            }
        }

        /// <summary>
        /// 按姓名搜索（带模糊匹配）
        /// </summary>
        public async Task<ApiResponse<List<PatientInfo>>> SearchByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ApiResponse<List<PatientInfo>>.Success(new List<PatientInfo>());

            return await SmartSearchAsync(new SearchCriteria { Name = name }, cancellationToken);
        }

        /// <summary>
        /// 按电话搜索（精确匹配）
        /// </summary>
        public async Task<ApiResponse<List<PatientInfo>>> SearchByPhoneAsync(
            string phone,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return ApiResponse<List<PatientInfo>>.Success(new List<PatientInfo>());

            return await SmartSearchAsync(new SearchCriteria { Phone = phone }, cancellationToken);
        }

        /// <summary>
        /// 按身份证搜索
        /// </summary>
        public async Task<ApiResponse<PatientInfo>> SearchByIdCardAsync(
            string idCard,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "SearchByIdCard");

            return await _smartLoadingService.LoadAsync(
                $"{SEARCH_CACHE_PREFIX}idcard:{idCard}",
                async (ct) => await _patientApi.SearchByIdCardAsync(idCard, ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromHours(1) // 身份证信息较稳定，缓存时间长
                },
                cancellationToken);
        }

        /// <summary>
        /// 获取最近就诊患者
        /// </summary>
        public async Task<ApiResponse<List<PatientInfo>>> GetRecentPatientsAsync(
            int days = 7,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "GetRecent");

            return await _smartLoadingService.LoadAsync(
                $"{RECENT_CACHE_KEY}:{days}:{limit}",
                async (ct) =>
                {
                    var result = await _patientApi.GetRecentPatientsAsync(days, limit, ct);
                    
                    // 预加载最近患者的处方信息
                    if (result.IsSuccess && result.Data?.Any() == true)
                    {
                        await PreloadRecentPatientsPrescriptions(result.Data, ct);
                    }
                    
                    return result;
                },
                new LoadingOptions
                {
                    Priority = LoadPriority.Normal,
                    CacheDuration = TimeSpan.FromMinutes(5),
                    EnablePredictivePreload = true
                },
                cancellationToken);
        }

        #endregion

        #region 统计和分析

        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        public async Task<ApiResponse<PatientStatistics>> GetStatisticsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "GetStatistics");

            var cacheKey = $"{CACHE_PREFIX}stats:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            
            return await _smartLoadingService.LoadAsync(
                cacheKey,
                async (ct) => await _patientApi.GetStatisticsAsync(startDate, endDate, ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.Low,
                    CacheDuration = TimeSpan.FromHours(1)
                },
                cancellationToken);
        }

        /// <summary>
        /// 获取患者就诊历史
        /// </summary>
        public async Task<ApiResponse<List<VisitHistory>>> GetVisitHistoryAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "GetVisitHistory", patientId);

            return await _smartLoadingService.LoadAsync(
                $"{CACHE_PREFIX}history:{patientId}",
                async (ct) => await _patientApi.GetVisitHistoryAsync(patientId, ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.Normal,
                    CacheDuration = TimeSpan.FromMinutes(15),
                    EnableBackgroundRefresh = true
                },
                cancellationToken);
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量导入患者
        /// </summary>
        public async Task<ApiResponse<BatchImportResult>> BatchImportAsync(
            List<PatientInfo> patients,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Patients", "BatchImport");

            try
            {
                // 使用智能并发管理
                var concurrencyLevel = await _concurrencyManager.GetOptimalConcurrencyAsync("BatchImport");
                var semaphore = new SemaphoreSlim(concurrencyLevel);
                
                var results = new List<ImportResult>();
                var batches = patients.Chunk(20).ToList();

                foreach (var batch in batches)
                {
                    var tasks = batch.Select(async patient =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var result = await CreateAsync(patient, cancellationToken);
                            return new ImportResult
                            {
                                Patient = patient,
                                Success = result.IsSuccess,
                                ErrorMessage = result.IsSuccess ? null : result.Message
                            };
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    var batchResults = await Task.WhenAll(tasks);
                    results.AddRange(batchResults);
                }

                return ApiResponse<BatchImportResult>.Success(new BatchImportResult
                {
                    TotalCount = patients.Count,
                    SuccessCount = results.Count(r => r.Success),
                    FailedCount = results.Count(r => !r.Success),
                    Details = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者失败");
                throw;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 记录用户行为
        /// </summary>
        private void RecordUserAction(string module, string action, Guid? entityId = null)
        {
            _behaviorAnalyzer.RecordAction(new UserAction
            {
                ModuleName = module,
                ActionName = action,
                EntityId = entityId,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 预加载频繁访问的患者
        /// </summary>
        private async Task PreloadFrequentPatients(
            IEnumerable<PatientInfo> patients,
            CancellationToken cancellationToken)
        {
            try
            {
                // 获取用户行为分析
                var frequentPatientIds = await _behaviorAnalyzer.GetFrequentEntitiesAsync("Patient", 10);
                
                var preloadTasks = patients
                    .Where(p => frequentPatientIds.Contains(p.Id))
                    .Select(p => _predictivePreloadService.PreloadDataAsync(
                        "PatientDetails",
                        new Dictionary<string, object> { { "patientId", p.Id } }));
                
                await Task.WhenAll(preloadTasks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "预加载频繁患者数据失败");
            }
        }

        /// <summary>
        /// 预加载患者相关数据
        /// </summary>
        private async Task PreloadPatientRelatedData(Guid patientId, CancellationToken cancellationToken)
        {
            var tasks = new[]
            {
                _predictivePreloadService.PreloadDataAsync(
                    "PatientHistory",
                    new Dictionary<string, object> { { "patientId", patientId } }),
                    
                _predictivePreloadService.PreloadDataAsync(
                    "RecentPrescriptions",
                    new Dictionary<string, object> { { "patientId", patientId } }),
                    
                _predictivePreloadService.PreloadDataAsync(
                    "MedicalRecords",
                    new Dictionary<string, object> { { "patientId", patientId } })
            };
            
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 预加载最近患者的处方
        /// </summary>
        private async Task PreloadRecentPatientsPrescriptions(
            IEnumerable<PatientInfo> patients,
            CancellationToken cancellationToken)
        {
            var tasks = patients.Take(5).Select(p =>
                _predictivePreloadService.PreloadDataAsync(
                    "RecentPrescriptions",
                    new Dictionary<string, object> { { "patientId", p.Id } }));
            
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 内部搜索实现
        /// </summary>
        private async Task<ApiResponse<List<PatientInfo>>> SearchByIdInternalAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await GetByIdAsync(id, cancellationToken);
            return result.IsSuccess
                ? ApiResponse<List<PatientInfo>>.Success(new List<PatientInfo> { result.Data })
                : ApiResponse<List<PatientInfo>>.Failure(result.Message);
        }

        private async Task<ApiResponse<List<PatientInfo>>> SearchByPhoneInternalAsync(
            string phone,
            CancellationToken cancellationToken)
        {
            return await _smartLoadingService.LoadAsync(
                $"{SEARCH_CACHE_PREFIX}phone:{phone}",
                async (ct) => await _patientApi.SearchByPhoneAsync(phone, ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromMinutes(30)
                },
                cancellationToken);
        }

        private async Task<ApiResponse<List<PatientInfo>>> SearchByNameInternalAsync(
            string name,
            CancellationToken cancellationToken)
        {
            return await _smartLoadingService.LoadAsync(
                $"{SEARCH_CACHE_PREFIX}name:{name}",
                async (ct) => await _patientApi.SearchByNameAsync(name, ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.Normal,
                    CacheDuration = TimeSpan.FromMinutes(15)
                },
                cancellationToken);
        }

        /// <summary>
        /// 组合搜索
        /// </summary>
        private async Task<ApiResponse<List<PatientInfo>>> CombinedSearchAsync(
            SearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            await _searchThrottle.WaitAsync(cancellationToken);
            try
            {
                // 并行执行多个搜索
                var tasks = new List<Task<ApiResponse<List<PatientInfo>>>>();
                
                if (!string.IsNullOrWhiteSpace(criteria.Name))
                    tasks.Add(SearchByNameInternalAsync(criteria.Name, cancellationToken));
                    
                if (!string.IsNullOrWhiteSpace(criteria.Phone))
                    tasks.Add(SearchByPhoneInternalAsync(criteria.Phone, cancellationToken));
                
                var results = await Task.WhenAll(tasks);
                
                // 合并结果并去重
                var allPatients = results
                    .Where(r => r.IsSuccess)
                    .SelectMany(r => r.Data)
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();
                
                return ApiResponse<List<PatientInfo>>.Success(allPatients);
            }
            finally
            {
                _searchThrottle.Release();
            }
        }

        /// <summary>
        /// 完整搜索
        /// </summary>
        private async Task<ApiResponse<List<PatientInfo>>> FullSearchAsync(
            SearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"{SEARCH_CACHE_PREFIX}full:{criteria.GetHashCode()}";
            
            return await _smartLoadingService.LoadAsync(
                cacheKey,
                async (ct) => await _patientApi.SearchAsync(criteria, ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.Normal,
                    CacheDuration = TimeSpan.FromMinutes(10)
                },
                cancellationToken);
        }

        /// <summary>
        /// 更新最近患者缓存
        /// </summary>
        private async Task UpdateRecentPatientsCache(PatientInfo patient)
        {
            if (_cacheService.TryGetValue<ApiResponse<List<PatientInfo>>>(RECENT_CACHE_KEY, out var cached))
            {
                cached.Data.Insert(0, patient);
                if (cached.Data.Count > 20)
                {
                    cached.Data.RemoveAt(cached.Data.Count - 1);
                }
                _cacheService.Set(RECENT_CACHE_KEY, cached, TimeSpan.FromMinutes(5));
            }
        }

        /// <summary>
        /// 清理患者缓存
        /// </summary>
        private void ClearPatientCache(Guid patientId)
        {
            _cacheService.Remove($"{CACHE_PREFIX}{patientId}");
            _cacheService.Remove($"{CACHE_PREFIX}history:{patientId}");
            InvalidateListCache();
            InvalidateSearchCache();
        }

        /// <summary>
        /// 失效列表缓存
        /// </summary>
        private void InvalidateListCache()
        {
            var keys = _cacheService.GetKeys($"{CACHE_PREFIX}list:*");
            foreach (var key in keys)
            {
                _cacheService.Remove(key);
            }
        }

        /// <summary>
        /// 失效搜索缓存
        /// </summary>
        private void InvalidateSearchCache()
        {
            var keys = _cacheService.GetKeys($"{SEARCH_CACHE_PREFIX}*");
            foreach (var key in keys)
            {
                _cacheService.Remove(key);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _searchThrottle?.Dispose();
            _logger.LogInformation("优化后的患者服务已释放");
        }

        #endregion
    }

    #region 支持类

    /// <summary>
    /// 搜索条件
    /// </summary>
    public class SearchCriteria
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string IdCard { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// 搜索策略
    /// </summary>
    public enum SearchStrategy
    {
        ById,
        ByPhone,
        ByName,
        ByIdCard,
        Combined,
        Full
    }

    /// <summary>
    /// 搜索优化器
    /// </summary>
    public class SearchOptimizer
    {
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger _logger;

        public SearchOptimizer(IMemoryCacheService cacheService, ILogger logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public SearchStrategy DetermineStrategy(SearchCriteria criteria)
        {
            // 根据条件判断最优搜索策略
            if (criteria.Id.HasValue)
                return SearchStrategy.ById;
                
            if (!string.IsNullOrWhiteSpace(criteria.IdCard))
                return SearchStrategy.ByIdCard;
                
            if (!string.IsNullOrWhiteSpace(criteria.Phone))
                return SearchStrategy.ByPhone;
                
            if (!string.IsNullOrWhiteSpace(criteria.Name))
                return SearchStrategy.ByName;
                
            var hasMultipleCriteria = 
                (!string.IsNullOrWhiteSpace(criteria.Name) ? 1 : 0) +
                (!string.IsNullOrWhiteSpace(criteria.Phone) ? 1 : 0) +
                (criteria.StartDate.HasValue ? 1 : 0) > 1;
                
            return hasMultipleCriteria ? SearchStrategy.Combined : SearchStrategy.Full;
        }
    }

    /// <summary>
    /// 患者统计信息
    /// </summary>
    public class PatientStatistics
    {
        public int TotalPatients { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int ActivePatients { get; set; }
        public Dictionary<string, int> AgeDistribution { get; set; }
        public Dictionary<string, int> GenderDistribution { get; set; }
    }

    /// <summary>
    /// 就诊历史
    /// </summary>
    public class VisitHistory
    {
        public Guid Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string ChiefComplaint { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
    }

    /// <summary>
    /// 批量导入结果
    /// </summary>
    public class BatchImportResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<ImportResult> Details { get; set; }
    }

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult
    {
        public PatientInfo Patient { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion
}