using LYBT.Common.Models;
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
        /// 根据用户ID获取医生详情
        /// </summary>
        Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 关键词搜索
        /// </summary>
        Task<List<DoctorDto>> SearchAsync(string keyword);

        /// <summary>
        /// 分页获取医生列表
        /// </summary>
        Task<PagedResultDto<DoctorDto>> GetPagedAsync(DoctorQueryDto query);

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<bool> AddAsync(DoctorCreateDto doctorCreateDto);

        /// <summary>
        /// 编辑医生
        /// </summary>
        Task<bool> UpdateAsync(DoctorEditDto doctorEditDto);

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
        /// 重置密码
        /// </summary>
        Task<bool> ResetPasswordAsync(Guid id, string newPassword);

        /// <summary>
        /// 修改密码
        /// </summary>
        Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
    }
}