using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Services.Interfaces {

    /// <summary>
    /// 用户查询服务接口 - UltraThink三层架构
    /// 职责：复杂查询逻辑，搜索统计专业化处理
    /// </summary>
    public interface IUserQueryService {

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 分页获取用户列表
        /// </summary>
        Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();

        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        Task<ServiceResult<List<object>>> GetRolesAsync();

        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query);

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        Task<ServiceResult<bool>> ValidateUsernameAsync(string username);

        /// <summary>
        /// 获取所有医生
        /// </summary>
        Task<ServiceResult<List<UserDto>>> GetDoctorsAsync();

        /// <summary>
        /// 检查医生是否在线
        /// </summary>
        Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId);
    }
}
