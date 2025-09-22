using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{

    /// <summary>
    /// 验方服务 - UltraThink双层架构纯委托模式
    /// </summary>
    public class FormulaService(
        IFormulaQueryService queryService,
        IFormulaBusinessService businessService) : IFormulaService
    {
        private readonly IFormulaQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IFormulaBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region Query Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
            => await _queryService.GetByIdAsync(id);

        /// <inheritdoc/>
        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query)
            => await _queryService.SearchFormulasAsync(query);

        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
            => await _queryService.GetFormulasAsync(keyword, category);

        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
            => await _queryService.GetAllFormulasAsync();

        /// <inheritdoc/>
        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
            => await _queryService.GetCategoriesAsync();

        /// <inheritdoc/>
        public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
            => await _queryService.GetTemplatesAsync();

        /// <inheritdoc/>
        public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
            => await _queryService.GetByTypeAsync(formulaType);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword)
        {
            // 委托给查询服务的搜索方法
            return await _queryService.SearchAsync(keyword);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var result = await _queryService.GetByIdAsync(id);
            return result.IsSuccess;
        }

        public async Task<bool> IsNameDuplicatedAsync(string name, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // 获取所有验方并检查名称重复
            var allFormulasResult = await _queryService.GetAllFormulasAsync();
            if (!allFormulasResult.IsSuccess)
                return false;

            return allFormulasResult.Data?.Any(f => 
                f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                (!excludeId.HasValue || f.Id != excludeId.Value)) ?? false;
        }

        #endregion Query Operations

        #region Business Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                return await _businessService.CreateAsync(dto);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                return await _businessService.UpdateAsync(id, dto);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                return await _businessService.DeleteAsync(id);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult> EnableAsync(Guid id)
            => await _businessService.EnableAsync(id);

        /// <inheritdoc/>
        public async Task<ServiceResult> DisableAsync(Guid id)
            => await _businessService.DisableAsync(id);

        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
            => await _businessService.ToggleStatusAsync(id);

        public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
            => await _businessService.CopyAsync(id, newName);

        /// <inheritdoc/>
        public async Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
            => await _businessService.CreateFromPrescriptionAsync(prescriptionId, name);

        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName)
            => await _businessService.ShareFormulaAsync(id, operatorId, operatorName);

        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName)
            => await _businessService.UnshareFormulaAsync(id, operatorId, operatorName);

        /// <inheritdoc/>
        public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId, string newName, Guid userId)
        {
            // 委托给BusinessService的复制功能
            return await _businessService.CopyAsync(formulaId, newName);
        }

        #endregion Business Operations

        #region 批量操作 - 必需功能（用户明确需求）

        /// <inheritdoc/>
        public Task<ServiceResult<object>> ImportFormulasAsync(List<FormulaCreateDto> formulas)
        {
            return Task.FromResult(ServiceResult<object>.Failure("简单诊所版本暂不支持验方批量导入功能"));
        }

        /// <inheritdoc/>
        public Task<ServiceResult<byte[]>> ExportFormulasAsync(PagedQueryBaseDto query)
        {
            return Task.FromResult(ServiceResult<byte[]>.Failure("简单诊所版本暂不支持验方批量导出功能"));
        }

        #endregion 批量操作 - 必需功能（用户明确需求）
    }
}
