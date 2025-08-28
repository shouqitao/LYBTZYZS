using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace LYBT.Module.Formula.Helpers
{
    /// <summary>
    /// 验方查询辅助类
    /// 负责各种复杂查询、搜索、筛选和数据检索逻辑
    /// </summary>
    public class FormulaQueryHelper
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaQueryHelper> _logger;

        public FormulaQueryHelper(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaQueryHelper> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询验方
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
        {
            try
            {
                var formulas = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                // 应用筛选条件
                formulas = ApplyFilters(formulas, query);

                // 应用排序
                formulas = ApplySorting(formulas, query.OrderBy, !query.IsAscending);

                var total = await formulas.CountAsync();
                var items = await formulas
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(items);
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = total,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询验方失败");                return ServiceResult<PagedResult<FormulaDto>>.Failure("分页查询验方失败", ex);            }
        }

        /// <summary>
        /// 搜索验方
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query)
        {
            try
            {
                var formulas = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    var keyword = query.Keyword.Trim();
                    formulas = formulas.Where(f => 
                        f.Name.Contains(keyword) ||
                        (f.Effect != null && f.Effect.Contains(keyword)) ||
                        (f.Usage != null && f.Usage.Contains(keyword)) ||
                        (f.Property != null && f.Property.Contains(keyword)));
                }

                var total = await formulas.CountAsync();
                var items = await formulas
                    .OrderBy(f => f.Name)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(items);
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = total,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "搜索验方失败");                return ServiceResult<PagedResult<FormulaDto>>.Failure("搜索验方失败", ex);            }
        }

        /// <summary>
        /// 获取验方列表（支持筛选）
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
        {
            try
            {
                var query = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(f => f.Name.Contains(keyword));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    // 基于效果字段进行分类筛选
                    query = query.Where(f => f.Effect != null && f.Effect.Contains(category));
                }

                var formulas = await query.Take(50).OrderBy(f => f.Name).ToListAsync();
                var dtos = _mapper.Map<List<FormulaDto>>(formulas);

                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取验方列表失败");                return ServiceResult<List<FormulaDto>>.Failure("获取验方列表失败", ex);            }
        }

        /// <summary>
        /// 获取所有验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取所有验方失败");                return ServiceResult<List<FormulaDto>>.Failure("获取所有验方失败", ex);            }
        }

        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled && f.IsShared)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取验方模板列表失败");                return ServiceResult<List<FormulaDto>>.Failure("获取验方模板列表失败", ex);            }
        }

        /// <summary>
        /// 根据类型获取验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .Where(f => f.Effect != null && f.Effect.Contains(formulaType))
                    .Take(20)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据类型获取验方失败: {Type}", formulaType);                return ServiceResult<List<FormulaDto>>.Failure("根据类型获取验方失败", ex);            }
        }

        /// <summary>
        /// 获取分类列表
        /// </summary>
        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            try
            {
                // 从数据库中提取分类信息
                var effectCategories = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled && f.Effect != null)
                    .Select(f => f.Effect!)
                    .Distinct()
                    .ToListAsync();

                // 提取关键词作为分类
                var categories = new HashSet<string>();
                
                foreach (var effect in effectCategories)
                {                    if (effect.Contains("清热")) categories.Add("清热类");                    if (effect.Contains("补气") || effect.Contains("补血")) categories.Add("补益类");                    if (effect.Contains("活血")) categories.Add("活血类");                    if (effect.Contains("健脾") || effect.Contains("化湿")) categories.Add("健脾类");                    if (effect.Contains("安神")) categories.Add("安神类");                    if (effect.Contains("解表")) categories.Add("解表类");                }

                // 添加默认分类
                var defaultCategories = new List<string>
                {                    "经典验方",                    "自制验方",                     "常用验方",                    "特殊验方"                };

                categories.UnionWith(defaultCategories);

                return ServiceResult<List<string>>.Success(categories.OrderBy(c => c).ToList());
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取分类列表失败");                return ServiceResult<List<string>>.Failure("获取分类列表失败", ex);            }
        }

        /// <summary>
        /// 导出验方数据
        /// </summary>
        public async Task<ServiceResult<List<FormulaExportDto>>> ExportFormulasAsync(List<Guid> formulaIds)
        {
            try
            {                _logger.LogInformation("开始导出验方，数量: {Count}", formulaIds.Count);                var formulas = await _dbContext.Formulas
                    .Where(f => formulaIds.Contains(f.Id) && f.Status == CommonStatus.Enabled)
                    .Include(f => f.Herbs)
                    .ToListAsync();

                var exportDtos = formulas.Select(f => new FormulaExportDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Effect = f.Effect,
                    Usage = f.Usage,
                    Property = f.Property,
                    IsShared = f.IsShared,
                    Remark = f.Remark,
                    Status = f.Status,
                    Herbs = f.Herbs?.Select(fh => new FormulaHerbExportDto
                    {
                        HerbId = fh.HerbId,
                        HerbName = fh.HerbName,
                        Quantity = fh.Quantity,
                        Unit = fh.Unit,                        Preparation = "",                        Usage = fh.Usage,
                        Price = 0,
                        Subtotal = 0,
                        SortOrder = 0
                    }).ToList() ?? new List<FormulaHerbExportDto>(),
                    HerbCount = f.Herbs?.Count ?? 0,
                    TotalPrice = 0,
                    ExportTime = DateTime.Now
                }).ToList();

                return ServiceResult<List<FormulaExportDto>>.Success(exportDtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "导出验方数据异常");                return ServiceResult<List<FormulaExportDto>>.Failure($"导出验方数据异常: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 导出所有验方数据
        /// </summary>
        public async Task<ServiceResult<List<FormulaExportDto>>> ExportAllFormulasAsync(
            bool includePrivate = false, 
            string? category = null)
        {
            try
            {
                var query = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!includePrivate)
                {
                    query = query.Where(f => f.IsShared);
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(f => f.Effect != null && f.Effect.Contains(category));
                }

                var formulaIds = await query.Select(f => f.Id).ToListAsync();
                return await ExportFormulasAsync(formulaIds);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "导出所有验方数据异常");                return ServiceResult<List<FormulaExportDto>>.Failure($"导出所有验方数据异常: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 获取导入历史记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaImportResultDto>>> GetImportHistoryAsync(
            int pageIndex = 1,
            int pageSize = 20,
            string? importBatch = null)
        {
            try
            {
                // TODO: 实现导入历史记录存储和查询
                // 需要创建ImportHistory表来存储导入记录

                // 临时实现：返回空结果
                var result = new PagedResult<FormulaImportResultDto>
                {
                    Items = new List<FormulaImportResultDto>(),
                    TotalCount = 0,
                    CurrentPage = pageIndex,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<FormulaImportResultDto>>.Success(result);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取导入历史记录异常");                return ServiceResult<PagedResult<FormulaImportResultDto>>.Failure($"获取导入历史记录异常: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 高级搜索验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> AdvancedSearchAsync(
            string? name = null,
            string? effect = null,
            string? herbName = null,
            bool? isShared = null,
            int maxResults = 50)
        {
            try
            {
                var query = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

                if (!string.IsNullOrWhiteSpace(name))
                {
                    query = query.Where(f => f.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(effect))
                {
                    query = query.Where(f => f.Effect != null && f.Effect.Contains(effect));
                }

                if (isShared.HasValue)
                {
                    query = query.Where(f => f.IsShared == isShared.Value);
                }

                if (!string.IsNullOrWhiteSpace(herbName))
                {
                    query = query.Where(f => f.Herbs.Any(h => h.HerbName.Contains(herbName)));
                }

                var formulas = await query
                    .Take(maxResults)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "高级搜索验方失败");                return ServiceResult<List<FormulaDto>>.Failure("高级搜索验方失败", ex);            }
        }

        /// <summary>
        /// 获取热门验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetPopularFormulasAsync(int count = 10)
        {
            try
            {
                // 基于分享状态和简单的使用频次模拟
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled && f.IsShared)
                    .OrderBy(f => f.Name) // 临时排序，实际应基于使用频次
                    .Take(count)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取热门验方失败");                return ServiceResult<List<FormulaDto>>.Failure("获取热门验方失败", ex);            }
        }

        /// <summary>
        /// 获取最近添加的验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetRecentFormulasAsync(int count = 10)
        {
            try
            {
                var formulas = await _dbContext.Formulas
                    .Where(f => f.Status == CommonStatus.Enabled)
                    .OrderByDescending(f => f.Id) // 基于ID倒序，模拟创建时间排序
                    .Take(count)
                    .ToListAsync();

                var dtos = _mapper.Map<List<FormulaDto>>(formulas);
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取最近添加的验方失败");                return ServiceResult<List<FormulaDto>>.Failure("获取最近添加的验方失败", ex);            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private IQueryable<LYBT.Entities.Formula.Formula> ApplyFilters(
            IQueryable<LYBT.Entities.Formula.Formula> query, FormulaQueryDto filters)
        {
            if (!string.IsNullOrWhiteSpace(filters.Keyword))
            {
                var keyword = filters.Keyword.Trim();
                query = query.Where(f => 
                    f.Name.Contains(keyword) ||
                    (f.Effect != null && f.Effect.Contains(keyword)));
            }

            // TODO: Category属性暂时不可用，稍后添加
            // if (!string.IsNullOrWhiteSpace(filters.Category))
            // {
            //     query = query.Where(f => f.Effect != null && f.Effect.Contains(filters.Category));
            // }

            if (filters.IsShared.HasValue)
            {
                query = query.Where(f => f.IsShared == filters.IsShared.Value);
            }

            return query;
        }

        /// <summary>
        /// 应用排序
        /// </summary>
        private IQueryable<LYBT.Entities.Formula.Formula> ApplySorting(
            IQueryable<LYBT.Entities.Formula.Formula> query, 
            string? sortBy, 
            bool descending = false)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return query.OrderBy(f => f.Name);
            }

            return sortBy.ToLower() switch
            {                "name" => descending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),                "effect" => descending ? query.OrderByDescending(f => f.Effect) : query.OrderBy(f => f.Effect),                "isshared" => descending ? query.OrderByDescending(f => f.IsShared) : query.OrderBy(f => f.IsShared),
                _ => query.OrderBy(f => f.Name)
            };
        }

        #endregion
    }
}
