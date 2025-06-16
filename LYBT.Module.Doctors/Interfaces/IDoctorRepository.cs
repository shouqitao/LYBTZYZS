using LYBT.Models;
using LYBT.Models.Doctors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Doctors.Interfaces {
    /// <summary>
    /// 医生仓储接口，定义医生数据操作
    /// </summary>
    public interface IDoctorRepository {
        /// <summary>
        /// 获取医生详情
        /// </summary>
        Task<DoctorModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有医生列表
        /// </summary>
        Task<List<DoctorModel>> GetListAsync();

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<bool> AddAsync(DoctorModel doctorModel);

        /// <summary>
        /// 更新医生
        /// </summary>
        Task<bool> UpdateAsync(DoctorModel doctorModel);

        /// <summary>
        /// 删除医生
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
