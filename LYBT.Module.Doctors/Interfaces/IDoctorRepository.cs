using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Doctors.Interfaces {

    /// <summary>
    /// 医生仓储接口
    /// </summary>
    public interface IDoctorRepository {

        /// <summary>
        /// 根据ID获取医生详情
        /// </summary>
        Task<DoctorModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据用户ID获取医生详情
        /// </summary>
        Task<DoctorModel?> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 获取所有在职医生列表
        /// </summary>
        Task<List<DoctorModel>> GetActiveDoctorsAsync();

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
        Task<bool> AddAsync(DoctorModel model);

        /// <summary>
        /// 更新医生
        /// </summary>
        Task<bool> UpdateAsync(DoctorModel model);

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
        /// 检查医生是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// 根据拼音码搜索医生
        /// </summary>
        Task<List<DoctorModel>> SearchByPinyinAsync(string pinyin);
    }
}