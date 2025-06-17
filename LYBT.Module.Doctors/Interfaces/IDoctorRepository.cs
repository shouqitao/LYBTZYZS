using LYBT.Models;
using LYBT.Models.Doctors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Doctors.Dtos;

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
        /// 获取所有医生列表（不分页）
        /// </summary>
        Task<List<DoctorModel>> GetListAsync();

        /// <summary>
        /// 搜索医生
        /// </summary>
        Task<List<DoctorModel>> SearchAsync(string keyword);

        /// <summary>
        /// 分页获取医生
        /// </summary>
        Task<(List<DoctorModel> list, int total)> GetPagedAsync(DoctorQueryDto query);

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<bool> AddAsync(DoctorModel doctorModel);

        /// <summary>
        /// 更新医生
        /// </summary>
        Task<bool> UpdateAsync(DoctorModel doctorModel);

        /// <summary>
        /// 禁用医生
        /// </summary>
        Task<bool> DisableAsync(Guid id);

        /// <summary>
        /// 启用医生
        /// </summary>
        Task<bool> EnableAsync(Guid id);

        /// <summary>
        /// 批量禁用
        /// </summary>
        Task<int> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用
        /// </summary>
        Task<int> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 更新密码
        /// </summary>
        Task<bool> UpdatePasswordAsync(Guid id, string passwordHash);
    }
}
