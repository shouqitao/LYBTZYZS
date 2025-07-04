using LYBT.Common.Enums;
using LYBT.Infrastructure;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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
            return await _appDbContext.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <summary>
        /// 获取所有医生
        /// </summary>
        public async Task<List<DoctorModel>> GetListAsync() {
            return await _appDbContext.Doctors
                .Include(d => d.User)
                .ToListAsync();
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        public async Task<bool> AddAsync(DoctorModel doctorModel) {
            _appDbContext.Doctors.Add(doctorModel);
            if (_appDbContext.Entry(doctorModel.User).State == EntityState.Detached) {
                _appDbContext.Users.Add(doctorModel.User);
            }
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新医生
        /// </summary>
        public async Task<bool> UpdateAsync(DoctorModel doctorModel) {
            _appDbContext.Doctors.Update(doctorModel);
            _appDbContext.Users.Update(doctorModel.User);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 禁用医生
        /// </summary>
        public async Task<bool> DisableAsync(Guid id) {
            var model = await _appDbContext.Doctors.FindAsync(id);
            if (model == null)
                return false;
            model.Status = DoctorStatus.Inactive;
            _appDbContext.Doctors.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 启用医生
        /// </summary>
        public async Task<bool> EnableAsync(Guid id) {
            var model = await _appDbContext.Doctors.FindAsync(id);
            if (model == null)
                return false;
            model.Status = DoctorStatus.Active;
            _appDbContext.Doctors.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var list = await _appDbContext.Doctors.Where(d => ids.Contains(d.Id)).ToListAsync();
            foreach (var d in list)
                d.Status = DoctorStatus.Inactive;
            _appDbContext.Doctors.UpdateRange(list);
            return await _appDbContext.SaveChangesAsync();
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            var list = await _appDbContext.Doctors.Where(d => ids.Contains(d.Id)).ToListAsync();
            foreach (var d in list)
                d.Status = DoctorStatus.Active;
            _appDbContext.Doctors.UpdateRange(list);
            return await _appDbContext.SaveChangesAsync();
        }

        public async Task<List<DoctorModel>> SearchAsync(string keyword) {
            var query = _appDbContext.Doctors
                .Include(d => d.User)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword)) {
                query = query.Where(d => d.User.RealName.Contains(keyword) || (d.User.PhoneNumber ?? "").Contains(keyword) || d.PinyinCode.Contains(keyword));
            }
            return await query.OrderByDescending(d => d.CreatedTime).Take(20).ToListAsync();
        }

        public async Task<(List<DoctorModel> list, int total)> GetPagedAsync(DoctorQueryDto query) {
            var dbSet = _appDbContext.Doctors
                .Include(d => d.User)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Keyword)) {
                dbSet = dbSet.Where(d => d.User.RealName.Contains(query.Keyword) || (d.User.PhoneNumber ?? "").Contains(query.Keyword) || d.PinyinCode.Contains(query.Keyword));
            }
            if (query.IsActive.HasValue) {
                var status = query.IsActive.Value ? DoctorStatus.Active : DoctorStatus.Inactive;
                dbSet = dbSet.Where(d => d.Status == status);
            }
            int total = await dbSet.CountAsync();
            var list = await dbSet.OrderByDescending(d => d.CreatedTime)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
            return (list, total);
        }

        public async Task<bool> UpdatePasswordAsync(Guid id, string passwordHash) {
            var model = await _appDbContext.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (model == null)
                return false;
            model.User.PasswordHash = passwordHash;
            _appDbContext.Users.Update(model.User);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}