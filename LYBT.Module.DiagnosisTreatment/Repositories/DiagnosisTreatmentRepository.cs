using LYBT.Infrastructure;
using LYBT.Models;
using LYBT.Models.DiagnosisTreatment;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Module.DiagnosisTreatment.Repositories {
    /// <summary>
    /// 诊疗仓储实现类，实现诊疗相关数据库操作
    /// </summary>
    public class DiagnosisTreatmentRepository : IDiagnosisTreatmentRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public DiagnosisTreatmentRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        public async Task<DiagnosisTreatmentModel?> GetByIdAsync(Guid id) {
            // 查找单个诊疗记录
            return await _appDbContext.DiagnosisTreatments.FindAsync(id);
        }

        /// <summary>
        /// 获取所有诊疗记录列表
        /// </summary>
        public async Task<List<DiagnosisTreatmentModel>> GetListAsync() {
            // 获取所有诊疗记录
            return await Task.FromResult(_appDbContext.DiagnosisTreatments.ToList());
        }

        /// <summary>
        /// 新增诊疗记录
        /// </summary>
        public async Task<bool> AddAsync(DiagnosisTreatmentModel model) {
            _appDbContext.DiagnosisTreatments.Add(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新诊疗记录
        /// </summary>
        public async Task<bool> UpdateAsync(DiagnosisTreatmentModel model) {
            _appDbContext.DiagnosisTreatments.Update(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除诊疗记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _appDbContext.DiagnosisTreatments.FindAsync(id);
            if (model == null)
                return false;
            _appDbContext.DiagnosisTreatments.Remove(model);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
