using LYBT.Infrastructure;
using LYBT.Models;
using LYBT.Models.Registration;
using LYBT.Module.Registration.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Module.Registration.Repositories {
    /// <summary>
    /// 挂号仓储实现类，实现挂号数据库操作
    /// </summary>
    public class RegistrationRepository : IRegistrationRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public RegistrationRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取挂号详情
        /// </summary>
        public async Task<RegistrationModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Registrations.FindAsync(id);
        }

        /// <summary>
        /// 获取所有挂号列表
        /// </summary>
        public async Task<List<RegistrationModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Registrations.ToList());
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        public async Task<bool> AddAsync(RegistrationModel model) {
            _appDbContext.Registrations.Add(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新挂号
        /// </summary>
        public async Task<bool> UpdateAsync(RegistrationModel model) {
            _appDbContext.Registrations.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除挂号
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.Registrations.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.Registrations.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
