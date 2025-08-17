using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 验方服务接口 - UltraThink统一标准
    /// </summary>
    public interface IFormulaService
    {
        Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query);
        Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);
        Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();
        Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType);
        Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name);
        Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId);
        Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome);
        Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid doctorId);
        
        // 新增Client层期望的方法签名
        Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null);
        Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync();
        Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName);
        Task<ServiceResult<bool>> ToggleStatusAsync(Guid id);
        Task<ServiceResult<List<string>>> GetCategoriesAsync();
        
        // UltraThink P0修复：添加Client层期望的SearchFormulasAsync方法
        Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query);
        
        // 验方分享功能
        Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName);
        Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName);
    }
}