using LYBT.Infrastructure.Data;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.TreatmentRoom.Interfaces;

namespace LYBT.Module.TreatmentRoom.Repositories {

    /// <summary>
    /// 治疗室仓储实现类，封装数据库操作
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
        /// 根据ID获取治疗室记录
        /// </summary>
        public async Task<TreatmentRoomModel?> GetByIdAsync(Guid id) {
            return await _context.TreatmentRooms.FindAsync(id);
        }

        /// <summary>
        /// 获取所有治疗室记录
        /// </summary>
        public async Task<List<TreatmentRoomModel>> GetListAsync() {
            return await Task.FromResult(_context.TreatmentRooms.ToList());
        }

        /// <summary>
        /// 新增治疗室记录
        /// </summary>
        public async Task<bool> AddAsync(TreatmentRoomModel treatmentRoomModel) {
            _context.TreatmentRooms.Add(treatmentRoomModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新治疗室记录
        /// </summary>
        public async Task<bool> UpdateAsync(TreatmentRoomModel treatmentRoomModel) {
            _context.TreatmentRooms.Update(treatmentRoomModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除治疗室记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _context.TreatmentRooms.FindAsync(id);
            if (model == null)
                return false;
            _context.TreatmentRooms.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行GetByStatusAsync操作。
        /// </summary>
        /// <param name="status">参数status</param>
        /// <returns>返回值</returns>
        public async Task<List<TreatmentRoomModel>> GetByStatusAsync(string status) {
            var list = _context.TreatmentRooms
                .Where(t => t.Status == status)
                .ToList();
            return await Task.FromResult(list);
        }
    }
}