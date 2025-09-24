using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{

    /// <summary>
    /// 验方查询服务 - UltraThink重构版 (<300行)
    /// 职责：分页查询、筛选、推荐、分类等查询相关功能
    /// 改为使用ReadRepository，移除直接的DbContext依赖
    /// </summary>
    public class FormulaQueryService : IFormulaQueryService
    {
        private readonly IFormulaReadRepository _readRepository;
        private readonly ILogger<FormulaQueryService> _logger;

        public FormulaQueryService(
            IFormulaReadRepository readRepository,
            ILogger<FormulaQueryService> logger)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var result = await _readRepository.GetPagedFormulaDtosAsync(query);
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
                var result = await _readRepository.SearchFormulaDtosAsync(query);
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
        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _readRepository.GetCategoriesAsync();
                return ServiceResult<List<string>>.Success(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方分类失败");
                return ServiceResult<List<string>>.Failure($"获取分类失败: {ex.Message}");
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
                var formulas = await _readRepository.GetFormulaDtosAsync(keyword);
                return ServiceResult<List<FormulaDto>>.Success(formulas);
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
                var formulas = await _readRepository.GetAllFormulaDtosAsync();
                return ServiceResult<List<FormulaDto>>.Success(formulas);
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
                var templates = await _readRepository.GetTemplateDtosAsync();
                return ServiceResult<List<FormulaDto>>.Success(templates);
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

                var formulas = await _readRepository.GetFormulaDtosByTypeAsync(formulaType);
                return ServiceResult<List<FormulaDto>>.Success(formulas);
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
                var formulas = await _readRepository.GetFormulaDtosAsync(keyword, category);
                return ServiceResult<List<FormulaDto>>.Success(formulas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询验方失败，关键字: {Keyword}, 分类: {Category}", keyword, category);
                return ServiceResult<List<FormulaDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        #endregion 基础模板功能（Record-Only保留）

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

        #region 单个验方查询

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        /// <param name="id">验方ID，不能为空</param>
        /// <returns>包含验方详情的服务结果，失败时返回错误消息</returns>
        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<FormulaDto>.Failure("验方ID不能为空");
                }

                var formula = await _readRepository.GetFormulaDtoByIdAsync(id);
                if (formula == null)
                {
                    return ServiceResult<FormulaDto>.Failure("验方不存在或已禁用");
                }

                return ServiceResult<FormulaDto>.Success(formula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败，ID: {FormulaId}", id);
                return ServiceResult<FormulaDto>.Failure($"获取验方详情失败: {ex.Message}");
            }
        }

        #endregion 单个验方查询
    }
}
