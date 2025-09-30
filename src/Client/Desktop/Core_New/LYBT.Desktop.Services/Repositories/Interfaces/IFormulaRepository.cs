using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Services.Repositories.Interfaces
{
    /// <summary>
    /// 配方数据仓储接口 - UltraThink架构
    /// </summary>
    public interface IFormulaRepository
    {
        Task<List<FormulaDto>> GetAllAsync();
        Task<FormulaDto> GetByIdAsync(Guid id);
        Task<FormulaDto> CreateAsync(FormulaDto formula);
        Task<FormulaDto> UpdateAsync(FormulaDto formula);
        Task<bool> DeleteAsync(Guid id);
        Task<List<FormulaDto>> SearchAsync(string keyword);
        Task<List<FormulaDto>> GetTemplatesAsync();
        Task<List<FormulaDto>> GetByCategoryAsync(string category);
        Task<FormulaDto> DuplicateFormulaAsync(Guid formulaId);
    }
}