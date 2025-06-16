using LYBT.Infrastructure;
using LYBT.Models;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.TreatmentRoom.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Module.TreatmentRoom.Repositories {
    /// <summary>
    /// 治疗室仓储实现类，封装数据库操作
    /// </summary>
    public class TreatmentRoomRepository : ITreatmentRoomRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public TreatmentRoomRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取治疗室记录
        /// </summary>
        public async Task<TreatmentRoomModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.TreatmentRooms.FindAsync(id);
        }

        /// <summary>
        /// 获取所有治疗室记录
        /// </summary>
        public async Task<List<TreatmentRoomModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.TreatmentRooms.ToList());
        }

        /// <summary>
        /// 新增治疗室记录
        /// </summary>
        public async Task<bool> AddAsync(TreatmentRoomModel treatmentRoomModel) {
            _appDbContext.TreatmentRooms.Add(treatmentRoomModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新治疗室记录
        /// </summary>
        public async Task<bool> UpdateAsync(TreatmentRoomModel treatmentRoomModel) {
            _appDbContext.TreatmentRooms.Update(treatmentRoomModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除治疗室记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.TreatmentRooms.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.TreatmentRooms.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
