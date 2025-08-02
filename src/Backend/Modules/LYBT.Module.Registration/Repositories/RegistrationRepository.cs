using LYBT.Infrastructure.Data;
using LYBT.Models.Registration;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Registration.Repositories {

    /// <summary>
    /// 挂号仓储实现类，实现挂号数据库操作
    /// </summary>
    public class RegistrationRepository : IRegistrationRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public RegistrationRepository(AppDbContext context) {
            _context = context;
        }

        /// <summary>
        /// 根据ID获取挂号详情
        /// </summary>
        public async Task<RegistrationModel?> GetByIdAsync(Guid id) {
            return await _context.Registrations.FindAsync(id);
        }

        /// <summary>
        /// 获取所有挂号列表
        /// </summary>
        public async Task<List<RegistrationModel>> GetListAsync() {
            return await _context.Registrations.ToListAsync();
        }

        /// <summary>
        /// 分页查询挂号列表
        /// </summary>
        public async Task<(List<RegistrationModel> list, int total)> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var queryable = _context.Registrations.AsQueryable();

            // 根据操作者角色决定数据访问权限
            if (operatorRole != UserRole.Admin) {
                // 普通用户只能查看未取消的挂号
                queryable = queryable.Where(r => r.Status != RegistrationStatus.Cancelled);
            }

            // 总数统计
            var total = await queryable.CountAsync();

            // 分页和排序
            var list = await queryable
                .OrderByDescending(r => r.RegistrationTime)
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (list, total);
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        public async Task<bool> AddAsync(RegistrationModel model) {
            _context.Registrations.Add(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新挂号
        /// </summary>
        public async Task<bool> UpdateAsync(RegistrationModel model) {
            _context.Registrations.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除挂号（物理删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _context.Registrations.FindAsync(id);
            if (model == null)
                return false;
            _context.Registrations.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 取消挂号，设置状态为已取消
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            var model = await _context.Registrations.FindAsync(id);
            if (model == null)
                return false;
            model.Status = RegistrationStatus.Cancelled;
            _context.Registrations.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}