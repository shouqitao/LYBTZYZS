using LYBT.Infrastructure;
using LYBT.Module.Records.Interfaces;
using LYBT.Module.Records.Models;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Records.Repositories {

    /// <summary>
    /// 病历仓储实现类，封装病历表的数据库操作
    /// </summary>
    public class RecordRepository : IRecordRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造函数，注入数据库上下文
        /// </summary>
        public RecordRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取病历记录
        /// </summary>
        public async Task<RecordModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Records.FindAsync(id);
        }

        /// <summary>
        /// 获取所有病历记录
        /// </summary>
        public async Task<List<RecordModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Records.ToList());
        }

        /// <summary>
        /// 新增病历记录
        /// </summary>
        public async Task<bool> AddAsync(RecordModel recordModel) {
            _appDbContext.Records.Add(recordModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新病历记录
        /// </summary>
        public async Task<bool> UpdateAsync(RecordModel recordModel) {
            _appDbContext.Records.Update(recordModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除病历记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var recordModel = await _appDbContext.Records.FindAsync(id);
            if (recordModel == null)
                return false;
            _appDbContext.Records.Remove(recordModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行GetListByPatientIdAsync操作。
        /// </summary>
        /// <param name="patientId">参数patientId</param>
        /// <returns>返回值</returns>
        public async Task<List<RecordModel>> GetListByPatientIdAsync(Guid patientId) {
            return await _appDbContext.Records
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.RecordTime)
                .ToListAsync();
        }

        /// <summary>
        /// 执行GetSharedRecordsAsync操作。
        /// </summary>
        /// <param name="doctorId">参数doctorId</param>
        /// <returns>返回值</returns>
        public async Task<List<RecordModel>> GetSharedRecordsAsync(Guid doctorId) {
            var list = await _appDbContext.Records
                .Where(r => r.IsShared)
                .ToListAsync();
            return list.Where(r => r.SharedToDoctorIds.Contains(doctorId.ToString())).ToList();
        }
    }
}