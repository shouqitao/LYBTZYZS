using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using LYBT.Module.Formula.Interfaces;
using LYBT.Models.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formula.Repositories
{
    public class FormulaRepository : IFormulaRepository
    {
        private readonly AppDbContext _context;

        public FormulaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FormulaModel>> GetAllAsync()
        {
            return await _context.Formulas.ToListAsync();
        }

        public async Task<FormulaModel?> GetByIdAsync(Guid id)
        {
            return await _context.Formulas
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<bool> AddAsync(FormulaModel model)
        {
            _context.Formulas.Add(model);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(FormulaModel model)
        {
            _context.Formulas.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var formula = await GetByIdAsync(id);
            if (formula == null) return false;
            
            _context.Formulas.Remove(formula);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<FormulaModel>> GetTemplatesAsync()
        {
            return await _context.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .ToListAsync();
        }
    }
}