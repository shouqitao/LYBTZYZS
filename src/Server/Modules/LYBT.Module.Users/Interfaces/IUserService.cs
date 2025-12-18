using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户服务统一接口 - 标准CRUD模式
    /// Issue #1008: 重构为标准接口，移除过度设计方法
    /// 遵循单一服务原则，符合MVP适度设计原则
    /// </summary>
    public interface IUserService
    {
        #region 查询操作

        /// <summary>
        /// 分页获取用户列表（返回UserListDto，用于列表视图）
        /// OpenSpec: refactor-dto-simplification - 使用扁平化DTO
        /// </summary>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字（可选，搜索用户名/邮箱/真实姓名）</param>
        /// <param name="role">角色筛选（可选）</param>
        /// <param name="status">状态筛选（可选）</param>
        Task<Result<PagedResult<UserListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null);

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        Task<Result<UserDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 搜索用户（返回所有匹配结果）
        /// </summary>
        /// <param name="keyword">搜索关键字</param>
        Task<Result<List<UserListDto>>> SearchAsync(string keyword);

        #endregion

        #region 业务操作

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<Result<UserDetailDto>> CreateAsync(UserInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<Result<UserDetailDto>> UpdateAsync(Guid id, UserInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// 管理员重置密码（Issue #1162: 支持自动生成临时密码）
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="request">重置密码请求</param>
        Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request);

        /// <summary>
        /// 验证用户密码
        /// Issue #1864: Auth/User职责分离，密码验证由UserService负责
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">明文密码</param>
        /// <returns>验证成功返回用户信息，失败返回错误</returns>
        Task<Result<UserDetailDto>> ValidatePasswordAsync(string userName, string password);

        /// <summary>
        /// 更改密码
        /// </summary>
        Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        /// <summary>
        /// 修改个人信息 (Issue #1888)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="dto">个人资料DTO</param>
        Task<Result<UserDetailDto>> ChangeProfileAsync(Guid userId, ChangeProfileDto dto);

        #endregion

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法 ==========

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        /// <param name="id">用户ID</param>
        Task<Result<UserDetailDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复软删除的用户
        /// </summary>
        /// <param name="id">用户ID</param>
        Task<Result<UserDetailDto>> RestoreAsync(Guid id);
    }
}
