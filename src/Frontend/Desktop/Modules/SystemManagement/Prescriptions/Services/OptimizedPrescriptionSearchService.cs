using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Admin.Prescriptions.Services
{
    /// <summary>
    /// 优化的处方搜索服务
    /// 集成防抖、缓存和批量处理功能
    /// </summary>
    public class OptimizedPrescriptionSearchService
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly ApiOptimizationService _apiOptimizer;
        private readonly ILogger<OptimizedPrescriptionSearchService> _logger;

        public OptimizedPrescriptionSearchService(
            IPrescriptionService prescriptionService,
            ApiOptimizationService apiOptimizer,
            ILogger<OptimizedPrescriptionSearchService> logger)
        {
            _prescriptionService = prescriptionService;
            _apiOptimizer = apiOptimizer;
            _logger = logger;
        }

        #region 防抖搜索

        /// <summary>
        /// 防抖搜索处方（用于实时搜索输入框）
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>搜索结果</returns>
        public async Task<List<PrescriptionInfo>> DebouncedSearchAsync(
            string keyword, 
            int pageIndex = 1, 
            int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<PrescriptionInfo>();

            // 使用防抖避免频繁的搜索请求
            var searchKey = $"search_{keyword}_{pageIndex}_{pageSize}";
            
            return await _apiOptimizer.DebounceAsync(searchKey, async () =>
            {
                _logger.LogDebug("执行防抖处方搜索: {Keyword}", keyword);
                
                var query = new PrescriptionPagedQueryDto
                {
                    Keyword = keyword,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                var result = await _prescriptionService.GetPagedAsync(query);
                return result.Items?.ToList() ?? new List<PrescriptionInfo>();
            }, TimeSpan.FromMilliseconds(500)); // 500ms延迟
        }

        /// <summary>
        /// 智能搜索建议（防抖 + 缓存）
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>搜索建议</returns>
        public async Task<List<string>> GetSearchSuggestionsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
                return new List<string>();

            var suggestionKey = $"suggestions_{keyword.ToLower()}";
            
            return await _apiOptimizer.DebounceAsync(suggestionKey, async () =>
            {
                _logger.LogDebug("获取搜索建议: {Keyword}", keyword);
                
                // 模拟搜索建议API调用
                var suggestions = await GenerateSearchSuggestions(keyword);
                return suggestions;
            }, TimeSpan.FromMilliseconds(300));
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量获取处方详情
        /// </summary>
        /// <param name="prescriptionIds">处方ID列表</param>
        /// <returns>处方详情列表</returns>
        public async Task<List<PrescriptionInfo?>> BatchGetPrescriptionsAsync(
            IEnumerable<Guid> prescriptionIds)
        {
            var ids = prescriptionIds.ToList();
            if (!ids.Any())
                return new List<PrescriptionInfo?>();

            var tasks = ids.Select(id => BatchGetSinglePrescriptionAsync(id)).ToList();
            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>
        /// 批量获取单个处方（内部使用批量处理优化）
        /// </summary>
        private async Task<PrescriptionInfo?> BatchGetSinglePrescriptionAsync(Guid prescriptionId)
        {
            return await _apiOptimizer.BatchAsync(
                "get_prescriptions", // 批处理键
                prescriptionId, // 输入
                async (ids) => // 批量API调用
                {
                    _logger.LogDebug("执行批量获取处方详情: {Count}个", ids.Count);
                    
                    var results = new List<PrescriptionInfo?>();
                    foreach (var id in ids)
                    {
                        var result = await _prescriptionService.GetByIdAsync(id);
                        results.Add(result);
                    }
                    return results;
                },
                TimeSpan.FromMilliseconds(200) // 200ms批处理窗口
            );
        }

        /// <summary>
        /// 批量更新处方状态
        /// </summary>
        /// <param name="prescriptionIds">处方ID列表</param>
        /// <param name="status">新状态</param>
        /// <returns>更新结果</returns>
        public async Task<List<bool>> BatchUpdateStatusAsync(
            IEnumerable<Guid> prescriptionIds, 
            int status)
        {
            var ids = prescriptionIds.ToList();
            if (!ids.Any())
                return new List<bool>();

            var tasks = ids.Select(id => BatchUpdateSingleStatusAsync(id, status)).ToList();
            return (await Task.WhenAll(tasks)).ToList();
        }

        private async Task<bool> BatchUpdateSingleStatusAsync(Guid prescriptionId, int status)
        {
            return await _apiOptimizer.BatchAsync(
                $"update_status_{status}", // 按状态分组批处理
                prescriptionId,
                async (ids) =>
                {
                    _logger.LogDebug("执行批量状态更新: {Count}个, 状态: {Status}", ids.Count, status);
                    
                    var results = new List<bool>();
                    foreach (var id in ids)
                    {
                        try
                        {
                            await _prescriptionService.UpdateStatusAsync(id, status);
                            results.Add(true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "更新处方状态失败: {Id}", id);
                            results.Add(false);
                        }
                    }
                    return results;
                },
                TimeSpan.FromMilliseconds(300)
            );
        }

        #endregion

        #region 智能重试

        /// <summary>
        /// 可靠的处方保存（带重试）
        /// </summary>
        /// <param name="prescription">处方数据</param>
        /// <returns>保存结果</returns>
        public async Task<bool> ReliableSaveAsync(CreatePrescriptionDto prescription)
        {
            return await _apiOptimizer.RetryAsync(async () =>
            {
                _logger.LogDebug("尝试保存处方");
                
                var result = await _prescriptionService.CreateAsync(prescription);
                
                if (!result.Success)
                {
                    throw new InvalidOperationException($"保存处方失败: {result.Message}");
                }

                return result.Success;
            }, 
            maxRetries: 3,
            baseDelay: TimeSpan.FromSeconds(1),
            shouldRetry: (ex, attempt) =>
            {
                // 只对网络错误和服务器错误重试
                return ex is TaskCanceledException || 
                       ex is TimeoutException ||
                       ex.Message.Contains("网络") ||
                       ex.Message.Contains("服务器");
            });
        }

        /// <summary>
        /// 可靠的数据同步（带指数退避重试）
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>同步结果</returns>
        public async Task<bool> ReliableSyncAsync(Guid prescriptionId)
        {
            return await _apiOptimizer.RetryAsync(async () =>
            {
                _logger.LogDebug("尝试同步处方数据: {Id}", prescriptionId);
                
                // 模拟数据同步操作
                await Task.Delay(100); // 模拟网络延迟
                
                // 模拟偶尔的网络失败
                if (Random.Shared.Next(1, 10) <= 2) // 20%失败率
                {
                    throw new TimeoutException("网络超时");
                }
                
                return true;
            },
            maxRetries: 5,
            baseDelay: TimeSpan.FromMilliseconds(500));
        }

        #endregion

        #region 预加载和预测

        /// <summary>
        /// 智能预加载相关处方
        /// </summary>
        /// <param name="currentPrescription">当前处方</param>
        /// <returns>预加载任务</returns>
        public async Task PreloadRelatedPrescriptionsAsync(PrescriptionInfo currentPrescription)
        {
            try
            {
                // 在后台预加载相关数据
                _ = Task.Run(async () =>
                {
                    _logger.LogDebug("预加载相关处方: {Id}", currentPrescription.Id);
                    
                    // 预加载同一患者的其他处方
                    if (currentPrescription.PatientId.HasValue)
                    {
                        await DebouncedSearchAsync($"patient_{currentPrescription.PatientId}", 1, 5);
                    }
                    
                    // 预加载使用相同药材的处方
                    var majorHerbs = currentPrescription.Items?
                        .Take(3)
                        .Select(i => i.HerbName)
                        .Where(name => !string.IsNullOrEmpty(name));
                    
                    if (majorHerbs?.Any() == true)
                    {
                        foreach (var herb in majorHerbs)
                        {
                            await DebouncedSearchAsync(herb!, 1, 3);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "预加载相关处方失败");
            }
        }

        /// <summary>
        /// 获取热门搜索关键词（优化缓存）
        /// </summary>
        /// <returns>热门关键词</returns>
        public async Task<List<string>> GetPopularKeywordsAsync()
        {
            return await _apiOptimizer.DebounceAsync("popular_keywords", async () =>
            {
                _logger.LogDebug("获取热门搜索关键词");
                
                // 模拟获取热门关键词
                return new List<string>
                {
                    "感冒", "咳嗽", "发热", "头痛", "胃痛", 
                    "失眠", "便秘", "腹泻", "高血压", "糖尿病"
                };
            }, TimeSpan.FromMinutes(10)); // 10分钟缓存
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 生成搜索建议
        /// </summary>
        private async Task<List<string>> GenerateSearchSuggestions(string keyword)
        {
            // 模拟搜索建议生成逻辑
            await Task.Delay(50); // 模拟处理时间
            
            var suggestions = new List<string>();
            var popularKeywords = await GetPopularKeywordsAsync();
            
            // 基于输入生成建议
            suggestions.AddRange(popularKeywords
                .Where(k => k.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Take(5));
            
            // 添加一些常见的医学术语建议
            if (keyword.Length >= 2)
            {
                var medicalTerms = new[]
                {
                    $"{keyword}汤", $"{keyword}散", $"{keyword}丸", $"{keyword}膏",
                    $"治疗{keyword}", $"{keyword}症状", $"{keyword}处方"
                };
                
                suggestions.AddRange(medicalTerms.Take(3));
            }
            
            return suggestions.Distinct().Take(8).ToList();
        }

        #endregion
    }
}