using LYBT.Infrastructure.Data;
using LYBT.Models.Records;
using LYBT.Module.Records.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Records.Repositories {

    /// <summary>
    /// 病历仓储实现类，封装病历表的数据库操作
    /// </summary>
    public class RecordRepository : IRecordRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造函数，注入数据库上下文
        /// </summary>
        public RecordRepository(AppDbContext context) {
            _context = context;
        }

        /// <summary>
        /// 根据ID获取病历记录
        /// </summary>
        public async Task<RecordModel?> GetByIdAsync(Guid id) {
            return await _context.Records.FindAsync(id);
        }

        /// <summary>
        /// 获取所有病历记录
        /// </summary>
        public async Task<List<RecordModel>> GetListAsync() {
            return await _context.Records
                .OrderByDescending(r => r.RecordTime)
                .ToListAsync();
        }

        /// <summary>
        /// 分页查询病历列表
        /// </summary>
        public async Task<(List<RecordModel> list, int total)> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var queryable = _context.Records.AsQueryable();

            // 根据操作者角色决定数据访问权限
            // 普通用户可能只能查看自己的病历，这里先不做限制

            // 总数统计
            var total = await queryable.CountAsync();

            // 分页和排序
            var list = await queryable
                .OrderByDescending(r => r.RecordTime)
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (list, total);
        }

        /// <summary>
        /// 新增病历记录
        /// </summary>
        public async Task<bool> AddAsync(RecordModel recordModel) {
            _context.Records.Add(recordModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新病历记录
        /// </summary>
        public async Task<bool> UpdateAsync(RecordModel recordModel) {
            _context.Records.Update(recordModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除病历记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var recordModel = await _context.Records.FindAsync(id);
            if (recordModel == null)
                return false;
            _context.Records.Remove(recordModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行GetListByPatientIdAsync操作。
        /// </summary>
        /// <param name="patientId">参数patientId</param>
        /// <returns>返回值</returns>
        public async Task<List<RecordModel>> GetListByPatientIdAsync(Guid patientId) {
            return await _context.Records
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
            var list = await _context.Records
                .Where(r => r.IsShared)
                .ToListAsync();
            return list.Where(r => r.SharedToDoctorIds.Contains(doctorId.ToString())).ToList();
        }
    }
}