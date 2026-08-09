using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 用户Service接口
    /// </summary>
    public interface IUserService : ICrudService<UserListDto, UserDetailDto, UserInputDto>
    {
        #region 查询操作

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<CommandResult<UserDetailDto>> GetByUsernameAsync(string username, CancellationToken ct = default);

        /// <summary>
        /// 获取所有用户
        /// </summary>
        Task<CommandResult<List<UserDetailDto>>> GetAllAsync(CancellationToken ct = default);

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
        Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync(
            Guid userId,
            string newPassword, CancellationToken ct = default);

        #endregion
    }
}
