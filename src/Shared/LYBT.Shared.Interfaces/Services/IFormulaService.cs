using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 验方服务接口 - 统一定义
    /// </summary>
    public interface IFormulaService
    {
        Task<FormulaDto> GetByIdAsync(Guid id);
        Task<PagedResult<FormulaDto>> GetPagedAsync(FormulaQueryDto query);
        Task<FormulaDto> CreateAsync(FormulaCreateDto dto);
        Task<FormulaDto> UpdateAsync(Guid id, FormulaUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<FormulaDto>> GetTemplatesAsync();
        Task<List<FormulaDto>> GetByTypeAsync(string formulaType);
        Task<FormulaDto> CreateFromPrescriptionAsync(Guid prescriptionId, string name);
        Task<FormulaAnalysisResult> AnalyzeFormulaAsync(Guid formulaId);
        Task<List<FormulaRecommendationDto>> GetRecommendationsAsync(string syndrome);
    }
}