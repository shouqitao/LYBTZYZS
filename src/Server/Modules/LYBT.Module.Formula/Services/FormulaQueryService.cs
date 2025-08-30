using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方查询服务 - 专注复杂查询和推荐逻辑 (UltraThink重构: <300行)
    /// 职责：分页查询、筛选、推荐、分类等查询相关功能
    /// </summary>
    public class FormulaQueryService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaQueryService> _logger;

        public FormulaQueryService(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaQueryService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        #region 分页查询

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
        {
            try
            {
                var queryable = BuildBaseQuery();
                
                // 应用筛选
                queryable = ApplyFilters(queryable, query);
                
                var totalCount = await queryable.CountAsync();
                
                // 应用分页和排序
                var items = await queryable
                    .OrderBy(f => f.Name)
                    .Skip(query.Skip)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(items);
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询验方失败");
                return ServiceResult<PagedResult<FormulaDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query)
        {
            try
            {
                var queryable = BuildBaseQuery();
                
                // 关键字搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    queryable = queryable.Where(f => 
                        f.Name.Contains(query.Keyword) ||
                        f.Effect.Contains(query.Keyword) ||
                        f.Usage.Contains(query.Keyword));
                }

                var totalCount = await queryable.CountAsync();
                
                var items = await queryable
                    .OrderBy(f => f.Name)
                    .Skip(query.Skip)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(items);
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索验方失败，关键字: {Keyword}", query.Keyword);
                return ServiceResult<PagedResult<FormulaDto>>.Failure($"搜索失败: {ex.Message}");
            }
        }

        #endregion

        #region 分类和模板查询

        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            try
            {
                // Formula实体没有Category属性，返回基本分类
                var categories = new List<string> { "经典验方", "临床验方", "个人验方" };
                return ServiceResult<List<string>>.Success(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方分类失败");
                return ServiceResult<List<string>>.Failure($"获取分类失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null)
        {
            try
            {
                var queryable = BuildBaseQuery();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    queryable = queryable.Where(f => f.Name.Contains(keyword));
                }

                var formulas = await queryable
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询验方失败，关键字: {Keyword}", keyword);
                return ServiceResult<List<FormulaDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        {
            try
            {
                var formulas = await BuildBaseQuery()
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有验方失败");
                return ServiceResult<List<FormulaDto>>.Failure($"获取验方失败: {ex.Message}");
            }
        }

        #endregion

        #region 智能推荐（简化版）

        public async Task<ServiceResult<List<object>>> GetRecommendationsAsync(string syndrome)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(syndrome))
                {
                    return ServiceResult<List<object>>.Failure("症候不能为空");
                }

                // 根据症候推荐验方
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled &&
                               (f.Effect.Contains(syndrome) || 
                                f.Usage.Contains(syndrome)))
                    .OrderBy(f => f.Name)
                    .Take(10)
                    .ToListAsync();

                var recommendations = formulas.Select(f => new
                {
                    FormulaId = f.Id,
                    FormulaName = f.Name,
                    Confidence = CalculateConfidence(f, syndrome),
                    Reason = $"适用于{syndrome}相关症候"
                }).ToList<object>();

                return ServiceResult<List<object>>.Success(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据症候推荐验方失败，症候: {Syndrome}", syndrome);
                return ServiceResult<List<object>>.Failure($"推荐失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有辅助方法

        private IQueryable<LYBT.Entities.Formula.Formula> BuildBaseQuery()
        {
            return _dbContext.Formulas
                .Include(f => f.Herbs)
                .Where(f => f.Status == CommonStatus.Enabled);
        }

        private IQueryable<LYBT.Entities.Formula.Formula> ApplyFilters(IQueryable<LYBT.Entities.Formula.Formula> query, FormulaQueryDto queryDto)
        {
            if (!string.IsNullOrWhiteSpace(queryDto.Keyword))
            {
                query = query.Where(f => f.Name.Contains(queryDto.Keyword) ||
                                       f.Effect.Contains(queryDto.Keyword));
            }

            if (queryDto.Status.HasValue)
            {
                query = query.Where(f => f.Status == queryDto.Status.Value);
            }

            return query;
        }

        private double CalculateConfidence(LYBT.Entities.Formula.Formula formula, string syndrome)
        {
            double confidence = 0.5; // 基础置信度

            if (formula.Effect?.Contains(syndrome) == true)
                confidence += 0.3;
            
            if (formula.Usage?.Contains(syndrome) == true)
                confidence += 0.2;

            return Math.Min(confidence, 1.0);
        }

        #endregion
    }
}