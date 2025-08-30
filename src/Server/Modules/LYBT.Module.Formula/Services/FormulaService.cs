using LYBT.Module.Formula.Services.Core;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方服务 - 扩展友好的组合式架构 (UltraThink最优设计: <150行)
    /// 职责：实现IFormulaService接口，协调各个专业服务
    /// 设计原则：委托模式，便于功能扩展和测试，遵循开放封闭原则
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly FormulaServiceCore _coreService;
        private readonly FormulaQueryService _queryService;
        private readonly FormulaBusinessService _businessService;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            FormulaServiceCore coreService,
            FormulaQueryService queryService,
            FormulaBusinessService businessService,
            ILogger<FormulaService> logger)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IFormulaService 基础CRUD实现 - 委托给CoreService

        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            return await _coreService.GetByIdAsync(id);
        }

        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            return await _coreService.CreateAsync(dto);
        }

        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            return await _coreService.UpdateAsync(id, dto);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await _coreService.DeleteAsync(id);
        }

        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            return await _coreService.EnableAsync(id);
        }

        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            return await _coreService.DisableAsync(id);
        }

        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            return await _coreService.ToggleStatusAsync(id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _coreService.ExistsAsync(id);
        }

        public async Task<bool> IsNameDuplicatedAsync(string name, Guid? excludeId = null)
        {
            return await _coreService.IsNameDuplicatedAsync(name, excludeId);
        }

        #endregion

        #region IFormulaService 查询功能实现 - 委托给QueryService

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
        {
            return await _queryService.GetPagedAsync(query);
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query)
        {
            return await _queryService.SearchFormulasAsync(query);
        }

        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null)
        {
            return await _queryService.GetFormulasAsync(keyword);
        }

        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        {
            return await _queryService.GetAllFormulasAsync();
        }

        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            return await _queryService.GetCategoriesAsync();
        }

        public async Task<ServiceResult<List<object>>> GetRecommendationsAsync(string syndrome)
        {
            return await _queryService.GetRecommendationsAsync(syndrome);
        }

        #endregion

        #region IFormulaService 业务功能实现 - 委托给BusinessService

        public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
        {
            return await _businessService.CopyAsync(id, newName);
        }

        public async Task<ServiceResult<object>> AnalyzeFormulaAsync(Guid formulaId)
        {
            return await _businessService.AnalyzeFormulaAsync(formulaId);
        }

        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id)
        {
            return await _businessService.ShareFormulaAsync(id);
        }

        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id)
        {
            return await _businessService.UnshareFormulaAsync(id);
        }

        #endregion
    }
}