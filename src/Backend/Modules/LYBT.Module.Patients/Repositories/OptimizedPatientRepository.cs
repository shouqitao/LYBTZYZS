using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories.Optimized;
using LYBT.Module.Patients.Entities;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Repositories
{
    /// <summary>
    /// 优化的患者Repository - UltraThink数据访问层优化
    /// 
    /// 优化特性：
    /// 1. 智能查询策略
    /// 2. 索引优化利用
    /// 3. 预编译查询
    /// 4. 连接复用
    /// 5. 批量操作优化
    /// </summary>
    public class OptimizedPatientRepository : OptimizedBaseRepository<Patient>, IPatientRepository
    {
        // 预编译查询
        private static readonly Func<AppDbContext, string, Task<Patient?>> _compiledGetByPhone =
            EF.CompileAsyncQuery((AppDbContext ctx, string phone) =>
                ctx.Set<Patient>().FirstOrDefault(p => p.Phone == phone));
        
        private static readonly Func<AppDbContext, string, IAsyncEnumerable<Patient>> _compiledSearchByName =
            EF.CompileAsyncQuery((AppDbContext ctx, string name) =>
                ctx.Set<Patient>().Where(p => p.Name.Contains(name)));
        
        private static readonly Func<AppDbContext, DateTime, DateTime, IAsyncEnumerable<Patient>> _compiledGetRecent =
            EF.CompileAsyncQuery((AppDbContext ctx, DateTime startDate, DateTime endDate) =>
                ctx.Set<Patient>()
                    .Where(p => p.LastVisitDate >= startDate && p.LastVisitDate <= endDate)
                    .OrderByDescending(p => p.LastVisitDate));

        private readonly ILogger<OptimizedPatientRepository> _typedLogger;

        public OptimizedPatientRepository(
            AppDbContext context,
            ILogger<OptimizedPatientRepository> logger,
            IMemoryCache cache)
            : base(context, logger, cache, QueryOptimizationOptions.Performance)
        {
            _typedLogger = logger;
        }

        #region 优化的查询方法

        /// <summary>
        /// 智能搜索患者
        /// </summary>
        public async Task<IEnumerable<Patient>> SmartSearchAsync(
            PatientSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            return await MonitoredQueryAsync(async () =>
            {
                var query = BuildSearchQuery(criteria);
                
                // 应用智能排序
                query = ApplySmartOrdering(query, criteria);
                
                // 限制结果集大小
                if (criteria.MaxResults > 0)
                {
                    query = query.Take(criteria.MaxResults);
                }
                
                return await query.ToListAsync(cancellationToken);
            }, "SmartSearch");
        }

        /// <summary>
        /// 按电话查询（使用预编译查询）
        /// </summary>
        public async Task<Patient?> GetByPhoneAsync(
            string phone,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyPrefix}phone:{phone}";
            
            if (_cache.TryGetValue<Patient>(cacheKey, out var cached))
            {
                return cached;
            }
            
            var patient = await _compiledGetByPhone(_context, phone);
            
            if (patient != null)
            {
                _cache.Set(cacheKey, patient, TimeSpan.FromMinutes(30));
            }
            
            return patient;
        }

        /// <summary>
        /// 按姓名搜索（使用预编译查询）
        /// </summary>
        public async Task<IEnumerable<Patient>> SearchByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            var results = new List<Patient>();
            
            await foreach (var patient in _compiledSearchByName(_context, name)
                .WithCancellation(cancellationToken))
            {
                results.Add(patient);
            }
            
            return results;
        }

        /// <summary>
        /// 获取最近就诊患者（使用预编译查询）
        /// </summary>
        public async Task<IEnumerable<Patient>> GetRecentPatientsAsync(
            int days = 7,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            var startDate = DateTime.Now.AddDays(-days);
            var endDate = DateTime.Now;
            
            var cacheKey = $"{CacheKeyPrefix}recent:{days}:{limit}";
            
            if (_cache.TryGetValue<List<Patient>>(cacheKey, out var cached))
            {
                return cached!;
            }
            
            var results = new List<Patient>();
            var count = 0;
            
            await foreach (var patient in _compiledGetRecent(_context, startDate, endDate)
                .WithCancellation(cancellationToken))
            {
                results.Add(patient);
                if (++count >= limit) break;
            }
            
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(5));
            
            return results;
        }

        /// <summary>
        /// 批量查询优化
        /// </summary>
        public async Task<Dictionary<string, Patient>> GetByPhonesAsync(
            IEnumerable<string> phones,
            CancellationToken cancellationToken = default)
        {
            var phoneList = phones.ToList();
            var result = new Dictionary<string, Patient>();
            
            // 先从缓存获取
            var uncachedPhones = new List<string>();
            foreach (var phone in phoneList)
            {
                var cacheKey = $"{CacheKeyPrefix}phone:{phone}";
                if (_cache.TryGetValue<Patient>(cacheKey, out var cached))
                {
                    result[phone] = cached!;
                }
                else
                {
                    uncachedPhones.Add(phone);
                }
            }
            
            // 批量查询未缓存的数据
            if (uncachedPhones.Any())
            {
                var patients = await _dbSet
                    .AsNoTracking()
                    .Where(p => uncachedPhones.Contains(p.Phone))
                    .ToListAsync(cancellationToken);
                
                foreach (var patient in patients)
                {
                    result[patient.Phone] = patient;
                    _cache.Set($"{CacheKeyPrefix}phone:{patient.Phone}", patient, TimeSpan.FromMinutes(30));
                }
            }
            
            return result;
        }

        #endregion

        #region 统计和分析

        /// <summary>
        /// 获取患者统计信息（优化版）
        /// </summary>
        public async Task<PatientStatistics> GetStatisticsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyPrefix}stats:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            
            if (_cache.TryGetValue<PatientStatistics>(cacheKey, out var cached))
            {
                return cached!;
            }
            
            // 并行执行多个统计查询
            var query = _dbSet.AsNoTracking();
            
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);
            }
            
            var totalTask = query.CountAsync(cancellationToken);
            var newThisMonthTask = query
                .Where(p => p.CreatedAt >= DateTime.Now.AddMonths(-1))
                .CountAsync(cancellationToken);
            var activeTask = query
                .Where(p => p.LastVisitDate >= DateTime.Now.AddMonths(-3))
                .CountAsync(cancellationToken);
            
            // 年龄分布
            var ageDistributionTask = query
                .GroupBy(p => EF.Functions.DateDiffYear(p.BirthDate, DateTime.Now) / 10 * 10)
                .Select(g => new { AgeGroup = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => $"{x.AgeGroup}-{x.AgeGroup + 9}", x => x.Count, cancellationToken);
            
            // 性别分布
            var genderDistributionTask = query
                .GroupBy(p => p.Gender)
                .Select(g => new { Gender = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Gender.ToString(), x => x.Count, cancellationToken);
            
            await Task.WhenAll(totalTask, newThisMonthTask, activeTask, ageDistributionTask, genderDistributionTask);
            
            var statistics = new PatientStatistics
            {
                TotalPatients = await totalTask,
                NewPatientsThisMonth = await newThisMonthTask,
                ActivePatients = await activeTask,
                AgeDistribution = await ageDistributionTask,
                GenderDistribution = await genderDistributionTask
            };
            
            _cache.Set(cacheKey, statistics, TimeSpan.FromHours(1));
            
            return statistics;
        }

        #endregion

        #region 批量操作优化

        /// <summary>
        /// 批量导入患者（事务优化）
        /// </summary>
        public async Task<BatchImportResult> BatchImportAsync(
            IEnumerable<Patient> patients,
            CancellationToken cancellationToken = default)
        {
            var patientList = patients.ToList();
            var results = new List<ImportResult>();
            
            await BulkOperationAsync(async context =>
            {
                var successCount = 0;
                
                foreach (var batch in patientList.Chunk(50))
                {
                    try
                    {
                        // 检查重复
                        var phones = batch.Select(p => p.Phone).ToList();
                        var existingPhones = await context.Set<Patient>()
                            .Where(p => phones.Contains(p.Phone))
                            .Select(p => p.Phone)
                            .ToListAsync(cancellationToken);
                        
                        var newPatients = batch.Where(p => !existingPhones.Contains(p.Phone)).ToList();
                        
                        if (newPatients.Any())
                        {
                            await context.Set<Patient>().AddRangeAsync(newPatients, cancellationToken);
                            await context.SaveChangesAsync(cancellationToken);
                            successCount += newPatients.Count;
                            
                            foreach (var patient in newPatients)
                            {
                                results.Add(new ImportResult
                                {
                                    Patient = patient,
                                    Success = true
                                });
                            }
                        }
                        
                        // 记录重复的
                        foreach (var duplicate in batch.Where(p => existingPhones.Contains(p.Phone)))
                        {
                            results.Add(new ImportResult
                            {
                                Patient = duplicate,
                                Success = false,
                                ErrorMessage = "电话号码已存在"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _typedLogger.LogError(ex, "批量导入批次失败");
                        foreach (var patient in batch)
                        {
                            results.Add(new ImportResult
                            {
                                Patient = patient,
                                Success = false,
                                ErrorMessage = ex.Message
                            });
                        }
                    }
                }
                
                return successCount;
            }, cancellationToken);
            
            return new BatchImportResult
            {
                TotalCount = patientList.Count,
                SuccessCount = results.Count(r => r.Success),
                FailedCount = results.Count(r => !r.Success),
                Details = results
            };
        }

        /// <summary>
        /// 批量更新最后就诊时间
        /// </summary>
        public async Task<int> UpdateLastVisitDateAsync(
            Dictionary<Guid, DateTime> updates,
            CancellationToken cancellationToken = default)
        {
            if (!updates.Any()) return 0;
            
            var updated = 0;
            
            // 使用原生SQL批量更新以获得最佳性能
            var sql = @"
                UPDATE Patients 
                SET LastVisitDate = @visitDate, UpdatedAt = @now 
                WHERE Id = @id";
            
            foreach (var batch in updates.Chunk(100))
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    foreach (var update in batch)
                    {
                        updated += await _context.Database.ExecuteSqlRawAsync(
                            sql,
                            new Microsoft.Data.SqlClient.SqlParameter("@id", update.Key),
                            new Microsoft.Data.SqlClient.SqlParameter("@visitDate", update.Value),
                            new Microsoft.Data.SqlClient.SqlParameter("@now", DateTime.Now));
                    }
                    
                    await transaction.CommitAsync(cancellationToken);
                    
                    // 清理缓存
                    foreach (var id in batch.Select(b => b.Key))
                    {
                        _cache.Remove($"{CacheKeyPrefix}{id}");
                    }
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            
            return updated;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 构建搜索查询
        /// </summary>
        private IQueryable<Patient> BuildSearchQuery(PatientSearchCriteria criteria)
        {
            var query = _dbSet.AsNoTracking();
            
            // 应用过滤条件
            if (!string.IsNullOrWhiteSpace(criteria.Name))
            {
                query = query.Where(p => p.Name.Contains(criteria.Name));
            }
            
            if (!string.IsNullOrWhiteSpace(criteria.Phone))
            {
                query = query.Where(p => p.Phone == criteria.Phone);
            }
            
            if (!string.IsNullOrWhiteSpace(criteria.IdCard))
            {
                query = query.Where(p => p.IdCard == criteria.IdCard);
            }
            
            if (criteria.Gender.HasValue)
            {
                query = query.Where(p => p.Gender == criteria.Gender.Value);
            }
            
            if (criteria.MinAge.HasValue)
            {
                var maxBirthDate = DateTime.Now.AddYears(-criteria.MinAge.Value);
                query = query.Where(p => p.BirthDate <= maxBirthDate);
            }
            
            if (criteria.MaxAge.HasValue)
            {
                var minBirthDate = DateTime.Now.AddYears(-criteria.MaxAge.Value - 1);
                query = query.Where(p => p.BirthDate >= minBirthDate);
            }
            
            if (criteria.StartDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= criteria.StartDate.Value);
            }
            
            if (criteria.EndDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= criteria.EndDate.Value);
            }
            
            return query;
        }

        /// <summary>
        /// 应用智能排序
        /// </summary>
        private IQueryable<Patient> ApplySmartOrdering(
            IQueryable<Patient> query,
            PatientSearchCriteria criteria)
        {
            // 根据搜索条件智能决定排序策略
            if (!string.IsNullOrWhiteSpace(criteria.Name))
            {
                // 如果按姓名搜索，按姓名相关度排序
                query = query.OrderBy(p => p.Name.IndexOf(criteria.Name));
            }
            else if (criteria.StartDate.HasValue || criteria.EndDate.HasValue)
            {
                // 如果有日期条件，按日期排序
                query = query.OrderByDescending(p => p.CreatedAt);
            }
            else
            {
                // 默认按最后就诊时间排序
                query = query.OrderByDescending(p => p.LastVisitDate ?? p.CreatedAt);
            }
            
            return query;
        }

        /// <summary>
        /// 重写默认包含
        /// </summary>
        protected override IQueryable<Patient> ApplyDefaultIncludes(IQueryable<Patient> query)
        {
            // 默认不包含关联数据，需要时显式Include
            return query;
        }

        /// <summary>
        /// 重写全局过滤器
        /// </summary>
        protected override IQueryable<Patient> ApplyGlobalFilters(IQueryable<Patient> query)
        {
            // 过滤软删除的患者
            return query.Where(p => !p.IsDeleted);
        }

        #endregion
    }

    #region 支持类

    /// <summary>
    /// 患者搜索条件
    /// </summary>
    public class PatientSearchCriteria
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? IdCard { get; set; }
        public Gender? Gender { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int MaxResults { get; set; } = 100;
    }

    /// <summary>
    /// 患者统计信息
    /// </summary>
    public class PatientStatistics
    {
        public int TotalPatients { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int ActivePatients { get; set; }
        public Dictionary<string, int> AgeDistribution { get; set; } = new();
        public Dictionary<string, int> GenderDistribution { get; set; } = new();
    }

    /// <summary>
    /// 批量导入结果
    /// </summary>
    public class BatchImportResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<ImportResult> Details { get; set; } = new();
    }

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult
    {
        public Patient Patient { get; set; } = null!;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 性别枚举
    /// </summary>
    public enum Gender
    {
        Male = 1,
        Female = 2,
        Other = 3
    }

    #endregion
}