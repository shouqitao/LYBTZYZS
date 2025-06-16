using LYBT.Infrastructure;
using LYBT.Models;
using LYBT.Models.FormulaTemplates;
using LYBT.Module.FormulaTemplates.Interfaces;

namespace LYBT.Module.FormulaTemplates.Repositories {
    /// <summary>
    /// 经验方模板仓储实现类，数据库操作
    /// </summary>
    public class FormulaTemplateRepository : IFormulaTemplateRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public FormulaTemplateRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 获取模板详情
        /// </summary>
        public async Task<FormulaTemplateModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.FormulaTemplates.FindAsync(id);
        }

        /// <summary>
        /// 获取全部模板列表
        /// </summary>
        public async Task<List<FormulaTemplateModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.FormulaTemplates.ToList());
        }

        /// <summary>
        /// 新增模板
        /// </summary>
        public async Task<bool> AddAsync(FormulaTemplateModel model) {
            _appDbContext.FormulaTemplates.Add(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新模板
        /// </summary>
        public async Task<bool> UpdateAsync(FormulaTemplateModel model) {
            _appDbContext.FormulaTemplates.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.FormulaTemplates.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.FormulaTemplates.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
