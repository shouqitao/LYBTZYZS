using FormulaEntity = LYBT.Entities.Formula.Formula;
using LYBT.Entities.Formula;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Formula.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Formula.Repositories
{
    /// <summary>
    /// 验方仓储 - 简化版，只包含基础CRUD
    /// </summary>
    public class FormulaRepository : BaseRepository<FormulaEntity>, IFormulaRepository
    {
        public FormulaRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 获取启用的验方模板
        /// </summary>
        public async Task<List<FormulaEntity>> GetTemplatesAsync()
        {
            return await _dbSet
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
    }
}