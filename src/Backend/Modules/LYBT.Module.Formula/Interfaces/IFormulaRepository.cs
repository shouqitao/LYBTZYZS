using LYBT.Models.Formula;

namespace LYBT.Module.Formula.Interfaces
{
    public interface IFormulaRepository
    {
        Task<List<FormulaModel>> GetAllAsync();
        Task<FormulaModel?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(FormulaModel model);
        Task<bool> UpdateAsync(FormulaModel model);
        Task<bool> DeleteAsync(Guid id);
        Task<List<FormulaModel>> GetTemplatesAsync();
    }
}