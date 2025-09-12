using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
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

        /// <summary>
        /// 分页查询验方
        /// </summary>
        /// <param name="query">查询条件，包含分页参数和筛选条件</param>
        /// <returns>包含验方分页结果的服务结果，失败时返回错误消息</returns>
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

        /// <summary>
        /// 搜索验方（分页）
        /// </summary>
        /// <param name="query">搜索查询条件，支持关键字搜索验方名称、功效、用法</param>
        /// <returns>包含搜索结果分页数据的服务结果，失败时返回错误消息</returns>
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
                        (f.Effect != null && f.Effect.Contains(query.Keyword)) ||
                        (f.Usage != null && f.Usage.Contains(query.Keyword)));
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

        #endregion 分页查询

        #region 分类和模板查询

        /// <summary>
        /// 获取验方分类列表
        /// </summary>
        /// <returns>包含所有验方分类的服务结果，包括经典验方、临床验方、个人验方</returns>
        public Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            try
            {
                // Formula实体没有Category属性，返回基本分类
                var categories = new List<string> { "经典验方", "临床验方", "个人验方" };
                return Task.FromResult(ServiceResult<List<string>>.Success(categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方分类失败");
                return Task.FromResult(ServiceResult<List<string>>.Failure($"获取分类失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 获取验方列表
        /// </summary>
        /// <param name="keyword">可选的搜索关键字，用于筛选验方名称</param>
        /// <returns>包含符合条件验方列表的服务结果，失败时返回错误消息</returns>
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

        /// <summary>
        /// 获取所有启用状态的验方
        /// </summary>
        /// <returns>包含所有启用验方列表的服务结果，按名称排序</returns>
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

        #endregion 分类和模板查询

        #region 基础模板功能（Record-Only保留）

        /// <summary>
        /// 获取验方模板列表 - Record-Only模式保留基础功能
        /// </summary>
        /// <returns>包含共享验方模板列表的服务结果，用于处方开具时的模板选择</returns>
        public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
        {
            try
            {
                // 获取模板验方（IsShared = true的验方作为模板）
                var templates = await _dbContext.Formulas
                    .Include(f => f.Herbs)
                    .Where(f => f.Status == CommonStatus.Enabled && f.IsShared)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(templates);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方模板失败");
                return ServiceResult<List<FormulaDto>>.Failure($"获取模板失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据验方类型查询验方 - Record-Only模式保留基础功能
        /// </summary>
        /// <param name="formulaType">验方类型关键字，不能为空</param>
        /// <returns>包含指定类型验方列表的服务结果，失败时返回错误消息</returns>
        public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(formulaType))
                {
                    return ServiceResult<List<FormulaDto>>.Failure("验方类型不能为空");
                }

                // 根据验方类型查询（基于名称或功效匹配）
                var formulas = await _dbContext.Formulas
                    .Include(f => f.Herbs)
                    .Where(f => f.Status == CommonStatus.Enabled &&
                               (f.Name.Contains(formulaType) ||
                                (f.Effect != null && f.Effect.Contains(formulaType))))
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型查询验方失败，类型: {Type}", formulaType);
                return ServiceResult<List<FormulaDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据关键字和分类查询验方 - Record-Only模式保留基础功能
        /// </summary>
        /// <param name="keyword">可选的搜索关键字，用于匹配验方名称或功效</param>
        /// <param name="category">可选的验方分类筛选条件</param>
        /// <returns>包含符合条件验方列表的服务结果，支持多条件组合筛选</returns>
        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword, string? category)
        {
            try
            {
                var queryable = BuildBaseQuery();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    queryable = queryable.Where(f => f.Name.Contains(keyword) ||
                                                    (f.Effect != null && f.Effect.Contains(keyword)));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    // 根据分类过滤（基于名称或效果匹配分类）
                    queryable = queryable.Where(f => f.Name.Contains(category) ||
                                                    (f.Effect != null && f.Effect.Contains(category)));
                }

                var formulas = await queryable
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询验方失败，关键字: {Keyword}, 分类: {Category}", keyword, category);
                return ServiceResult<List<FormulaDto>>.Failure($"查询失败: {ex.Message}");
            }
        }



        #endregion 基础模板功能（Record-Only保留）

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
                                       (f.Effect != null && f.Effect.Contains(queryDto.Keyword)));
            }

            if (queryDto.Status.HasValue)
            {
                query = query.Where(f => f.Status == queryDto.Status.Value);
            }

            return query;
        }

        /// <summary>
        /// 智能推荐计算方法已移除 - Record-Only模式下不再需要复杂的推荐算法
        /// </summary>
        [Obsolete("Smart recommendation calculation removed in Record-Only mode.", false)]
        private static double CalculateConfidence(LYBT.Entities.Formula.Formula formula, string syndrome)
        {
            // Record-Only模式下不再使用推荐算法
            return 0.0;
        }

        /// <summary>
        /// 智能推荐计算方法已移除 - Record-Only模式下不再需要复杂的推荐算法
        /// </summary>
        [Obsolete("Smart recommendation calculation removed in Record-Only mode.", false)]
        private static double CalculateMatchScore(LYBT.Entities.Formula.Formula formula, string symptoms, string diagnosis)
        {
            // Record-Only模式下不再使用推荐算法
            return 0.0;
        }

        #endregion 私有辅助方法

        #region 搜索接口 - 简化版接口兼容性

        /// <summary>
        /// 根据关键字搜索验方
        /// </summary>
        /// <param name="keyword">搜索关键字，用于匹配验方名称</param>
        /// <returns>包含搜索结果的服务结果，委托给GetFormulasAsync方法实现</returns>
        public async Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword)
        {
            return await GetFormulasAsync(keyword);
        }

        #endregion 搜索接口 - 简化版接口兼容性
    }
}
