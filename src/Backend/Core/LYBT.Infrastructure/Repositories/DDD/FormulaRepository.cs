using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Aggregates.FormulaAggregate;
using LYBT.Domain.ValueObjects;

namespace LYBT.Infrastructure.Repositories.DDD
{
    /// <summary>
    /// 验方聚合根Repository实现 - DDD模式
    /// </summary>
    public class FormulaRepository : DomainRepositoryBase<Formula>, IFormulaRepository
    {
        public FormulaRepository(AppDbContext context, ILogger<FormulaRepository> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据创建者ID获取验方
        /// </summary>
        public async Task<List<Formula>> GetByCreatorIdAsync(Guid creatorId)
        {
            Logger.LogDebug("Getting formulas by creator ID: {CreatorId}", creatorId);
            
            return await QueryAsNoTracking()
                .Where(f => f.CreatorId == creatorId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取公开的验方
        /// </summary>
        public async Task<List<Formula>> GetPublicFormulasAsync()
        {
            Logger.LogDebug("Getting public formulas");
            
            return await QueryAsNoTracking()
                .Where(f => f.IsPublic && f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据目标证候获取验方
        /// </summary>
        public async Task<List<Formula>> GetByTargetSyndromeAsync(TCMSyndrome syndrome)
        {
            Logger.LogDebug("Getting formulas by target syndrome: {Syndrome}", syndrome);
            
            return await QueryAsNoTracking()
                .Where(f => f.TargetSyndrome == syndrome && f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取已审批的验方
        /// </summary>
        public async Task<List<Formula>> GetApprovedFormulasAsync()
        {
            Logger.LogDebug("Getting approved formulas");
            
            return await QueryAsNoTracking()
                .Where(f => f.Approval.IsApproved && f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据验方名称查找
        /// </summary>
        public async Task<List<Formula>> GetByNameAsync(string name)
        {
            Logger.LogDebug("Getting formulas by name: {Name}", name);
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<Formula>();
            }

            return await QueryAsNoTracking()
                .Where(f => f.Name.Contains(name) || 
                           f.FormulaInfo.ChineseName.Contains(name) ||
                           f.FormulaInfo.EnglishName.Contains(name) ||
                           f.FormulaInfo.PinYinCode.Contains(name.ToUpper()) ||
                           f.FormulaInfo.WuBiCode.Contains(name.ToUpper()))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据分类获取验方
        /// </summary>
        public async Task<List<Formula>> GetByClassificationAsync(string classification)
        {
            Logger.LogDebug("Getting formulas by classification: {Classification}", classification);
            
            if (string.IsNullOrWhiteSpace(classification))
            {
                return new List<Formula>();
            }

            return await QueryAsNoTracking()
                .Where(f => f.FormulaInfo.Classification.Contains(classification))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据主要功效查找验方
        /// </summary>
        public async Task<List<Formula>> GetByMainEffectsAsync(string effects)
        {
            Logger.LogDebug("Getting formulas by main effects: {Effects}", effects);
            
            if (string.IsNullOrWhiteSpace(effects))
            {
                return new List<Formula>();
            }

            return await QueryAsNoTracking()
                .Where(f => f.Efficacy.MainEffects.Contains(effects))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据适应症查找验方
        /// </summary>
        public async Task<List<Formula>> GetByIndicationsAsync(string indications)
        {
            Logger.LogDebug("Getting formulas by indications: {Indications}", indications);
            
            if (string.IsNullOrWhiteSpace(indications))
            {
                return new List<Formula>();
            }

            return await QueryAsNoTracking()
                .Where(f => f.Efficacy.Indications.Contains(indications))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据来源查找验方
        /// </summary>
        public async Task<List<Formula>> GetBySourceAsync(string source)
        {
            Logger.LogDebug("Getting formulas by source: {Source}", source);
            
            if (string.IsNullOrWhiteSpace(source))
            {
                return new List<Formula>();
            }

            return await QueryAsNoTracking()
                .Where(f => f.Source.Contains(source))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据包含的药材查找验方
        /// </summary>
        public async Task<List<Formula>> GetByContainingHerbAsync(Guid herbId)
        {
            Logger.LogDebug("Getting formulas containing herb ID: {HerbId}", herbId);
            
            return await QueryAsNoTracking()
                .Where(f => f.Herbs.Any(h => h.HerbId == herbId))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据包含的药材名称查找验方
        /// </summary>
        public async Task<List<Formula>> GetByContainingHerbNameAsync(string herbName)
        {
            Logger.LogDebug("Getting formulas containing herb name: {HerbName}", herbName);
            
            if (string.IsNullOrWhiteSpace(herbName))
            {
                return new List<Formula>();
            }

            return await QueryAsNoTracking()
                .Where(f => f.Herbs.Any(h => h.HerbName.Contains(herbName)))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取待审批的验方
        /// </summary>
        public async Task<List<Formula>> GetPendingApprovalFormulasAsync()
        {
            Logger.LogDebug("Getting pending approval formulas");
            
            return await QueryAsNoTracking()
                .Where(f => !f.Approval.IsApproved && f.IsActive)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据审批者名称获取审批的验方
        /// </summary>
        public async Task<List<Formula>> GetByReviewerIdAsync(string reviewerName)
        {
            Logger.LogDebug("Getting formulas by reviewer name: {ReviewerName}", reviewerName);
            
            if (string.IsNullOrWhiteSpace(reviewerName))
            {
                return new List<Formula>();
            }
            
            return await QueryAsNoTracking()
                .Where(f => f.Approval.ReviewerId == reviewerName)
                .OrderByDescending(f => f.Approval.ReviewTime)
                .ToListAsync();
        }

        /// <summary>
        /// 检查验方名称是否已存在
        /// </summary>
        public async Task<bool> IsNameExistsAsync(string name, Guid? excludeFormulaId = null)
        {
            Logger.LogDebug("Checking if formula name exists: {Name}, excluding: {ExcludeId}", 
                name, excludeFormulaId);
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var query = QueryAsNoTracking().Where(f => f.Name == name);
            
            if (excludeFormulaId.HasValue)
            {
                query = query.Where(f => f.Id != excludeFormulaId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 搜索验方（综合搜索）
        /// </summary>
        public async Task<List<Formula>> SearchFormulasAsync(string searchTerm)
        {
            Logger.LogDebug("Searching formulas with term: {SearchTerm}", searchTerm);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetPublicFormulasAsync();
            }

            var term = searchTerm.Trim().ToUpper();
            
            return await QueryAsNoTracking()
                .Where(f => f.IsActive && (
                    f.Name.Contains(searchTerm) ||
                    f.FormulaInfo.ChineseName.Contains(searchTerm) ||
                    f.FormulaInfo.EnglishName.Contains(searchTerm) ||
                    f.FormulaInfo.PinYinCode.Contains(term) ||
                    f.FormulaInfo.WuBiCode.Contains(term) ||
                    f.Efficacy.MainEffects.Contains(searchTerm) ||
                    f.Efficacy.Indications.Contains(searchTerm) ||
                    f.Source.Contains(searchTerm) ||
                    f.Herbs.Any(h => h.HerbName.Contains(searchTerm))
                ))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取验方统计信息
        /// </summary>
        public async Task<FormulaStatistics> GetStatisticsAsync()
        {
            Logger.LogDebug("Getting formula statistics");
            
            var totalCount = await CountAsync();
            var activeCount = await CountAsync(f => f.IsActive);
            var publicCount = await CountAsync(f => f.IsPublic && f.IsActive);
            var approvedCount = await CountAsync(f => f.Approval.IsApproved && f.IsActive);
            var pendingApprovalCount = await CountAsync(f => !f.Approval.IsApproved && f.IsActive);
            
            var classificationStats = await QueryAsNoTracking()
                .Where(f => f.IsActive)
                .GroupBy(f => f.FormulaInfo.Classification)
                .Select(g => new { Classification = g.Key, Count = g.Count() })
                .ToListAsync();
            
            return new FormulaStatistics
            {
                TotalFormulas = totalCount,
                ActiveFormulas = activeCount,
                PublicFormulas = publicCount,
                ApprovedFormulas = approvedCount,
                PendingApprovalFormulas = pendingApprovalCount,
                ClassificationStatistics = classificationStats.ToDictionary(x => x.Classification, x => x.Count)
            };
        }

        /// <summary>
        /// 获取热门验方（根据使用频率）
        /// </summary>
        public async Task<List<Formula>> GetPopularFormulasAsync(int topCount = 20)
        {
            Logger.LogDebug("Getting popular formulas, top: {TopCount}", topCount);
            
            // 这里可以根据处方引用频率来排序，暂时按照名称排序
            return await QueryAsNoTracking()
                .Where(f => f.IsActive && f.IsPublic && f.Approval.IsApproved)
                .OrderBy(f => f.Name)
                .Take(topCount)
                .ToListAsync();
        }

        /// <summary>
        /// 包含导航属性的查询重写
        /// </summary>
        protected override IQueryable<Formula> IncludeNavigationProperties(IQueryable<Formula> query)
        {
            // 验方聚合根已经通过EF Core的OwnsOne和OwnsMany配置包含了所有值对象和子实体
            // 无需额外的Include，EF Core会自动加载拥有的实体
            return query;
        }
    }

    /// <summary>
    /// 验方统计信息
    /// </summary>
    public class FormulaStatistics
    {
        public int TotalFormulas { get; set; }
        public int ActiveFormulas { get; set; }
        public int PublicFormulas { get; set; }
        public int ApprovedFormulas { get; set; }
        public int PendingApprovalFormulas { get; set; }
        public Dictionary<string, int> ClassificationStatistics { get; set; } = new();
    }
}