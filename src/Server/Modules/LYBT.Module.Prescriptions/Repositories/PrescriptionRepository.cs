using LYBT.Infrastructure.Data;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Prescriptions.Repositories
{

    /// <summary>
    /// 表示PrescriptionRepository。
    /// </summary>
    public class PrescriptionRepository : IPrescriptionRepository
    {
        private readonly AppDbContext _context;

        public PrescriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 执行GetByIdAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>        /// <returns>返回值</returns>
        public async Task<Prescription?> GetByIdAsync(Guid id)
        {
            return await _context.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// 执行GetListAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<Prescription>> GetListAsync()
        {
            return await _context.Prescriptions
                .Include(p => p.Items)
                .ToListAsync();
        }

        /// <summary>
        /// 执行AddAsync操作。
        /// </summary>        /// <param name="model">参数model</param>        /// <returns>返回值</returns>
        public async Task<bool> AddAsync(Prescription model)
        {
            _context.Prescriptions.Add(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行UpdateAsync操作。
        /// </summary>        /// <param name="model">参数model</param>        /// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(Prescription model)
        {
            _context.Prescriptions.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行DeleteAsync操作。
        /// </summary>        /// <param name="id">参数id</param>        /// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var m = await _context.Prescriptions.FindAsync(id);
            if (m == null)
                return false;
            _context.Prescriptions.Remove(m);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行CancelAsync操作。
        /// </summary>        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<bool> CancelAsync(Guid id)
        {
            var model = await _context.Prescriptions.FindAsync(id);
            if (model == null)
                return false;
            model.Status = PrescriptionStatus.Draft;
            _context.Prescriptions.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
