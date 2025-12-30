using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.CommandHandlers;

/// <summary>
/// 用户CommandHandler接口
/// OpenSpec: unify-desktop-architecture (Phase 2.2)
/// 封装IUserRepository，提供统一的CRUD操作和错误处理
/// </summary>
public interface IUserCommandHandler : ICommandHandlerBase<UserListDto, UserDetailDto, UserInputDto>
{
    /// <summary>
    /// 按用户名搜索
    /// </summary>
    /// <param name="username">用户名关键字</param>
    /// <returns>匹配的用户列表</returns>
    Task<CommandResult<List<UserListDto>>> SearchByUsernameAsync(string username);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="newPassword">新密码</param>
    /// <returns>重置结果</returns>
    Task<CommandResult<bool>> ResetPasswordAsync(Guid id, string newPassword);

    /// <summary>
    /// 启用/禁用用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="isActive">是否启用</param>
    /// <returns>操作结果</returns>
    Task<CommandResult<bool>> SetActiveStatusAsync(Guid id, bool isActive);
}
