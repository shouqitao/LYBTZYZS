using LYBT.Common.Enums.Registration;
using LYBT.Models.Registration;
using LYBT.Module.Registration.Data;
using LYBT.Module.Registration.Interfaces;

namespace LYBT.Module.Registration.Repositories {

    /// <summary>
    /// 挂号仓储实现类，实现挂号数据库操作
    /// </summary>
    public class RegistrationRepository : IRegistrationRepository {
        private readonly RegistrationDbContext _registrationDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public RegistrationRepository(RegistrationDbContext registrationDbContext) {
            _registrationDbContext = registrationDbContext;
        }

        /// <summary>
        /// 根据ID获取挂号详情
        /// </summary>
        public async Task<RegistrationModel?> GetByIdAsync(Guid id) {
            return await _registrationDbContext.Registrations.FindAsync(id);
        }

        /// <summary>
        /// 获取所有挂号列表
        /// </summary>
        public async Task<List<RegistrationModel>> GetListAsync() {
            return await Task.FromResult(_registrationDbContext.Registrations.ToList());
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        public async Task<bool> AddAsync(RegistrationModel model) {
            _registrationDbContext.Registrations.Add(model);
            return await _registrationDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新挂号
        /// </summary>
        public async Task<bool> UpdateAsync(RegistrationModel model) {
            _registrationDbContext.Registrations.Update(model);
            return await _registrationDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除挂号（物理删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _registrationDbContext.Registrations.FindAsync(id);
            if (model == null)
                return false;
            _registrationDbContext.Registrations.Remove(model);
            return await _registrationDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 取消挂号，设置状态为已取消
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            var model = await _registrationDbContext.Registrations.FindAsync(id);
            if (model == null)
                return false;
            model.Status = RegistrationStatus.Cancelled;
            _registrationDbContext.Registrations.Update(model);
            return await _registrationDbContext.SaveChangesAsync() > 0;
        }
    }
}