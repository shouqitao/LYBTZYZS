using LYBT.Shared.Models.Common;
// using LYBT.Shared.Models.Enums; // UserRole已删除
using SharedUserCreateDto = LYBT.Shared.Models.Contracts.Users.UserCreateDto;
using SharedUserDto = LYBT.Shared.Models.Contracts.Users.UserDto;
using SharedUserPagedQueryDto = LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto;
using SharedUserUpdateDto = LYBT.Shared.Models.Contracts.Users.UserUpdateDto;

namespace LYBT.Module.Users.Interfaces {

    /// <summary>
    /// 用户服务接口，封装业务逻辑（含日志集成）
    /// </summary>
    public interface IUserService {

        /// <summary>
        /// 分页/条件查找用户
        /// 根据当前操作者角色决定是否包含禁用用户
        /// </summary>
        Task<PaginatedResult<SharedUserDto>> GetPagedAsync(SharedUserPagedQueryDto query);

        /// <summary>
        /// 根据ID获取用户详情
        /// 根据当前操作者角色决定是否包含禁用用户
        /// </summary>
        Task<SharedUserDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增用户
        /// </summary>
        Task<SharedUserDto?> AddAsync(SharedUserCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 编辑用户
        /// </summary>
        Task<bool> UpdateAsync(SharedUserUpdateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 禁用用户（软删除）
        /// </summary>
        Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 启用用户
        /// </summary>
        Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 管理员重置密码为默认值
        /// </summary>
        Task<bool> ResetPasswordAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 用户修改密码
        /// </summary>
        Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        /// <summary>
        /// 用户修改个人信息
        /// </summary>
        Task<bool> ChangeProfileAsync(Guid id, string realName, string? phoneNumber);

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        List<object> GetRoles();

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        Task<List<SharedUserDto>> GetActiveUsersAsync();
    }
}