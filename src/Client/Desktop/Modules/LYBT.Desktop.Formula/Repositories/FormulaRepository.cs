using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Repositories
{
    /// <summary>
    /// 验方数据仓储实现 - ADR-002合规版本
    /// 直接调用IFormulaApi（Refit HTTP客户端），符合架构决策
    /// </summary>
    public class FormulaRepository : IFormulaRepository
    {
        private readonly IFormulaApi _formulaApi;
        private readonly ILogger<FormulaRepository> _logger;

        public FormulaRepository(
            IFormulaApi formulaApi,
            ILogger<FormulaRepository> logger)
        {
            _formulaApi = formulaApi;
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        public async Task<FormulaDto> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _formulaApi.GetFormulaByIdAsync(id);
                return response.Content ?? throw new InvalidOperationException($"验方 {id} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新验方（使用CreateDto）
        /// </summary>
        public async Task<FormulaDto> CreateAsync(FormulaCreateDto formula)
        {
            if (formula == null)
                throw new ArgumentNullException(nameof(formula));

            try
            {
                var response = await _formulaApi.CreateFormulaAsync(formula);
                return response.Content ?? throw new InvalidOperationException("创建验方失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败");
                throw;
            }
        }

        /// <summary>
        /// 更新验方信息（使用UpdateDto）
        /// </summary>
        public async Task<FormulaDto> UpdateAsync(FormulaUpdateDto formula)
        {
            if (formula?.Id == null || formula.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update formula with null or invalid id");
                throw new ArgumentException("Formula ID is required", nameof(formula));
            }

            try
            {
                var response = await _formulaApi.UpdateFormulaAsync(formula.Id, formula);
                return response.Content ?? throw new InvalidOperationException($"更新验方失败，ID: {formula.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败，ID: {Id}", formula.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _formulaApi.DeleteFormulaAsync(id);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败，ID: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 搜索验方（关键字查询）
        /// </summary>
        public async Task<List<FormulaDto>> SearchAsync(string keyword)
        {
            try
            {
                var response = await _formulaApi.GetFormulasAsync(page: 1, pageSize: 1000, keyword: keyword);
                return response.Content?.Items ?? new List<FormulaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索验方失败，关键字: {Keyword}", keyword);
                throw;
            }
        }

        /// <summary>
        /// 分页查询验方列表（服务端分页）
        /// </summary>
        public async Task<PagedResult<FormulaDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var response = await _formulaApi.GetFormulasAsync(page, pageSize, keyword);
                return response.Content ?? new PagedResult<FormulaDto>
                {
                    Items = new List<FormulaDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询验方失败，Page: {Page}, PageSize: {PageSize}, Keyword: {Keyword}",
                    page, pageSize, keyword);
                throw;
            }
        }

        /// <summary>
        /// 克隆验方
        /// </summary>
        public async Task<FormulaDto> CloneFormulaAsync(Guid formulaId)
        {
            try
            {
                var response = await _formulaApi.CloneFormulaAsync(formulaId);
                return response.Content ?? throw new InvalidOperationException($"克隆验方失败，ID: {formulaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆验方失败，ID: {Id}", formulaId);
                throw;
            }
        }
    }
}
