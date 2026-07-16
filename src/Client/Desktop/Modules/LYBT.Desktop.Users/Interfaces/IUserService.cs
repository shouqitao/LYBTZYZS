using LYBT.Desktop.Shared.Results;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using System.Threading;

namespace LYBT.Desktop.Users.Interfaces
{
    /// <summary>
    /// 用户Service接口
    /// </summary>
    public interface IUserService
    {
        #region 基本CRUD操作

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<CommandResult<UserDetailDto>> CreateUserAsync(UserInputDto createDto, CancellationToken ct = default);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<CommandResult<UserDetailDto>> UpdateUserAsync(UserInputDto updateDto, CancellationToken ct = default);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<CommandResult<bool>> DeleteUserAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// 批量删除用户
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> userIds, CancellationToken ct = default);

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<CommandResult<UserDetailDto>> GetByIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// 分页查询用户
        /// </summary>
        Task<CommandResult<PagedResult<UserListDto>>> GetPagedAsync(
            int page, int pageSize, string? searchText = null, CancellationToken ct = default);

        /// <summary>
        /// 获取所有用户
        /// </summary>
        Task<CommandResult<List<UserDetailDto>>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<CommandResult<UserDetailDto>> GetByUsernameAsync(string username, CancellationToken ct = default);

        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<CommandResult<List<UserListDto>>> SearchAsync(string keyword, CancellationToken ct = default);

        /// <summary>
        /// 获取医生列表
        /// </summary>
        Task<CommandResult<List<UserListDto>>> GetDoctorsAsync(CancellationToken ct = default);

        #endregion

        #region 个人资料管理

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        Task<CommandResult<UserDetailDto>> ChangeProfileAsync(
            Guid userId, ChangeProfileDto dto, CancellationToken ct = default);

        #endregion

        #region 密码管理

        /// <summary>
        /// 修改密码（占位实现 - 实际应该调用认证服务）
        /// </summary>
        Task<CommandResult<bool>> ChangePasswordAsync(
            Guid userId, string oldPassword, string newPassword, CancellationToken ct = default);

        /// <summary>
        /// 重置用户密码（管理员操作）(Issue #1911)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPassword">新密码（明文）</param>
        /// <returns>成功标志、错误信息、重置响应数据</returns>
        Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync(
            Guid userId,
            string newPassword, CancellationToken ct = default);

        #endregion

        #region 状态管理

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        Task<CommandResult<UserDetailDto>> ToggleStatusAsync(Guid userId, CancellationToken ct = default);

        #endregion
    }
}
