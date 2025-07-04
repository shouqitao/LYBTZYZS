using LYBT.Infrastructure;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Doctors.Repositories {
    /// <summary>
    /// 医生信息申请仓储实现
    /// </summary>
    public class DoctorInfoRequestRepository : IDoctorInfoRequestRepository {
        private readonly AppDbContext _dbContext;
        public DoctorInfoRequestRepository(AppDbContext dbContext) {
            _dbContext = dbContext;
        }
        public async Task AddAsync(DoctorInfoRequestModel model) {
            _dbContext.DoctorInfoRequests.Add(model);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<DoctorInfoRequestModel?> GetByIdAsync(Guid id) {
            return await _dbContext.DoctorInfoRequests.FirstOrDefaultAsync(r => r.Id == id);
        }
        public async Task UpdateAsync(DoctorInfoRequestModel model) {
            _dbContext.DoctorInfoRequests.Update(model);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<List<DoctorInfoRequestModel>> GetPendingListAsync() {
            return await _dbContext.DoctorInfoRequests
                .Where(r => r.Status == DoctorInfoRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedTime)
                .ToListAsync();
        }
    }
}
