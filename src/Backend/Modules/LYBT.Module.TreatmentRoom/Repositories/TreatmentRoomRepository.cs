using LYBT.Infrastructure.Data;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.TreatmentRoom.Interfaces;

namespace LYBT.Module.TreatmentRoom.Repositories {

    /// <summary>
    /// 治疗任务仓储实现类，封装数据库操作
    /// </summary>
    public class TreatmentRoomRepository : ITreatmentRoomRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public TreatmentRoomRepository(AppDbContext context) {
            _context = context;
        }

        /// <summary>
        /// 根据ID获取治疗任务记录
        /// </summary>
        public async Task<TreatmentTaskModel?> GetByIdAsync(Guid id) {
            return await _context.TreatmentTasks.FindAsync(id);
        }

        /// <summary>
        /// 获取所有治疗任务记录
        /// </summary>
        public async Task<List<TreatmentTaskModel>> GetListAsync() {
            return await Task.FromResult(_context.TreatmentTasks.ToList());
        }

        /// <summary>
        /// 新增治疗任务记录
        /// </summary>
        public async Task<bool> AddAsync(TreatmentTaskModel treatmentTaskModel) {
            _context.TreatmentTasks.Add(treatmentTaskModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新治疗任务记录
        /// </summary>
        public async Task<bool> UpdateAsync(TreatmentTaskModel treatmentTaskModel) {
            _context.TreatmentTasks.Update(treatmentTaskModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除治疗任务记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _context.TreatmentTasks.FindAsync(id);
            if (model == null)
                return false;
            _context.TreatmentTasks.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 根据状态获取治疗任务记录
        /// </summary>
        /// <param name="status">参数status</param>
        /// <returns>返回值</returns>
        public async Task<List<TreatmentTaskModel>> GetByStatusAsync(string status) {
            var list = _context.TreatmentTasks
                .Where(t => t.Status == status)
                .ToList();
            return await Task.FromResult(list);
        }
    }
}