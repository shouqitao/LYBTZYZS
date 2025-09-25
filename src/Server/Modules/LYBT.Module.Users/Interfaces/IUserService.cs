using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户服务统一接口 - 架构简化重构
    /// 合并原 IUserBusinessService 和 IUserQueryService 功能
    /// 遵循单一服务原则，降低复杂性
    /// </summary>
    public interface IUserService
    {
        #region 查询操作

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 分页获取用户列表
        /// </summary>
        Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query);

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<ServiceResult<UserDto>> GetByUsernameAsync(string userName);

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
        /// 验证用户名是否可用
        /// </summary>
        Task<ServiceResult<bool>> ValidateUsernameAsync(string userName);

        /// <summary>
        /// 获取所有医生
        /// </summary>
        Task<ServiceResult<List<UserDto>>> GetDoctorsAsync();

        /// <summary>
        /// 检查医生是否在线
        /// </summary>
        Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId);

        #endregion

        #region 业务操作

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        Task<ServiceResult<bool>> DeleteUserAsync(Guid id);

        /// <summary>
        /// 禁用用户
        /// </summary>
        Task<ServiceResult<bool>> DisableAsync(Guid id);

        /// <summary>
        /// 启用用户
        /// </summary>
        Task<ServiceResult<bool>> EnableAsync(Guid id);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 重置密码
        /// </summary>
        Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);

        /// <summary>
        /// 更改密码
        /// </summary>
        Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        /// <summary>
        /// 修改个人信息
        /// </summary>
        Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber);

        #endregion
    }
}