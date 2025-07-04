using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Dtos;

/// <summary>
/// 用户服务接口，封装业务逻辑（含日志集成）
/// </summary>
public interface IUserService {

    /// <summary>
    /// 分页/条件查找用户
    /// </summary>
    Task<(IList<UserDto> users, int total)> SearchAsync(UserQueryDto query);

    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    Task<UserDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// 新增用户
    /// </summary>
    Task<bool> AddAsync(UserCreateDto dto, Guid operatorId, string operatorName);

    /// <summary>
    /// 编辑用户
    /// </summary>
    Task<bool> UpdateAsync(UserDetailDto dto, Guid operatorId, string operatorName);

    /// <summary>
    /// 禁用用户
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
    Task<bool> ChangeProfileAsync(Guid id, string realName, string? email, string? phoneNumber);

    /// <summary>
    /// 获取系统所有角色
    /// </summary>
    List<UserRole> GetRoles();
}