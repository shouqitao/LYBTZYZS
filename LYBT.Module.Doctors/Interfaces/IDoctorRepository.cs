using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Doctors.Interfaces {

    /// <summary>
    /// 医生仓储接口
    /// 实现软删除策略：医生只能禁用/启用，不能物理删除
    /// </summary>
    public interface IDoctorRepository {

        /// <summary>
        /// 根据ID获取医生详情
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        Task<DoctorModel?> GetByIdAsync(Guid id, bool includeDisabled = false);

        /// <summary>
        /// 根据用户ID获取医生详情
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        Task<DoctorModel?> GetByUserIdAsync(Guid userId, bool includeDisabled = false);

        /// <summary>
        /// 获取所有在职医生列表
        /// </summary>
        Task<List<DoctorModel>> GetActiveDoctorsAsync();

        /// <summary>
        /// 搜索医生
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        Task<List<DoctorModel>> SearchAsync(string keyword, bool includeDisabled = false);

        /// <summary>
        /// 分页获取医生
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        Task<(List<DoctorModel> list, int total)> GetPagedAsync(DoctorQueryDto query, bool includeDisabled = false);

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<bool> AddAsync(DoctorModel model);

        /// <summary>
        /// 更新医生
        /// </summary>
        Task<bool> UpdateAsync(DoctorModel model);

        /// <summary>
        /// 禁用医生（软删除）
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
        /// 检查医生是否存在（包括禁用的医生）
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// 根据拼音码搜索医生
        /// 权限控制：禁用的医生仅管理员可查询
        /// </summary>
        Task<List<DoctorModel>> SearchByPinyinAsync(string pinyin, bool includeDisabled = false);
    }
}