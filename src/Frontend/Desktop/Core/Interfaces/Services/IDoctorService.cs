using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Doctors;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 医生服务接口
    /// </summary>
    public interface IDoctorService
    {
        /// <summary>
        /// 获取医生列表
        /// </summary>
        Task<List<DoctorInfo>> GetDoctorsAsync();

        /// <summary>
        /// 根据ID获取医生信息
        /// </summary>
        Task<DoctorInfo> GetDoctorByIdAsync(Guid id);

        /// <summary>
        /// 添加医生
        /// </summary>
        Task<bool> AddDoctorAsync(DoctorInfo doctor);

        /// <summary>
        /// 更新医生信息
        /// </summary>
        Task<bool> UpdateDoctorAsync(DoctorInfo doctor);

        /// <summary>
        /// 删除医生
        /// </summary>
        Task<bool> DeleteDoctorAsync(Guid id);

        /// <summary>
        /// 根据科室获取医生列表
        /// </summary>
        Task<List<DoctorInfo>> GetByDepartmentAsync(string department);
    }
}