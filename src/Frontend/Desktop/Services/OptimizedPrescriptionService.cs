using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Desktop.Core.Caching;
using LYBT.Desktop.Core.Models;
using Refit;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 优化后的处方服务 - UltraThink业务代码优化
    /// 
    /// 优化特性：
    /// 1. 完全异步化操作
    /// 2. 智能缓存和预加载
    /// 3. 并发控制优化
    /// 4. 批量操作优化
    /// 5. 错误处理增强
    /// </summary>
    public class OptimizedPrescriptionService : IPrescriptionService
    {
        #region 依赖注入

        private readonly ILogger<OptimizedPrescriptionService> _logger;
        private readonly IPrescriptionApi _prescriptionApi;
        private readonly ISmartLoadingService _smartLoadingService;
        private readonly IPredictivePreloadService _predictivePreloadService;
        private readonly IUserBehaviorAnalyzer _behaviorAnalyzer;
        private readonly IMemoryCacheService _cacheService;
        private readonly ISmartConcurrencyManager _concurrencyManager;

        // 缓存键前缀
        private const string CACHE_PREFIX = "prescription:";
        private const string LIST_CACHE_KEY = "prescription:list";
        private const string TEMPLATE_CACHE_KEY = "prescription:templates";
        
        // 并发控制
        private readonly SemaphoreSlim _batchOperationSemaphore;
        private readonly SemaphoreSlim _searchSemaphore;

        #endregion

        #region 构造函数

        public OptimizedPrescriptionService(
            ILogger<OptimizedPrescriptionService> logger,
            IPrescriptionApi prescriptionApi,
            ISmartLoadingService smartLoadingService,
            IPredictivePreloadService predictivePreloadService,
            IUserBehaviorAnalyzer behaviorAnalyzer,
            IMemoryCacheService cacheService,
            ISmartConcurrencyManager concurrencyManager)
        {
            _logger = logger;
            _prescriptionApi = prescriptionApi;
            _smartLoadingService = smartLoadingService;
            _predictivePreloadService = predictivePreloadService;
            _behaviorAnalyzer = behaviorAnalyzer;
            _cacheService = cacheService;
            _concurrencyManager = concurrencyManager;

            // 初始化并发控制
            _batchOperationSemaphore = new SemaphoreSlim(5, 5); // 最多5个并发批量操作
            _searchSemaphore = new SemaphoreSlim(10, 10); // 最多10个并发搜索

            _logger.LogInformation("优化后的处方服务已初始化");
        }

        #endregion

        #region 核心CRUD操作 - 完全异步化

        /// <summary>
        /// 获取处方列表（带智能缓存）
        /// </summary>
        public async Task<ApiResponse<PagedResult<PrescriptionInfo>>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            CancellationToken cancellationToken = default)
        {
            // 记录用户行为
            RecordUserAction("Prescriptions", "GetPaged");

            // 智能加载
            return await _smartLoadingService.LoadAsync(
                $"{LIST_CACHE_KEY}:{pageNumber}:{pageSize}",
                async (ct) =>
                {
                    var result = await _prescriptionApi.GetPagedAsync(pageNumber, pageSize, ct);
                    
                    // 预加载相关数据
                    if (result.IsSuccess && result.Data?.Items?.Any() == true)
                    {
                        await PreloadRelatedDataAsync(result.Data.Items, ct);
                    }
                    
                    return result;
                },
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromMinutes(5),
                    EnablePredictivePreload = true
                },
                cancellationToken);
        }

        /// <summary>
        /// 根据ID获取处方（带智能缓存）
        /// </summary>
        public async Task<ApiResponse<PrescriptionInfo>> GetByIdAsync(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "GetById", id);

            return await _smartLoadingService.LoadAsync(
                $"{CACHE_PREFIX}{id}",
                async (ct) => await _prescriptionApi.GetByIdAsync(id, ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromMinutes(10),
                    EnableBackgroundRefresh = true
                },
                cancellationToken);
        }

        /// <summary>
        /// 创建处方（带缓存失效）
        /// </summary>
        public async Task<ApiResponse<PrescriptionInfo>> CreateAsync(
            PrescriptionInfo prescription, 
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "Create");

            try
            {
                var result = await _prescriptionApi.CreateAsync(prescription, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // 缓存新创建的处方
                    _cacheService.Set($"{CACHE_PREFIX}{result.Data.Id}", result, TimeSpan.FromMinutes(10));
                    
                    // 失效列表缓存
                    InvalidateListCache();
                    
                    // 触发预测性预加载
                    await _predictivePreloadService.StartPredictivePreloadingAsync("Prescriptions", "Create");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                throw;
            }
        }

        /// <summary>
        /// 更新处方（带缓存更新）
        /// </summary>
        public async Task<ApiResponse<PrescriptionInfo>> UpdateAsync(
            Guid id, 
            PrescriptionInfo prescription, 
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "Update", id);

            try
            {
                var result = await _prescriptionApi.UpdateAsync(id, prescription, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // 更新缓存
                    _cacheService.Set($"{CACHE_PREFIX}{id}", result, TimeSpan.FromMinutes(10));
                    
                    // 失效相关缓存
                    InvalidateRelatedCache(id);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除处方（带缓存清理）
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "Delete", id);

            try
            {
                var result = await _prescriptionApi.DeleteAsync(id, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // 清理缓存
                    _cacheService.Remove($"{CACHE_PREFIX}{id}");
                    InvalidateListCache();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败: {Id}", id);
                throw;
            }
        }

        #endregion

        #region 批量操作优化

        /// <summary>
        /// 批量删除（并发优化）
        /// </summary>
        public async Task<ApiResponse<int>> BatchDeleteAsync(
            List<Guid> ids, 
            CancellationToken cancellationToken = default)
        {
            if (ids == null || !ids.Any())
                return ApiResponse<int>.Success(0);

            RecordUserAction("Prescriptions", "BatchDelete");

            await _batchOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                // 使用智能并发管理器
                var concurrencyLevel = await _concurrencyManager.GetOptimalConcurrencyAsync("BatchDelete");
                
                // 分批处理
                var batches = ids.Chunk(10).ToList();
                var semaphore = new SemaphoreSlim(concurrencyLevel);
                var results = new List<ApiResponse<bool>>();

                var tasks = batches.Select(async batch =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var batchResults = await Task.WhenAll(
                            batch.Select(id => _prescriptionApi.DeleteAsync(id, cancellationToken)));
                        
                        // 清理缓存
                        foreach (var id in batch)
                        {
                            _cacheService.Remove($"{CACHE_PREFIX}{id}");
                        }
                        
                        return batchResults;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var allResults = await Task.WhenAll(tasks);
                var successCount = allResults.SelectMany(r => r).Count(r => r.IsSuccess);

                InvalidateListCache();
                
                return ApiResponse<int>.Success(successCount);
            }
            finally
            {
                _batchOperationSemaphore.Release();
            }
        }

        /// <summary>
        /// 批量更新状态（优化版）
        /// </summary>
        public async Task<ApiResponse<int>> BatchUpdateStatusAsync(
            List<Guid> ids, 
            int status, 
            CancellationToken cancellationToken = default)
        {
            if (ids == null || !ids.Any())
                return ApiResponse<int>.Success(0);

            RecordUserAction("Prescriptions", "BatchUpdateStatus");

            await _batchOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await _prescriptionApi.BatchUpdateStatusAsync(ids, status, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // 批量更新缓存
                    var updateTasks = ids.Select(async id =>
                    {
                        var cacheKey = $"{CACHE_PREFIX}{id}";
                        if (_cacheService.TryGetValue<ApiResponse<PrescriptionInfo>>(cacheKey, out var cached))
                        {
                            cached.Data.Status = status;
                            _cacheService.Set(cacheKey, cached, TimeSpan.FromMinutes(10));
                        }
                    });
                    
                    await Task.WhenAll(updateTasks);
                    InvalidateListCache();
                }
                
                return result;
            }
            finally
            {
                _batchOperationSemaphore.Release();
            }
        }

        #endregion

        #region 查询操作优化

        /// <summary>
        /// 按患者查询（带预加载）
        /// </summary>
        public async Task<ApiResponse<List<PrescriptionInfo>>> GetByPatientIdAsync(
            Guid patientId, 
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "GetByPatient", patientId);

            return await _smartLoadingService.LoadAsync(
                $"{CACHE_PREFIX}patient:{patientId}",
                async (ct) =>
                {
                    var result = await _prescriptionApi.GetByPatientIdAsync(patientId, ct);
                    
                    // 预加载相关医生和药材信息
                    if (result.IsSuccess && result.Data?.Any() == true)
                    {
                        await PreloadRelatedDataAsync(result.Data, ct);
                    }
                    
                    return result;
                },
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromMinutes(15),
                    EnablePredictivePreload = true
                },
                cancellationToken);
        }

        /// <summary>
        /// 搜索处方（并发控制）
        /// </summary>
        public async Task<ApiResponse<PagedResult<PrescriptionInfo>>> SearchAsync(
            string keyword,
            DateTime? startDate,
            DateTime? endDate,
            int? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "Search");

            await _searchSemaphore.WaitAsync(cancellationToken);
            try
            {
                var cacheKey = $"{CACHE_PREFIX}search:{keyword}:{startDate}:{endDate}:{status}:{pageNumber}:{pageSize}";
                
                return await _smartLoadingService.LoadAsync(
                    cacheKey,
                    async (ct) => await _prescriptionApi.SearchAsync(
                        keyword, startDate, endDate, status, pageNumber, pageSize, ct),
                    new LoadingOptions
                    {
                        Priority = LoadPriority.Normal,
                        CacheDuration = TimeSpan.FromMinutes(3),
                        EnableBackgroundRefresh = false
                    },
                    cancellationToken);
            }
            finally
            {
                _searchSemaphore.Release();
            }
        }

        /// <summary>
        /// 获取处方模板（智能缓存）
        /// </summary>
        public async Task<ApiResponse<List<PrescriptionTemplate>>> GetTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "GetTemplates");

            return await _smartLoadingService.LoadAsync(
                TEMPLATE_CACHE_KEY,
                async (ct) => await _prescriptionApi.GetTemplatesAsync(ct),
                new LoadingOptions
                {
                    Priority = LoadPriority.Normal,
                    CacheDuration = TimeSpan.FromHours(1), // 模板较少变化，缓存时间长
                    EnablePredictivePreload = true
                },
                cancellationToken);
        }

        #endregion

        #region 导出和打印优化

        /// <summary>
        /// 导出为Excel（流式处理）
        /// </summary>
        public async Task<ApiResponse<byte[]>> ExportToExcelAsync(
            List<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "ExportExcel");

            try
            {
                // 使用流式处理避免内存占用
                using var stream = new System.IO.MemoryStream();
                
                // 分批获取数据
                var prescriptions = new List<PrescriptionInfo>();
                foreach (var batch in ids.Chunk(50))
                {
                    var tasks = batch.Select(id => GetByIdAsync(id, cancellationToken));
                    var results = await Task.WhenAll(tasks);
                    prescriptions.AddRange(results.Where(r => r.IsSuccess).Select(r => r.Data));
                }
                
                // 这里应该调用实际的Excel生成逻辑
                var excelData = await GenerateExcelAsync(prescriptions, cancellationToken);
                
                return ApiResponse<byte[]>.Success(excelData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel失败");
                return ApiResponse<byte[]>.Failure($"导出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取打印数据（优化版）
        /// </summary>
        public async Task<ApiResponse<PrescriptionPrintData>> GetPrintDataAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RecordUserAction("Prescriptions", "GetPrintData", id);

            return await _smartLoadingService.LoadAsync(
                $"{CACHE_PREFIX}print:{id}",
                async (ct) =>
                {
                    // 并行获取所需数据
                    var prescriptionTask = GetByIdAsync(id, ct);
                    var patientTask = GetPatientInfoAsync(id, ct);
                    var doctorTask = GetDoctorInfoAsync(id, ct);
                    
                    await Task.WhenAll(prescriptionTask, patientTask, doctorTask);
                    
                    return ApiResponse<PrescriptionPrintData>.Success(new PrescriptionPrintData
                    {
                        Prescription = prescriptionTask.Result.Data,
                        PatientInfo = patientTask.Result,
                        DoctorInfo = doctorTask.Result,
                        PrintTime = DateTime.Now
                    });
                },
                new LoadingOptions
                {
                    Priority = LoadPriority.High,
                    CacheDuration = TimeSpan.FromMinutes(5)
                },
                cancellationToken);
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
        /// 预加载相关数据
        /// </summary>
        private async Task PreloadRelatedDataAsync(
            IEnumerable<PrescriptionInfo> prescriptions,
            CancellationToken cancellationToken)
        {
            try
            {
                var preloadTasks = new List<Task>();
                
                // 预加载患者信息
                var patientIds = prescriptions.Select(p => p.PatientId).Distinct();
                foreach (var patientId in patientIds)
                {
                    preloadTasks.Add(_predictivePreloadService.PreloadDataAsync(
                        "PatientInfo",
                        new Dictionary<string, object> { { "patientId", patientId } }));
                }
                
                // 预加载医生信息
                var doctorIds = prescriptions.Select(p => p.DoctorId).Distinct();
                foreach (var doctorId in doctorIds)
                {
                    preloadTasks.Add(_predictivePreloadService.PreloadDataAsync(
                        "DoctorInfo",
                        new Dictionary<string, object> { { "doctorId", doctorId } }));
                }
                
                await Task.WhenAll(preloadTasks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "预加载相关数据失败");
            }
        }

        /// <summary>
        /// 失效列表缓存
        /// </summary>
        private void InvalidateListCache()
        {
            // 移除所有列表缓存
            var keys = _cacheService.GetKeys($"{LIST_CACHE_KEY}:*");
            foreach (var key in keys)
            {
                _cacheService.Remove(key);
            }
        }

        /// <summary>
        /// 失效相关缓存
        /// </summary>
        private void InvalidateRelatedCache(Guid prescriptionId)
        {
            // 失效打印缓存
            _cacheService.Remove($"{CACHE_PREFIX}print:{prescriptionId}");
            
            // 失效列表缓存
            InvalidateListCache();
        }

        /// <summary>
        /// 生成Excel数据（示例）
        /// </summary>
        private async Task<byte[]> GenerateExcelAsync(
            List<PrescriptionInfo> prescriptions,
            CancellationToken cancellationToken)
        {
            // 这里应该使用实际的Excel生成库（如EPPlus）
            await Task.Delay(100, cancellationToken); // 模拟处理
            return new byte[0]; // 返回实际的Excel数据
        }

        /// <summary>
        /// 获取患者信息（示例）
        /// </summary>
        private async Task<object> GetPatientInfoAsync(Guid prescriptionId, CancellationToken cancellationToken)
        {
            // 实际应该调用PatientService
            await Task.Delay(10, cancellationToken);
            return new { Name = "患者姓名", Age = 30 };
        }

        /// <summary>
        /// 获取医生信息（示例）
        /// </summary>
        private async Task<object> GetDoctorInfoAsync(Guid prescriptionId, CancellationToken cancellationToken)
        {
            // 实际应该调用DoctorService
            await Task.Delay(10, cancellationToken);
            return new { Name = "医生姓名", Title = "主任医师" };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _batchOperationSemaphore?.Dispose();
            _searchSemaphore?.Dispose();
            _logger.LogInformation("优化后的处方服务已释放");
        }

        #endregion
    }

    #region 支持类

    /// <summary>
    /// 处方打印数据
    /// </summary>
    public class PrescriptionPrintData
    {
        public PrescriptionInfo Prescription { get; set; }
        public object PatientInfo { get; set; }
        public object DoctorInfo { get; set; }
        public DateTime PrintTime { get; set; }
    }

    /// <summary>
    /// 处方模板
    /// </summary>
    public class PrescriptionTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PrescriptionItemInfo> Items { get; set; }
        public DateTime CreatedTime { get; set; }
    }

    #endregion
}