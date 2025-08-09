using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using LYBT.Infrastructure.Data;

namespace LYBT.Infrastructure.Performance.Database.Components
{
    /// <summary>
    /// 批量操作执行器 - UltraThink专门化组件
    /// 职责单一：专注批量数据库操作的高效执行和事务管理
    /// 代码干净：清晰的批量操作逻辑和错误处理
    /// 性能出色：优化的批处理算法和资源管理
    /// </summary>
    public class BatchOperationExecutor
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BatchOperationExecutor> _logger;
        private const int DefaultBatchSize = 1000;

        public BatchOperationExecutor(AppDbContext context, ILogger<BatchOperationExecutor> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心批量操作方法

        /// <summary>
        /// 执行批量操作
        /// </summary>
        public async Task<BatchOperationResult> ExecuteBatchOperationAsync<T>(
            IEnumerable<T> entities, 
            BatchOperationType operationType, 
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            var entitiesList = entities.ToList();
            var result = new BatchOperationResult();
            var stopwatch = Stopwatch.StartNew();

            if (entitiesList.Count == 0)
            {
                _logger.LogWarning("批量操作：实体列表为空");
                return result;
            }

            try
            {
                _logger.LogInformation("开始批量操作: {OperationType}, 数量: {Count}", operationType, entitiesList.Count);

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    result = operationType switch
                    {
                        BatchOperationType.Insert => await ExecuteBatchInsertAsync(entitiesList, cancellationToken),
                        BatchOperationType.Update => await ExecuteBatchUpdateAsync(entitiesList, cancellationToken),
                        BatchOperationType.Delete => await ExecuteBatchDeleteAsync(entitiesList, cancellationToken),
                        BatchOperationType.Upsert => await ExecuteBatchUpsertAsync(entitiesList, cancellationToken),
                        _ => throw new ArgumentException($"不支持的批量操作类型: {operationType}")
                    };

                    await transaction.CommitAsync(cancellationToken);
                    
                    _logger.LogInformation("批量操作完成: {OperationType}, 成功: {Success}, 失败: {Failed}, 耗时: {ElapsedMs}ms", 
                        operationType, result.SuccessCount, result.FailureCount, result.ExecutionTimeMs);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "批量操作事务回滚: {OperationType}", operationType);
                    result.Errors.Add($"事务失败: {ex.Message}");
                    result.FailureCount = entitiesList.Count;
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量操作失败: {OperationType}", operationType);
                result.Errors.Add(ex.Message);
                result.FailureCount = entitiesList.Count;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 执行批量操作（带配置选项）
        /// </summary>
        public async Task<BatchOperationResult> ExecuteBatchOperationAsync<T>(
            IEnumerable<T> entities,
            BatchOperationType operationType,
            BatchOperationOptions options,
            CancellationToken cancellationToken = default) where T : class
        {
            var entitiesList = entities.ToList();
            var result = new BatchOperationResult();

            if (options.ValidateBeforeOperation)
            {
                var validationResult = ValidateEntities(entitiesList);
                if (!validationResult.IsValid)
                {
                    result.Errors.AddRange(validationResult.Errors);
                    result.FailureCount = entitiesList.Count;
                    return result;
                }
            }

            // 根据配置调整批次大小
            var batchSize = options.BatchSize ?? DefaultBatchSize;
            
            if (entitiesList.Count <= batchSize)
            {
                return await ExecuteBatchOperationAsync(entitiesList, operationType, cancellationToken);
            }
            else
            {
                return await ExecuteLargeBatchOperationAsync(entitiesList, operationType, batchSize, cancellationToken);
            }
        }

        #endregion

        #region 具体批量操作实现

        /// <summary>
        /// 批量插入
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchInsertAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            var result = new BatchOperationResult();
            
            try
            {
                // 分批处理以避免内存压力
                const int batchSize = DefaultBatchSize;
                
                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    
                    _context.Set<T>().AddRange(batch);
                    var saved = await _context.SaveChangesAsync(cancellationToken);
                    result.SuccessCount += saved;

                    _logger.LogDebug("批量插入进度: {Progress}/{Total}", 
                        Math.Min(i + batchSize, entities.Count), entities.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量插入失败");
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count - result.SuccessCount;
            }

            return result;
        }

        /// <summary>
        /// 批量更新
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchUpdateAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            var result = new BatchOperationResult();
            
            try
            {
                _context.Set<T>().UpdateRange(entities);
                var saved = await _context.SaveChangesAsync(cancellationToken);
                result.SuccessCount = saved;

                _logger.LogDebug("批量更新完成: {Count} 个实体", saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新失败");
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count;
            }

            return result;
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchDeleteAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            var result = new BatchOperationResult();
            
            try
            {
                _context.Set<T>().RemoveRange(entities);
                var saved = await _context.SaveChangesAsync(cancellationToken);
                result.SuccessCount = saved;

                _logger.LogDebug("批量删除完成: {Count} 个实体", saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除失败");
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count;
            }

            return result;
        }

        /// <summary>
        /// 批量Upsert（更新或插入）
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchUpsertAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            var result = new BatchOperationResult();
            
            try
            {
                var insertCount = 0;
                var updateCount = 0;

                foreach (var entity in entities)
                {
                    var entityId = GetEntityId(entity);
                    if (entityId == null)
                    {
                        result.Errors.Add($"无法获取实体 {typeof(T).Name} 的ID");
                        result.FailureCount++;
                        continue;
                    }

                    var existingEntity = await _context.Set<T>().FindAsync(new object[] { entityId }, cancellationToken);
                    
                    if (existingEntity != null)
                    {
                        _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                        updateCount++;
                    }
                    else
                    {
                        _context.Set<T>().Add(entity);
                        insertCount++;
                    }
                }
                
                var saved = await _context.SaveChangesAsync(cancellationToken);
                result.SuccessCount = saved;

                _logger.LogDebug("批量Upsert完成: 插入 {InsertCount}, 更新 {UpdateCount}", insertCount, updateCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量Upsert失败");
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count - result.SuccessCount;
            }

            return result;
        }

        #endregion

        #region 大批量操作处理

        /// <summary>
        /// 执行大批量操作（分批处理）
        /// </summary>
        private async Task<BatchOperationResult> ExecuteLargeBatchOperationAsync<T>(
            List<T> entities,
            BatchOperationType operationType,
            int batchSize,
            CancellationToken cancellationToken) where T : class
        {
            var totalResult = new BatchOperationResult();
            var totalBatches = (int)Math.Ceiling((double)entities.Count / batchSize);

            try
            {
                _logger.LogInformation("开始大批量操作: {OperationType}, 总数: {Total}, 批次大小: {BatchSize}, 批次数: {Batches}",
                    operationType, entities.Count, batchSize, totalBatches);

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var batch = entities.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                    
                    var batchResult = await ExecuteBatchOperationAsync(batch, operationType, cancellationToken);
                    
                    // 合并结果
                    totalResult.SuccessCount += batchResult.SuccessCount;
                    totalResult.FailureCount += batchResult.FailureCount;
                    totalResult.Errors.AddRange(batchResult.Errors);
                    totalResult.ExecutionTimeMs += batchResult.ExecutionTimeMs;

                    _logger.LogDebug("批次 {BatchIndex}/{TotalBatches} 完成: 成功 {Success}, 失败 {Failed}",
                        batchIndex + 1, totalBatches, batchResult.SuccessCount, batchResult.FailureCount);

                    // 检查是否应该停止（如果有太多错误）
                    if (totalResult.FailureCount > entities.Count * 0.1) // 失败率超过10%
                    {
                        _logger.LogWarning("批量操作失败率过高，停止执行");
                        totalResult.Errors.Add("批量操作失败率过高，停止执行");
                        break;
                    }
                }

                _logger.LogInformation("大批量操作完成: 总成功 {Success}, 总失败 {Failed}, 总耗时 {Time}ms",
                    totalResult.SuccessCount, totalResult.FailureCount, totalResult.ExecutionTimeMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "大批量操作失败");
                totalResult.Errors.Add($"大批量操作异常: {ex.Message}");
            }

            return totalResult;
        }

        #endregion

        #region 验证和辅助方法

        /// <summary>
        /// 验证实体集合
        /// </summary>
        private ValidationResult ValidateEntities<T>(List<T> entities)
        {
            var result = new ValidationResult { IsValid = true };

            if (!entities.Any())
            {
                result.IsValid = false;
                result.Errors.Add("实体集合不能为空");
                return result;
            }

            // 检查实体是否为null
            var nullCount = entities.Count(e => e == null);
            if (nullCount > 0)
            {
                result.IsValid = false;
                result.Errors.Add($"发现 {nullCount} 个空实体");
            }

            // 可以添加更多特定的验证逻辑
            return result;
        }

        /// <summary>
        /// 获取实体ID（简化实现）
        /// </summary>
        private object? GetEntityId<T>(T entity)
        {
            try
            {
                // 尝试获取Id属性
                var property = typeof(T).GetProperty("Id");
                return property?.GetValue(entity);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取实体ID失败: {EntityType}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// 创建操作统计报告
        /// </summary>
        public BatchOperationStatistics CreateStatistics(BatchOperationResult result, int totalEntities)
        {
            return new BatchOperationStatistics
            {
                TotalEntities = totalEntities,
                SuccessCount = result.SuccessCount,
                FailureCount = result.FailureCount,
                SuccessRate = totalEntities > 0 ? (double)result.SuccessCount / totalEntities * 100 : 0,
                ExecutionTimeMs = result.ExecutionTimeMs,
                EntitiesPerSecond = result.ExecutionTimeMs > 0 ? (double)result.SuccessCount / (result.ExecutionTimeMs / 1000.0) : 0,
                ErrorCount = result.Errors.Count
            };
        }

        #endregion

        #region 内部数据类

        /// <summary>
        /// 验证结果
        /// </summary>
        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
        }

        #endregion
    }

    #region 配置类

    /// <summary>
    /// 批量操作配置选项
    /// </summary>
    public class BatchOperationOptions
    {
        /// <summary>
        /// 批次大小
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// 操作前是否验证
        /// </summary>
        public bool ValidateBeforeOperation { get; set; } = true;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// 批量操作统计信息
    /// </summary>
    public class BatchOperationStatistics
    {
        public int TotalEntities { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double SuccessRate { get; set; }
        public long ExecutionTimeMs { get; set; }
        public double EntitiesPerSecond { get; set; }
        public int ErrorCount { get; set; }
    }

    #endregion
}