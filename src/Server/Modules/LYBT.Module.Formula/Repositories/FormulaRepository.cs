using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Formula.Interfaces;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formula.Repositories
{
    /// <summary>
    /// 验方仓储实现 - 数据层统一化重构
    /// 继承BaseRepository获得通用CRUD功能，只实现验方特有业务方法
    /// </summary>
    public class FormulaRepository : BaseRepository<FormulaModel>, IFormulaRepository
    {
        public FormulaRepository(AppDbContext context) : base(context)
        {
        }

        // 注意：基础CRUD方法由BaseRepository提供
        // GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync等都由基类实现

        public async Task<List<FormulaModel>> GetTemplatesAsync()
        {
            return await _context.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .ToListAsync();
        }
    }
}