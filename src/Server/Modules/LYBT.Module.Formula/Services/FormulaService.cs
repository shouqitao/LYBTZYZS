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
            => await _coreService.GetByIdAsync(id);

        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
            => await _coreService.CreateAsync(dto);

        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
            => await _coreService.UpdateAsync(id, dto);

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _coreService.DeleteAsync(id);

        public async Task<ServiceResult> EnableAsync(Guid id)
            => await _coreService.EnableAsync(id);

        public async Task<ServiceResult> DisableAsync(Guid id)
            => await _coreService.DisableAsync(id);

        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
            => await _coreService.ToggleStatusAsync(id);

        public async Task<bool> ExistsAsync(Guid id)
            => await _coreService.ExistsAsync(id);

        public async Task<bool> IsNameDuplicatedAsync(string name, Guid? excludeId = null)
            => await _coreService.IsNameDuplicatedAsync(name, excludeId);

        #endregion

        #region IFormulaService 查询功能实现 - 委托给QueryService

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query)
            => await _queryService.SearchFormulasAsync(query);

        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
            => await _queryService.GetFormulasAsync(keyword, category);

        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
            => await _queryService.GetAllFormulasAsync();

        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
            => await _queryService.GetCategoriesAsync();

        public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
            => await _queryService.GetTemplatesAsync();

        public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
            => await _queryService.GetByTypeAsync(formulaType);

        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome)
            => await _queryService.GetRecommendationsForSyndromeAsync(syndrome);

        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid doctorId)
            => await _queryService.GetRecommendationsAsync(symptoms, diagnosis, doctorId);

        #endregion

        #region IFormulaService 业务功能实现 - 委托给BusinessService

        public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
            => await _businessService.CopyAsync(id, newName);

        public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
            => await _businessService.AnalyzeFormulaAsync(formulaId);

        public async Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
            => await _businessService.CreateFromPrescriptionAsync(prescriptionId, name);

        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName)
            => await _businessService.ShareFormulaAsync(id, operatorId, operatorName);

        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName)
            => await _businessService.UnshareFormulaAsync(id, operatorId, operatorName);

        #endregion
    }
}