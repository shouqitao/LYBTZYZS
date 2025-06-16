using LYBT.Infrastructure;
using LYBT.Models;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Module.Doctors.Repositories {
    /// <summary>
    /// 医生仓储实现类，实现医生数据库操作
    /// </summary>
    public class DoctorRepository : IDoctorRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public DoctorRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 获取医生详情
        /// </summary>
        public async Task<DoctorModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Doctors.FindAsync(id);
        }

        /// <summary>
        /// 获取所有医生
        /// </summary>
        public async Task<List<DoctorModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Doctors.ToList());
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        public async Task<bool> AddAsync(DoctorModel doctorModel) {
            _appDbContext.Doctors.Add(doctorModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新医生
        /// </summary>
        public async Task<bool> UpdateAsync(DoctorModel doctorModel) {
            _appDbContext.Doctors.Update(doctorModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除医生
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.Doctors.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.Doctors.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
