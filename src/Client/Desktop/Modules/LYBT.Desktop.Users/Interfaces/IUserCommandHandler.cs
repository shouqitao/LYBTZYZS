using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces
{
    /// <summary>
    /// 用户命令处理器接口
    /// Desktop层架构重构 Phase 1: 接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IUserCommandHandler
    {
        #region 基本CRUD操作

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<(bool success, UserDto? user, string? errorMessage)> CreateAsync(UserInputDto createDto);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<(bool success, UserDto? user, string? errorMessage)> UpdateAsync(UserInputDto updateDto);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<(bool success, string? errorMessage)> DeleteAsync(Guid userId);

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<(bool success, UserDto? user, string? errorMessage)> GetByIdAsync(Guid userId);

        /// <summary>
        /// 分页查询用户
        /// </summary>
        Task<(bool success, PagedResult<UserDto>? data, string? errorMessage)> GetPagedAsync(
            int page, int pageSize, string? searchText = null);

        /// <summary>
        /// 获取所有用户
        /// </summary>
        Task<(bool success, List<UserDto>? users, string? errorMessage)> GetAllAsync();

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<(bool success, UserDto? user, string? errorMessage)> GetByUsernameAsync(string username);

        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<(bool success, List<UserDto>? users, string? errorMessage)> SearchAsync(string keyword);

        /// <summary>
        /// 获取医生列表
        /// </summary>
        Task<(bool success, List<UserDto>? doctors, string? errorMessage)> GetDoctorsAsync();

        #endregion

        #region 个人资料管理

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        Task<(bool success, UserDto? user, string? errorMessage)> ChangeProfileAsync(
            Guid userId, ChangeProfileDto dto);

        #endregion

        #region 密码管理

        /// <summary>
        /// 修改密码（占位实现 - 实际应该调用认证服务）
        /// </summary>
        Task<(bool success, string? errorMessage)> ChangePasswordAsync(
            Guid userId, string oldPassword, string newPassword);

        /// <summary>
        /// 重置用户密码（管理员操作）(Issue #1911)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPassword">新密码（明文）</param>
        /// <returns>成功标志、错误信息、重置响应数据</returns>
        Task<(bool success, string? errorMessage, ResetPasswordResponseDto? response)> ResetPasswordAsync(
            Guid userId,
            string newPassword);

        #endregion
    }
}
