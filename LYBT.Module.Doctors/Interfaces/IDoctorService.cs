using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Doctors.Dtos;

namespace LYBT.Module.Doctors.Interfaces {
    /// <summary>
    /// 医生业务服务接口，定义医生的业务操作
    /// </summary>
    public interface IDoctorService {
        /// <summary>
        /// 获取医生详情
        /// </summary>
        Task<DoctorDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取医生列表
        /// </summary>
        Task<List<DoctorDto>> GetListAsync();

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<bool> AddAsync(DoctorCreateDto doctorCreateDto);

        /// <summary>
        /// 编辑医生
        /// </summary>
        Task<bool> UpdateAsync(DoctorEditDto doctorEditDto);

        /// <summary>
        /// 删除医生
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
