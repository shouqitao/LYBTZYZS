using LYBT.Models.DiagnosisTreatment;
using LYBT.Infrastructure.Data;
using LYBT.Module.DiagnosisTreatment.Interfaces;

namespace LYBT.Module.DiagnosisTreatment.Repositories {

    /// <summary>
    /// 诊疗仓储实现类，实现诊疗相关数据库操作
    /// </summary>
    public class DiagnosisTreatmentRepository : IDiagnosisTreatmentRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public DiagnosisTreatmentRepository(AppDbContext context) {
            _context = context;
        }

        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        public async Task<DiagnosisTreatmentModel?> GetByIdAsync(Guid id) {
            // 查找单个诊疗记录
            return await _context.DiagnosisTreatments.FindAsync(id);
        }

        /// <summary>
        /// 获取所有诊疗记录列表
        /// </summary>
        public async Task<List<DiagnosisTreatmentModel>> GetListAsync() {
            // 获取所有诊疗记录
            return await Task.FromResult(_context.DiagnosisTreatments.ToList());
        }

        /// <summary>
        /// 新增诊疗记录
        /// </summary>
        public async Task<bool> AddAsync(DiagnosisTreatmentModel model) {
            _context.DiagnosisTreatments.Add(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新诊疗记录
        /// </summary>
        public async Task<bool> UpdateAsync(DiagnosisTreatmentModel model) {
            _context.DiagnosisTreatments.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除诊疗记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _context.DiagnosisTreatments.FindAsync(id);
            if (model == null)
                return false;
            _context.DiagnosisTreatments.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}