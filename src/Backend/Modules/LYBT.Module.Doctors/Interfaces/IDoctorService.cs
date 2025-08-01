using LYBT.Shared.Models.Enums;
using LYBT.Common.Models;
using LYBT.Shared.Models.Common;
using LYBT.Models.Doctors;

namespace LYBT.Module.Doctors.Interfaces {

    /// <summary>
    /// 医生业务服务接口
    /// 实现软删除策略：医生只能禁用/启用，不能物理删除
    /// </summary>
    public interface IDoctorService {

        /// <summary>
        /// 根据ID获取医生详情
        /// 根据当前操作者角色决定是否包含禁用医生
        /// </summary>
        Task<ApiResponse<DoctorDetailDto>> GetByIdAsync(Guid id, UserRole currentUserRole);

        /// <summary>
        /// 根据用户ID获取医生详情
        /// 根据当前操作者角色决定是否包含禁用医生
        /// </summary>
        Task<ApiResponse<DoctorDetailDto>> GetByUserIdAsync(Guid userId, UserRole currentUserRole);

        /// <summary>
        /// 搜索医生
        /// 根据当前操作者角色决定是否包含禁用医生
        /// </summary>
        Task<ApiResponse<List<DoctorDto>>> SearchAsync(string keyword, UserRole currentUserRole);

        /// <summary>
        /// 分页获取医生列表
        /// 根据当前操作者角色决定是否包含禁用医生
        /// </summary>
        Task<ApiResponse<PaginatedResult<DoctorDto>>> GetPagedAsync(DoctorQueryDto query, UserRole currentUserRole);

        /// <summary>
        /// 新增医生档案（仅管理员可操作，且用户必须具有医生角色）
        /// </summary>
        Task<ApiResponse<bool>> AddAsync(DoctorDetailDto dto, UserRole operatorRole);

        /// <summary>
        /// 更新医生信息（管理员可操作，医生可修改自己的档案）
        /// </summary>
        Task<ApiResponse<bool>> UpdateAsync(DoctorDetailDto dto, UserRole operatorRole, Guid operatorUserId);

        /// <summary>
        /// 禁用医生（软删除）
        /// </summary>
        Task<ApiResponse<bool>> DisableAsync(Guid id);

        /// <summary>
        /// 启用医生
        /// </summary>
        Task<ApiResponse<bool>> EnableAsync(Guid id);

        /// <summary>
        /// 批量禁用医生
        /// </summary>
        Task<ApiResponse<int>> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用医生
        /// </summary>
        Task<ApiResponse<int>> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 获取所有在职医生列表（不分页）
        /// </summary>
        Task<ApiResponse<List<DoctorDto>>> GetActiveDoctorsAsync();

        /// <summary>
        /// 检查用户是否已关联医生档案
        /// </summary>
        Task<ApiResponse<bool>> IsUserLinkedToDoctorAsync(Guid userId);
    }
}