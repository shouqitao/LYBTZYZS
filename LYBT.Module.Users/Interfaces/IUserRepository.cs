using System;
using System.Collections.Generic;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users.Models;

/// <summary>
/// 用户仓储接口，定义用户数据的持久化操作
/// </summary>
public interface IUserRepository {
    /// <summary>
    /// 新增用户
    /// </summary>
    Task<bool> AddAsync(UserModel user);

    /// <summary>
    /// 更新用户资料（通过ID）
    /// </summary>
    Task<bool> UpdateAsync(UserModel user);

    /// <summary>
    /// 禁用用户（将用户状态设为禁用）
    /// </summary>
    Task<bool> DisableAsync(Guid id);

    /// <summary>
    /// 启用用户（将用户状态设为启用）
    /// </summary>
    Task<bool> EnableAsync(Guid id);

    /// <summary>
    /// 分页条件查找用户（支持关键词、角色、状态筛选）
    /// </summary>
    Task<(IList<UserModel> users, int total)> GetPagedAsync(UserQueryDto query);

    /// <summary>
    /// 根据用户名查找用户（登录或唯一性校验）
    /// </summary>
    Task<UserModel?> GetByUsernameAsync(string userName);

    /// <summary>
    /// 根据用户ID查找（用于编辑、禁用等内部操作）
    /// </summary>
    Task<UserModel?> GetByIdAsync(Guid id);

    /// <summary>
    /// 校验用户名是否存在
    /// </summary>
    Task<bool> ExistsByUsernameAsync(string userName);

    /// <summary>
    /// 更新用户密码
    /// </summary>
    Task<bool> UpdatePasswordAsync(Guid id, string passwordHash);

    /// <summary>
    /// 批量更新启用状态
    /// </summary>
    Task<int> UpdateActiveStatusAsync(List<Guid> ids, bool isActive);
}
